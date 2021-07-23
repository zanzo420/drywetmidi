﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;

namespace Melanchall.DryWetMidi.Devices
{
    /// <summary>
    /// Represents an input MIDI device.
    /// </summary>
    public sealed class InputDevice : MidiDevice, IInputDevice
    {
        #region Constants

        private const int SysExBufferSize = 2048;
        private const int ChannelParametersBufferSize = 2;
        private static readonly int MidiTimeCodeComponentsCount = Enum.GetValues(typeof(MidiTimeCodeComponent)).Length;

        #endregion

        #region Events

        /// <summary>
        /// Occurs when a MIDI event is received.
        /// </summary>
        public event EventHandler<MidiEventReceivedEventArgs> EventReceived;

        /// <summary>
        /// Occurs when MIDI time code received, i.e. all MIDI events to complete MIDI time code are received.
        /// </summary>
        /// <remarks>
        /// This event will be raised only if <see cref="RaiseMidiTimeCodeReceived"/> is set to <c>true</c>.
        /// </remarks>
        public event EventHandler<MidiTimeCodeReceivedEventArgs> MidiTimeCodeReceived;

        /// <summary>
        /// Occurs when invalid system exclusive event is received.
        /// </summary>
        public event EventHandler<InvalidSysExEventReceivedEventArgs> InvalidSysExEventReceived;

        /// <summary>
        /// Occurs when invalid channel, system common or system real-time event received.
        /// </summary>
        public event EventHandler<InvalidShortEventReceivedEventArgs> InvalidShortEventReceived;

        #endregion

        #region Fields

        private readonly BytesToMidiEventConverter _bytesToMidiEventConverter = new BytesToMidiEventConverter(ChannelParametersBufferSize);

        private InputDeviceApi.Callback_Winmm _callback_Winmm;
        private InputDeviceApi.Callback_Apple _callback_Apple;

        private readonly byte[] _channelParametersBuffer = new byte[ChannelParametersBufferSize];

        private readonly Dictionary<MidiTimeCodeComponent, FourBitNumber> _midiTimeCodeComponents = new Dictionary<MidiTimeCodeComponent, FourBitNumber>();

        private readonly InputDeviceApi.API_TYPE _apiType;

        #endregion

        #region Constructor

        private InputDevice(IntPtr info)
            : base(info)
        {
            _apiType = InputDeviceApiProvider.Api.Api_GetApiType();
            _bytesToMidiEventConverter.ReadingSettings.SilentNoteOnPolicy = SilentNoteOnPolicy.NoteOn;
        }

        #endregion

        #region Finalizer

        /// <summary>
        /// Finalizes the current instance of the <see cref="InputDevice"/>.
        /// </summary>
        ~InputDevice()
        {
            Dispose(false);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating if <see cref="MidiTimeCodeReceived"/> event should be raised or not.
        /// </summary>
        public bool RaiseMidiTimeCodeReceived { get; set; } = true;

        /// <summary>
        /// Gets a value that indicates whether <see cref="InputDevice"/> is currently listening for
        /// incoming MIDI events.
        /// </summary>
        public bool IsListeningForEvents { get; private set; }

        /// <summary>
        /// Gets or sets reaction of the input device on <c>Note On</c> events with velocity of zero.
        /// The default is <see cref="SilentNoteOnPolicy.NoteOn"/>.
        /// </summary>
        /// <exception cref="InvalidEnumArgumentException"><paramref name="value"/> specified an invalid value.</exception>
        public SilentNoteOnPolicy SilentNoteOnPolicy
        {
            get { return _bytesToMidiEventConverter.ReadingSettings.SilentNoteOnPolicy; }
            set
            {
                ThrowIfArgument.IsInvalidEnumValue(nameof(value), value);

                _bytesToMidiEventConverter.ReadingSettings.SilentNoteOnPolicy = value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Starts listening for incoming MIDI events on the current input device.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The current <see cref="InputDevice"/> is disposed.</exception>
        /// <exception cref="MidiDeviceException">An error occurred on device.</exception>
        public void StartEventsListening()
        {
            if (IsListeningForEvents)
                return;

            EnsureDeviceIsNotDisposed();
            EnsureHandleIsCreated();

            NativeApi.HandleResult(
                InputDeviceApiProvider.Api.Api_Connect(_handle));
            IsListeningForEvents = true;
        }

        /// <summary>
        /// Stops listening for incoming MIDI events on the current input device.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The current <see cref="InputDevice"/> is disposed.</exception>
        /// <exception cref="MidiDeviceException">An error occurred on device.</exception>
        public void StopEventsListening()
        {
            if (!IsListeningForEvents || _handle == IntPtr.Zero)
                return;

            EnsureDeviceIsNotDisposed();

            NativeApi.HandleResult(
                StopEventsListeningSilently());
        }

        /// <summary>
        /// Retrieves the number of input MIDI devices presented in the system.
        /// </summary>
        /// <returns>Number of input MIDI devices presented in the system.</returns>
        public static int GetDevicesCount()
        {
            return InputDeviceApiProvider.Api.Api_GetDevicesCount();
        }

        // TODO: add get by index
        /// <summary>
        /// Retrieves all input MIDI devices presented in the system.
        /// </summary>
        /// <returns>All input MIDI devices presented in the system.</returns>
        public static IEnumerable<InputDevice> GetAll()
        {
            var devicesCount = GetDevicesCount();
            for (var i = 0; i < devicesCount; i++)
            {
                IntPtr info;
                NativeApi.HandleResult(
                    InputDeviceApiProvider.Api.Api_GetDeviceInfo(i, out info));
                yield return new InputDevice(info);
            }
        }

        /// <summary>
        /// Retrieves a first input MIDI device with the specified name.
        /// </summary>
        /// <param name="name">The name of an input MIDI device to retrieve.</param>
        /// <returns>Input MIDI device with the specified name.</returns>
        /// <exception cref="ArgumentException">
        /// <para>One of the following errors occured:</para>
        /// <list type="bullet">
        /// <item>
        /// <description><paramref name="name"/> is <c>null</c> or contains white-spaces only.</description>
        /// </item>
        /// <item>
        /// <description><paramref name="name"/> specifies an input MIDI device which is not presented in the system.</description>
        /// </item>
        /// </list>
        /// </exception>
        public static InputDevice GetByName(string name)
        {
            ThrowIfArgument.IsNullOrWhiteSpaceString(nameof(name), name, "Device name");

            var device = GetAll().FirstOrDefault(d => d.Name == name);
            if (device == null)
                throw new ArgumentException($"There is no MIDI input device '{name}'.", nameof(name));

            return device;
        }

        protected override void SetBasicDeviceInformation()
        {
            Name = InputDeviceApiProvider.Api.Api_GetDeviceName(_info);
            Manufacturer = InputDeviceApiProvider.Api.Api_GetDeviceManufacturer(_info);
            Product = InputDeviceApiProvider.Api.Api_GetDeviceProduct(_info);
            DriverVersion = InputDeviceApiProvider.Api.Api_GetDeviceDriverVersion(_info);
        }

        private void OnEventReceived(MidiEvent midiEvent)
        {
            EventReceived?.Invoke(this, new MidiEventReceivedEventArgs(midiEvent));

            if (RaiseMidiTimeCodeReceived)
            {
                var midiTimeCodeEvent = midiEvent as MidiTimeCodeEvent;
                if (midiTimeCodeEvent != null)
                    TryRaiseMidiTimeCodeReceived(midiTimeCodeEvent);
            }
        }

        private void OnMidiTimeCodeReceived(MidiTimeCodeType timeCodeType, int hours, int minutes, int seconds, int frames)
        {
            MidiTimeCodeReceived?.Invoke(this, new MidiTimeCodeReceivedEventArgs(timeCodeType, hours, minutes, seconds, frames));
        }

        private void OnInvalidSysExEventReceived(byte[] data)
        {
            InvalidSysExEventReceived?.Invoke(this, new InvalidSysExEventReceivedEventArgs(data));
        }

        private void OnInvalidShortEventReceived(byte statusByte, byte firstDataByte, byte secondDataByte)
        {
            InvalidShortEventReceived?.Invoke(this, new InvalidShortEventReceivedEventArgs(statusByte, firstDataByte, secondDataByte));
        }

        private void EnsureHandleIsCreated()
        {
            if (_handle != IntPtr.Zero)
                return;

            var sessionHandle = MidiDevicesSession.GetSessionHandle();

            switch (_apiType)
            {
                case InputDeviceApi.API_TYPE.API_TYPE_WINMM:
                    {
                        _callback_Winmm = OnMessage_Winmm;
                        NativeApi.HandleResult(
                            InputDeviceApiProvider.Api.Api_OpenDevice_Winmm(_info, sessionHandle, _callback_Winmm, SysExBufferSize, out _handle));
                    }
                    break;
                case InputDeviceApi.API_TYPE.API_TYPE_APPLE:
                    {
                        _callback_Apple = OnMessage_Apple;
                        NativeApi.HandleResult(
                            InputDeviceApiProvider.Api.Api_OpenDevice_Apple(_info, sessionHandle, _callback_Apple, out _handle));
                    }
                    break;
                default:
                    throw new NotSupportedException($"{_apiType} API is not supported.");
            }
        }

        private void DestroyHandle()
        {
            if (_handle == IntPtr.Zero)
                return;

            // TODO: handle result
            InputDeviceApiProvider.Api.Api_CloseDevice(_handle);
            _handle = IntPtr.Zero;

            MidiDevicesSession.ExitSession();
        }

        private void OnMessage_Winmm(IntPtr hMidi, NativeApi.MidiMessage wMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2)
        {
            if (!IsListeningForEvents || !IsEnabled)
                return;

            switch (wMsg)
            {
                case NativeApi.MidiMessage.MIM_DATA:
                case NativeApi.MidiMessage.MIM_MOREDATA:
                    OnShortMessage(dwParam1.ToInt32());
                    break;

                case NativeApi.MidiMessage.MIM_LONGDATA:
                    OnSysExMessage(dwParam1);
                    break;
                
                // TODO: get rid of specific errors and use one event
                case NativeApi.MidiMessage.MIM_ERROR:
                    {
                        var message = dwParam1.ToInt32();
                        var statusByte = message.GetFourthByte();
                        var firstDataByte = message.GetThirdByte();
                        var secondDataByte = message.GetSecondByte();
                        OnInvalidShortEventReceived(statusByte, firstDataByte, secondDataByte);
                    }
                    break;

                case NativeApi.MidiMessage.MIM_LONGERROR:
                    {
                        IntPtr dataPointer;
                        int size;

                        NativeApi.HandleResult(
                            InputDeviceApiProvider.Api.Api_GetSysExBufferData(dwParam1, out dataPointer, out size));

                        var data = new byte[size];
                        Marshal.Copy(dataPointer, data, 0, size);

                        OnInvalidSysExEventReceived(data);
                    }
                    break;
            }
        }

        private void OnMessage_Apple(IntPtr pktlist, IntPtr readProcRefCon, IntPtr srcConnRefCon)
        {
            if (!IsListeningForEvents || !IsEnabled)
                return;

            byte[] data = null;

            try
            {
                IntPtr dataPtr;
                int length;

                NativeApi.HandleResult(
                    InputDeviceApiProvider.Api.Api_GetEventData(pktlist, 0, out dataPtr, out length));

                data = new byte[length];
                Marshal.Copy(dataPtr, data, 0, length);

                // TODO: handle escape sysex
                if (data[0] == EventStatusBytes.Global.NormalSysEx)
                {
                    var sysExData = new byte[length - 1];
                    Buffer.BlockCopy(data, 1, sysExData, 0, sysExData.Length);

                    var midiEvent = new NormalSysExEvent(sysExData);
                    OnEventReceived(midiEvent);
                    return;
                }

                byte? runningStatusByte = null;

                using (var stream = new MemoryStream(data))
                using (var midiReader = new MidiReader(stream, new ReaderSettings()))
                {
                    midiReader.Position = 0;

                    while (midiReader.Position < length)
                    {
                        var statusByte = midiReader.ReadByte();
                        if (statusByte <= SevenBitNumber.MaxValue)
                        {
                            if (runningStatusByte == null)
                                throw new UnexpectedRunningStatusException();

                            statusByte = runningStatusByte.Value;
                            midiReader.Position--;
                        }

                        runningStatusByte = statusByte;

                        var eventReader = EventReaderFactory.GetReader(statusByte, smfOnly: false);
                        var midiEvent = eventReader.Read(midiReader, _bytesToMidiEventConverter.ReadingSettings, statusByte);

                        OnEventReceived(midiEvent);
                    }
                }
            }
            catch (Exception ex)
            {
                var exception = new MidiDeviceException($"Failed to parse message.", ex);
                exception.Data.Add("Data", data);
                OnError(exception);
            }
        }

        private void OnShortMessage(int message)
        {
            try
            {
                var statusByte = (byte)(message & 0xFF);

                _channelParametersBuffer[0] = (byte)((message >> 8) & 0xFF);
                _channelParametersBuffer[1] = (byte)((message >> 16) & 0xFF);

                var midiEvent = _bytesToMidiEventConverter.Convert(statusByte, _channelParametersBuffer);
                OnEventReceived(midiEvent);
            }
            catch (Exception ex)
            {
                var exception = new MidiDeviceException($"Failed to parse short message.", ex);
                exception.Data.Add("Message", message);
                OnError(exception);
            }
        }

        private void OnSysExMessage(IntPtr sysExHeaderPointer)
        {
            byte[] data = null;

            try
            {
                IntPtr dataPointer;
                int size;

                NativeApi.HandleResult(
                    InputDeviceApiProvider.Api.Api_GetSysExBufferData(sysExHeaderPointer, out dataPointer, out size));

                data = new byte[size - 1];
                Marshal.Copy(IntPtr.Add(dataPointer, 1), data, 0, data.Length);

                var midiEvent = new NormalSysExEvent(data);
                OnEventReceived(midiEvent);

                NativeApi.HandleResult(
                    InputDeviceApiProvider.Api.Api_RenewSysExBuffer(_handle, SysExBufferSize));
            }
            catch (Exception ex)
            {
                var exception = new MidiDeviceException($"Failed to parse system exclusive message.", ex);
                exception.Data.Add("Data", data);
                OnError(exception);
            }
        }

        private void TryRaiseMidiTimeCodeReceived(MidiTimeCodeEvent midiTimeCodeEvent)
        {
            var component = midiTimeCodeEvent.Component;
            var componentValue = midiTimeCodeEvent.ComponentValue;

            _midiTimeCodeComponents[component] = componentValue;
            if (_midiTimeCodeComponents.Count != MidiTimeCodeComponentsCount)
                return;

            var frames = DataTypesUtilities.Combine(_midiTimeCodeComponents[MidiTimeCodeComponent.FramesMsb],
                                                    _midiTimeCodeComponents[MidiTimeCodeComponent.FramesLsb]);

            var minutes = DataTypesUtilities.Combine(_midiTimeCodeComponents[MidiTimeCodeComponent.MinutesMsb],
                                                     _midiTimeCodeComponents[MidiTimeCodeComponent.MinutesLsb]);

            var seconds = DataTypesUtilities.Combine(_midiTimeCodeComponents[MidiTimeCodeComponent.SecondsMsb],
                                                     _midiTimeCodeComponents[MidiTimeCodeComponent.SecondsLsb]);

            var hoursAndTimeCodeType = DataTypesUtilities.Combine(_midiTimeCodeComponents[MidiTimeCodeComponent.HoursMsbAndTimeCodeType],
                                                                  _midiTimeCodeComponents[MidiTimeCodeComponent.HoursLsb]);
            var hours = hoursAndTimeCodeType & 0x1F;
            var timeCodeType = (MidiTimeCodeType)((hoursAndTimeCodeType >> 5) & 0x3);

            OnMidiTimeCodeReceived(timeCodeType, hours, minutes, seconds, frames);
            _midiTimeCodeComponents.Clear();
        }

        private InputDeviceApi.IN_DISCONNECTRESULT StopEventsListeningSilently()
        {
            IsListeningForEvents = false;
            return InputDeviceApiProvider.Api.Api_Disconnect(_handle);
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Releases the unmanaged resources used by the MIDI device class and optionally releases
        /// the managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to
        /// release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _bytesToMidiEventConverter.Dispose();
            }

            if (_handle != IntPtr.Zero)
            {
                StopEventsListeningSilently();
                DestroyHandle();
            }

            _disposed = true;
        }

        #endregion
    }
}
