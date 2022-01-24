using System;
using System.Linq;
using Melanchall.DryWetMidi.Interaction;
using NUnit.Framework;

namespace Melanchall.DryWetMidi.Tests.Interaction
{
    [TestFixture]
    public sealed class MetricTimeSpanTests
    {
        #region Constants

        private static readonly MusicalTimeSpan MusicalSpan = 50 * MusicalTimeSpan.Whole;

        private const long ZeroTime = 0;
        private const long ShortTime = 1000;
        private const long LargeTime = 100000;

        private static readonly ITimeSpan ZeroTimeSpan = new MidiTimeSpan();
        private static readonly ITimeSpan ShortTimeSpan = MusicalTimeSpan.Quarter.Dotted(2);
        private static readonly ITimeSpan LargeTimeSpan = new MetricTimeSpan(0, 2, 30);

        private static readonly MetricTimeSpan ZeroSpan = new MetricTimeSpan();
        private static readonly MetricTimeSpan ShortSpan = new MetricTimeSpan(0, 0, 5);
        private static readonly MetricTimeSpan LongSpan = new MetricTimeSpan(0, 5, 5);

        private static readonly Tuple<MetricTimeSpan, MetricTimeSpan>[] TimeSpansForComparison_Less = new[]
        {
            Tuple.Create(new MetricTimeSpan(), new MetricTimeSpan(0, 0, 1)),
            Tuple.Create(new MetricTimeSpan(), new MetricTimeSpan(0, 1, 0)),
            Tuple.Create(new MetricTimeSpan(), new MetricTimeSpan(1, 0, 0)),
            Tuple.Create(new MetricTimeSpan(), new MetricTimeSpan(0, 0, 1)),
            Tuple.Create(new MetricTimeSpan(2, 0, 0), new MetricTimeSpan(10, 0, 1)),
            Tuple.Create(new MetricTimeSpan(0, 10, 0), new MetricTimeSpan(0, 10, 1)),
            Tuple.Create(new MetricTimeSpan(10, 10, 0), new MetricTimeSpan(10, 10, 1)),
            Tuple.Create(new MetricTimeSpan(10000, 899, 0), new MetricTimeSpan(10000, 10000, 0)),
            Tuple.Create(new MetricTimeSpan(0, 100, 0), new MetricTimeSpan(0, 110, 1)),
            Tuple.Create(new MetricTimeSpan(199, 0, 1000), new MetricTimeSpan(200, 0, 800)),
            Tuple.Create(new MetricTimeSpan(), new MetricTimeSpan(10, 110, 891)),
            Tuple.Create(new MetricTimeSpan(), new MetricTimeSpan(10)),
            Tuple.Create(new MetricTimeSpan(10), new MetricTimeSpan(1000))
        };

        private static readonly Tuple<MetricTimeSpan, MetricTimeSpan>[] TimeSpansForComparison_Equal = new[]
        {
            Tuple.Create(new MetricTimeSpan(), new MetricTimeSpan()),
            Tuple.Create(new MetricTimeSpan(), new MetricTimeSpan(0, 0, 0)),
            Tuple.Create(new MetricTimeSpan(10, 0, 0), new MetricTimeSpan(10, 0, 0)),
            Tuple.Create(new MetricTimeSpan(100, 100, 100), new MetricTimeSpan(100, 100, 100)),
            Tuple.Create(new MetricTimeSpan(0, 345, 0), new MetricTimeSpan(0, 345, 0)),
            Tuple.Create(new MetricTimeSpan(0, 0, 1234), new MetricTimeSpan(0, 0, 1234)),
            Tuple.Create(new MetricTimeSpan(0), new MetricTimeSpan()),
            Tuple.Create(new MetricTimeSpan(10000), new MetricTimeSpan(10000)),
        };

        private static readonly object[] ParametersForValidParseCheck =
        {
            new object[] { "0:0:0:0", new MetricTimeSpan() },
            new object[] { "0:0:0", new MetricTimeSpan() },
            new object[] { "0:0", new MetricTimeSpan() },
            new object[] { "0:0:0:156", new MetricTimeSpan(0, 0, 0, 156) },
            new object[] { "2:0:156", new MetricTimeSpan(2, 0, 156) },
            new object[] { "1:156", new MetricTimeSpan(0, 1, 156) },

            new object[] { "1h2m3s4ms", new MetricTimeSpan(1, 2, 3, 4) },
            new object[] { "1h 2m3s", new MetricTimeSpan(1, 2, 3, 0) },
            new object[] { "1h2M 4ms", new MetricTimeSpan(1, 2, 0, 4) },
            new object[] { "1 h3s4ms", new MetricTimeSpan(1, 0, 3, 4) },
            new object[] { "2M3 S 4 MS", new MetricTimeSpan(0, 2, 3, 4) },
            new object[] { "1h2m", new MetricTimeSpan(1, 2, 0, 0) },
            new object[] { "1h 3s", new MetricTimeSpan(1, 0, 3, 0) },
            new object[] { "1h4MS", new MetricTimeSpan(1, 0, 0, 4) },
            new object[] { "2M3s", new MetricTimeSpan(0, 2, 3, 0) },
            new object[] { "2 m 4 Ms", new MetricTimeSpan(0, 2, 0, 4) },
            new object[] { "3 s 4 mS", new MetricTimeSpan(0, 0, 3, 4) },
        };

        private const double DoubleEpsilon = 0.0000001;

        private static readonly object[] Parameters_CheckTotalMicroseconds_FromTimeSpan =
        {
            new object[] { new TimeSpan(0, 0, 0), 0 },
            new object[] { new TimeSpan(0, 0, 1), 1 * 1000 * 1000 },
            new object[] { new TimeSpan(0, 1, 0), 1 * 60 * 1000 * 1000 },
            new object[] { new TimeSpan(0, 0, 0, 0, 1), 1 * 1000 },
            new object[] { new TimeSpan(1, 0, 0, 0, 1), 1L * 24 * 60 * 60 * 1000 * 1000 + 1 * 1000 },
            new object[] { new TimeSpan(0, 2, 0, 1, 0), 2L * 60 * 60 * 1000 * 1000 + 1 * 1000 * 1000 },
        };

        private static readonly object[] Parameters_CheckTotalMicroseconds_FromFields =
        {
            new object[] { new MetricTimeSpan(0), 0 },
            new object[] { new MetricTimeSpan(0, 0, 0), 0 },
            new object[] { new MetricTimeSpan(0, 0, 1), 1 * 1000 * 1000 },
            new object[] { new MetricTimeSpan(0, 1, 0), 1 * 60 * 1000 * 1000 },
            new object[] { new MetricTimeSpan(0, 0, 0, 1), 1 * 1000 },
            new object[] { new MetricTimeSpan(1, 0, 0, 1), 1L * 60 * 60 * 1000 * 1000 + 1 * 1000 },
            new object[] { new MetricTimeSpan(1, 0, 60, 1), 1L * 60 * 60 * 1000 * 1000 + 60 * 1000 * 1000 + 1 * 1000 },
        };

        private static readonly object[] Parameters_CheckTotalMilliseconds_FromTimeSpan =
        {
            new object[] { new TimeSpan(0, 0, 0), 0 },
            new object[] { new TimeSpan(0, 0, 1), 1 * 1000 },
            new object[] { new TimeSpan(0, 1, 0), 1 * 60 * 1000 },
            new object[] { new TimeSpan(0, 0, 0, 0, 1), 1 },
            new object[] { new TimeSpan(1, 0, 2, 0, 3), 1 * 24 * 60 * 60 * 1000 + 2 * 60 * 1000 + 3 },
        };

        private static readonly object[] Parameters_CheckTotalMilliseconds_FromFields =
        {
            new object[] { new MetricTimeSpan(0), 0 },
            new object[] { new MetricTimeSpan(500), 0.5 },
            new object[] { new MetricTimeSpan(0, 0, 0), 0 },
            new object[] { new MetricTimeSpan(0, 0, 1), 1 * 1000 },
            new object[] { new MetricTimeSpan(0, 1, 0), 1 * 60 * 1000 },
            new object[] { new MetricTimeSpan(0, 0, 0, 1), 1 },
            new object[] { new MetricTimeSpan(1, 2, 3, 4), 1 * 60 * 60 * 1000 + 2 * 60 * 1000 + 3 * 1000 + 4 },
        };

        private static readonly object[] Parameters_CheckTotalSeconds_FromTimeSpan =
        {
            new object[] { new TimeSpan(0, 0, 0), 0 },
            new object[] { new TimeSpan(0, 0, 1), 1 },
            new object[] { new TimeSpan(0, 1, 0), 1 * 60 },
            new object[] { new TimeSpan(0, 0, 0, 0, 1), 1.0 / 1000 },
            new object[] { new TimeSpan(0, 1, 0, 0, 1), 1 * 60 * 60 + 1.0 / 1000 },
            new object[] { new TimeSpan(0, 0, 1, 2, 3), 1 * 60 + 2 + 3.0 / 1000 },
        };

        private static readonly object[] Parameters_CheckTotalSeconds_FromFields =
        {
            new object[] { new MetricTimeSpan(0), 0 },
            new object[] { new MetricTimeSpan(500), 0.5 / 1000 },
            new object[] { new MetricTimeSpan(0, 0, 0), 0 },
            new object[] { new MetricTimeSpan(0, 0, 1), 1 },
            new object[] { new MetricTimeSpan(0, 1, 0), 1 * 60 },
            new object[] { new MetricTimeSpan(0, 0, 0, 1), 1.0 / 1000 },
            new object[] { new MetricTimeSpan(1, 2, 3, 4), 1 * 60 * 60 + 2 * 60 + 3 + 4.0 / 1000 },
        };

        private static readonly object[] Parameters_CheckTotalMinutes_FromTimeSpan =
        {
            new object[] { new TimeSpan(0, 0, 0), 0 },
            new object[] { new TimeSpan(0, 0, 30), 30.0 / 60 },
            new object[] { new TimeSpan(0, 1, 0), 1 },
            new object[] { new TimeSpan(0, 0, 0, 0, 60), 60.0 / 1000 / 60  },
            new object[] { new TimeSpan(1, 2, 3, 0, 60), 1 * 24 * 60 + 2 * 60 + 3 + 60.0 / 1000 / 60  },
        };

        private static readonly object[] Parameters_CheckTotalMinutes_FromFields =
        {
            new object[] { new MetricTimeSpan(0), 0 },
            new object[] { new MetricTimeSpan(500), 500.0 / 1000 / 1000 / 60 },
            new object[] { new MetricTimeSpan(0, 0, 0), 0 },
            new object[] { new MetricTimeSpan(0, 0, 30), 30.0 / 60 },
            new object[] { new MetricTimeSpan(0, 1, 0), 1 },
            new object[] { new MetricTimeSpan(0, 0, 0, 60), 60.0 / 1000 / 60 },
            new object[] { new MetricTimeSpan(1, 2, 3, 4), 1 * 60 + 2 + 3.0 / 60 + 4.0 / 1000 / 60 },
        };

        private static readonly object[] Parameters_CheckTotalHours_FromTimeSpan =
        {
            new object[] { new TimeSpan(0, 0, 0), 0 },
            new object[] { new TimeSpan(1, 0, 0), 1 },
            new object[] { new TimeSpan(0, 0, 1800), 1800.0 / 60 / 60 },
            new object[] { new TimeSpan(0, 30, 0), 30.0 / 60 },
            new object[] { new TimeSpan(0, 0, 0, 0, 360), 360.0 / 1000 / 60 / 60 },
            new object[] { new TimeSpan(1, 2, 3, 0, 4), 1 * 24 + 2 + 3.0 / 60 + 4.0 / 1000 / 60 / 60 },
        };

        private static readonly object[] Parameters_CheckTotalHours_FromFields =
        {
            new object[] { new MetricTimeSpan(0), 0 },
            new object[] { new MetricTimeSpan(100), 100.0 / 1000 / 1000 / 60 / 60 },
            new object[] { new MetricTimeSpan(0, 0, 0), 0 },
            new object[] { new MetricTimeSpan(0, 0, 1800), 1800.0 / 60 / 60 },
            new object[] { new MetricTimeSpan(0, 60, 0), 1 },
            new object[] { new MetricTimeSpan(1, 0, 0), 1 },
            new object[] { new MetricTimeSpan(0, 0, 0, 360), 360.0 / 1000 / 60 / 60 },
            new object[] { new MetricTimeSpan(1, 2, 3, 4), 1 + 2.0 / 60 + 3.0 / 60 / 60 + 4.0 / 1000 / 60 / 60 },
        };

        private static readonly object[] Parameters_CheckTotalDays_FromTimeSpan =
        {
            new object[] { new TimeSpan(0, 0, 0), 0 },
            new object[] { new TimeSpan(12, 0, 0), 12.0 / 24 },
            new object[] { new TimeSpan(0, 0, 1800), 1800.0 / 60 / 60 / 24 },
            new object[] { new TimeSpan(0, 30, 0), 30.0 / 60 / 24 },
            new object[] { new TimeSpan(0, 0, 0, 0, 360), 360.0 / 1000 / 60 / 60 / 24 },
            new object[] { new TimeSpan(0, 2, 0, 3), 2.0 / 24 + 3.0 / 60 / 60 / 24 },
        };

        private static readonly object[] Parameters_CheckTotalDays_FromFields =
        {
            new object[] { new MetricTimeSpan(0), 0 },
            new object[] { new MetricTimeSpan(1000), 1000.0 / 1000 / 1000 / 60 / 60 / 24 },
            new object[] { new MetricTimeSpan(0, 0, 0), 0 },
            new object[] { new MetricTimeSpan(0, 0, 1800), 1800.0 / 60 / 60 / 24 },
            new object[] { new MetricTimeSpan(0, 60, 0), 60.0 / 60 / 24 },
            new object[] { new MetricTimeSpan(1, 0, 0), 1.0 / 24 },
            new object[] { new MetricTimeSpan(0, 0, 0, 360), 360.0 / 1000 / 60 / 60 / 24 },
            new object[] { new MetricTimeSpan(1, 2, 3, 4), 1.0 / 24 + 2.0 / 60 / 24 + 3.0 / 60 / 60 / 24 + 4.0 / 1000 / 60 / 60 / 24 },
        };

        #endregion

        #region Test methods

        #region Convert

        #region Default

        [Test]
        public void Convert_Default_1()
        {
            TimeSpanTestUtilities.TestConversion(ZeroSpan,
                                                 new MidiTimeSpan(),
                                                 ZeroTimeSpan,
                                                 TimeSpanTestUtilities.DefaultTempoMap);
        }

        [Test]
        public void Convert_Default_2()
        {
            var timeSpan = ShortSpan;
            TimeSpanTestUtilities.TestConversion(timeSpan,
                                                 GetDefaultMidiTimeSpan(timeSpan),
                                                 ZeroTimeSpan,
                                                 TimeSpanTestUtilities.DefaultTempoMap);
        }

        [Test]
        public void Convert_Default_3()
        {
            var timeSpan = LongSpan;
            TimeSpanTestUtilities.TestConversion(timeSpan,
                                                 GetDefaultMidiTimeSpan(timeSpan),
                                                 ZeroTimeSpan,
                                                 TimeSpanTestUtilities.DefaultTempoMap);
        }

        [Test]
        public void Convert_Default_4()
        {
            TimeSpanTestUtilities.TestConversion(ZeroSpan,
                                                 new MidiTimeSpan(),
                                                 ShortTimeSpan,
                                                 TimeSpanTestUtilities.DefaultTempoMap);
        }

        [Test]
        public void Convert_Default_5()
        {
            var timeSpan = ShortSpan;
            TimeSpanTestUtilities.TestConversion(timeSpan,
                                                 GetDefaultMidiTimeSpan(timeSpan),
                                                 ShortTimeSpan,
                                                 TimeSpanTestUtilities.DefaultTempoMap);
        }

        [Test]
        public void Convert_Default_6()
        {
            var timeSpan = LongSpan;
            TimeSpanTestUtilities.TestConversion(timeSpan,
                                                 GetDefaultMidiTimeSpan(timeSpan),
                                                 ShortTimeSpan,
                                                 TimeSpanTestUtilities.DefaultTempoMap);
        }

        [Test]
        public void Convert_Default_7()
        {
            TimeSpanTestUtilities.TestConversion(ZeroSpan,
                                                 new MidiTimeSpan(),
                                                 LargeTimeSpan,
                                                 TimeSpanTestUtilities.DefaultTempoMap);
        }

        [Test]
        public void Convert_Default_8()
        {
            var timeSpan = ShortSpan;
            TimeSpanTestUtilities.TestConversion(timeSpan,
                                                 GetDefaultMidiTimeSpan(timeSpan),
                                                 LargeTimeSpan,
                                                 TimeSpanTestUtilities.DefaultTempoMap);
        }

        [Test]
        public void Convert_Default_9()
        {
            var timeSpan = LongSpan;
            TimeSpanTestUtilities.TestConversion(timeSpan,
                                                 GetDefaultMidiTimeSpan(timeSpan),
                                                 LargeTimeSpan,
                                                 TimeSpanTestUtilities.DefaultTempoMap);
        }

        #endregion

        #region Simple

        [Test]
        public void Convert_Simple_1()
        {
            TimeSpanTestUtilities.TestConversion(ZeroSpan,
                                                 new MidiTimeSpan(),
                                                 ZeroTimeSpan,
                                                 TimeSpanTestUtilities.SimpleTempoMap);
        }

        [Test]
        public void Convert_Simple_2()
        {
            TimeSpanTestUtilities.TestConversion(ShortSpan,
                                                 ShortSpan,
                                                 ZeroTimeSpan,
                                                 TimeSpanTestUtilities.SimpleTempoMap);
        }

        [Test]
        public void Convert_Simple_3()
        {
            TimeSpanTestUtilities.TestConversion(LongSpan,
                                                 LongSpan,
                                                 ZeroTimeSpan,
                                                 TimeSpanTestUtilities.SimpleTempoMap);
        }

        [Test]
        public void Convert_Simple_4()
        {
            TimeSpanTestUtilities.TestConversion(ZeroSpan,
                                                 new MidiTimeSpan(),
                                                 ShortTimeSpan,
                                                 TimeSpanTestUtilities.SimpleTempoMap);
        }

        [Test]
        public void Convert_Simple_5()
        {
            TimeSpanTestUtilities.TestConversion(ShortSpan,
                                                 ShortSpan,
                                                 ShortTimeSpan,
                                                 TimeSpanTestUtilities.SimpleTempoMap);
        }

        [Test]
        public void Convert_Simple_6()
        {
            TimeSpanTestUtilities.TestConversion(LongSpan,
                                                 LongSpan,
                                                 ShortTimeSpan,
                                                 TimeSpanTestUtilities.SimpleTempoMap);
        }

        [Test]
        public void Convert_Simple_7()
        {
            TimeSpanTestUtilities.TestConversion(ZeroSpan,
                                                 new MidiTimeSpan(),
                                                 LargeTimeSpan,
                                                 TimeSpanTestUtilities.SimpleTempoMap);
        }

        [Test]
        public void Convert_Simple_8()
        {
            TimeSpanTestUtilities.TestConversion(ShortSpan,
                                                 ShortSpan,
                                                 LargeTimeSpan,
                                                 TimeSpanTestUtilities.SimpleTempoMap);
        }

        [Test]
        public void Convert_Simple_9()
        {
            TimeSpanTestUtilities.TestConversion(LongSpan,
                                                 LongSpan,
                                                 LargeTimeSpan,
                                                 TimeSpanTestUtilities.SimpleTempoMap);
        }

        #endregion

        #region Complex

        [Test]
        public void Convert_Complex_1()
        {
            TimeSpanTestUtilities.TestConversion(ZeroSpan,
                                                 new MidiTimeSpan(),
                                                 ZeroTimeSpan,
                                                 TimeSpanTestUtilities.ComplexTempoMap);
        }

        [Test]
        public void Convert_Complex_2()
        {
            TimeSpanTestUtilities.TestConversion(ShortSpan,
                                                 ShortSpan,
                                                 ZeroTimeSpan,
                                                 TimeSpanTestUtilities.ComplexTempoMap);
        }

        [Test]
        public void Convert_Complex_3()
        {
            TimeSpanTestUtilities.TestConversion(LongSpan,
                                                 LongSpan,
                                                 ZeroTimeSpan,
                                                 TimeSpanTestUtilities.ComplexTempoMap);
        }

        [Test]
        public void Convert_Complex_4()
        {
            TimeSpanTestUtilities.TestConversion(ZeroSpan,
                                                 new MidiTimeSpan(),
                                                 ShortTimeSpan,
                                                 TimeSpanTestUtilities.ComplexTempoMap);
        }

        [Test]
        public void Convert_Complex_5()
        {
            TimeSpanTestUtilities.TestConversion(ShortSpan,
                                                 ShortSpan,
                                                 ShortTimeSpan,
                                                 TimeSpanTestUtilities.ComplexTempoMap);
        }

        [Test]
        public void Convert_Complex_6()
        {
            TimeSpanTestUtilities.TestConversion(LongSpan,
                                                 LongSpan,
                                                 ShortTimeSpan,
                                                 TimeSpanTestUtilities.ComplexTempoMap);
        }

        [Test]
        public void Convert_Complex_7()
        {
            TimeSpanTestUtilities.TestConversion(ZeroSpan,
                                                 new MidiTimeSpan(),
                                                 LargeTimeSpan,
                                                 TimeSpanTestUtilities.ComplexTempoMap);
        }

        [Test]
        public void Convert_Complex_8()
        {
            TimeSpanTestUtilities.TestConversion(ShortSpan,
                                                 ShortSpan,
                                                 LargeTimeSpan,
                                                 TimeSpanTestUtilities.ComplexTempoMap);
        }

        [Test]
        public void Convert_Complex_9()
        {
            TimeSpanTestUtilities.TestConversion(LongSpan,
                                                 LongSpan,
                                                 LargeTimeSpan,
                                                 TimeSpanTestUtilities.ComplexTempoMap);
        }

        #endregion

        #endregion

        #region Parse

        [TestCaseSource(nameof(ParametersForValidParseCheck))]
        public void ParseMetricTimeSpan_Valid(string metricTimeSpanString, MetricTimeSpan expectedTimeSpan)
        {
            TimeSpanTestUtilities.Parse(metricTimeSpanString, expectedTimeSpan);
        }

        [TestCase("Not a time span")]
        [TestCase("h H")]
        [TestCase("m s")]
        public void ParseMetricTimeSpan_InvalidInput(string invalidMetricTimeSpanString)
        {
            TimeSpanTestUtilities.ParseInvalidInput(invalidMetricTimeSpanString);
        }

        #endregion

        #region Add

        [Test]
        public void Add_SameType_1()
        {
            TimeSpanTestUtilities.Add_SameType(new MetricTimeSpan(),
                                               new MetricTimeSpan(),
                                               new MetricTimeSpan());
        }

        [Test]
        public void Add_SameType_2()
        {
            TimeSpanTestUtilities.Add_SameType(new MetricTimeSpan(2, 5, 8, 9),
                                               new MetricTimeSpan(),
                                               new MetricTimeSpan(2, 5, 8, 9));
        }

        [Test]
        public void Add_SameType_3()
        {
            TimeSpanTestUtilities.Add_SameType(new MetricTimeSpan(2, 5, 8, 9),
                                               new MetricTimeSpan(0, 8, 7),
                                               new MetricTimeSpan(2, 13, 15, 9));
        }

        [Test]
        public void Add_TimeTime_1()
        {
            TimeSpanTestUtilities.Add_TimeTime(ShortSpan,
                                               MusicalSpan);
        }

        [Test]
        public void Add_TimeTime_2()
        {
            TimeSpanTestUtilities.Add_TimeTime(LongSpan,
                                               MusicalSpan);
        }

        [Test]
        public void Add_TimeLength_Default_1()
        {
            TimeSpanTestUtilities.Add_TimeLength(ShortSpan,
                                                 MusicalSpan,
                                                 TimeSpanTestUtilities.DefaultTempoMap);
        }

        [Test]
        public void Add_TimeLength_Default_2()
        {
            TimeSpanTestUtilities.Add_TimeLength(LongSpan,
                                                 MusicalSpan,
                                                 TimeSpanTestUtilities.DefaultTempoMap);
        }

        [Test]
        public void Add_TimeLength_Simple_1()
        {
            TimeSpanTestUtilities.Add_TimeLength(ShortSpan,
                                                 MusicalSpan,
                                                 TimeSpanTestUtilities.SimpleTempoMap);
        }

        [Test]
        public void Add_TimeLength_Simple_2()
        {
            TimeSpanTestUtilities.Add_TimeLength(LongSpan,
                                                 MusicalSpan,
                                                 TimeSpanTestUtilities.SimpleTempoMap);
        }

        [Test]
        public void Add_TimeLength_Complex_1()
        {
            TimeSpanTestUtilities.Add_TimeLength(ShortSpan,
                                                 MusicalSpan,
                                                 TimeSpanTestUtilities.ComplexTempoMap);
        }

        [Test]
        public void Add_TimeLength_Complex_2()
        {
            TimeSpanTestUtilities.Add_TimeLength(LongSpan,
                                                 MusicalSpan,
                                                 TimeSpanTestUtilities.ComplexTempoMap);
        }

        [Test]
        public void Add_LengthLength_Default_1()
        {
            TimeSpanTestUtilities.Add_LengthLength(ShortSpan,
                                                   MusicalSpan,
                                                   TimeSpanTestUtilities.DefaultTempoMap,
                                                   ZeroTime);
        }

        [Test]
        public void Add_LengthLength_Default_2()
        {
            TimeSpanTestUtilities.Add_LengthLength(ShortSpan,
                                                   MusicalSpan,
                                                   TimeSpanTestUtilities.DefaultTempoMap,
                                                   ShortTime);
        }

        [Test]
        public void Add_LengthLength_Default_3()
        {
            TimeSpanTestUtilities.Add_LengthLength(ShortSpan,
                                                   MusicalSpan,
                                                   TimeSpanTestUtilities.DefaultTempoMap,
                                                   LargeTime);
        }

        [Test]
        public void Add_LengthLength_Default_4()
        {
            TimeSpanTestUtilities.Add_LengthLength(LongSpan,
                                                   MusicalSpan,
                                                   TimeSpanTestUtilities.DefaultTempoMap,
                                                   ZeroTime);
        }

        [Test]
        public void Add_LengthLength_Default_5()
        {
            TimeSpanTestUtilities.Add_LengthLength(LongSpan,
                                                   MusicalSpan,
                                                   TimeSpanTestUtilities.DefaultTempoMap,
                                                   ShortTime);
        }

        [Test]
        public void Add_LengthLength_Default_6()
        {
            TimeSpanTestUtilities.Add_LengthLength(LongSpan,
                                                   MusicalSpan,
                                                   TimeSpanTestUtilities.DefaultTempoMap,
                                                   LargeTime);
        }

        [Test]
        public void Add_LengthLength_Simple_1()
        {
            TimeSpanTestUtilities.Add_LengthLength(ShortSpan,
                                                   MusicalSpan,
                                                   TimeSpanTestUtilities.SimpleTempoMap,
                                                   ZeroTime);
        }

        [Test]
        public void Add_LengthLength_Simple_2()
        {
            TimeSpanTestUtilities.Add_LengthLength(ShortSpan,
                                                   MusicalSpan,
                                                   TimeSpanTestUtilities.SimpleTempoMap,
                                                   ShortTime);
        }

        [Test]
        public void Add_LengthLength_Simple_3()
        {
            TimeSpanTestUtilities.Add_LengthLength(ShortSpan,
                                                   MusicalSpan,
                                                   TimeSpanTestUtilities.SimpleTempoMap,
                                                   LargeTime);
        }

        [Test]
        public void Add_LengthLength_Simple_4()
        {
            TimeSpanTestUtilities.Add_LengthLength(LongSpan,
                                                   MusicalSpan,
                                                   TimeSpanTestUtilities.SimpleTempoMap,
                                                   ZeroTime);
        }

        [Test]
        public void Add_LengthLength_Simple_5()
        {
            TimeSpanTestUtilities.Add_LengthLength(LongSpan,
                                                   MusicalSpan,
                                                   TimeSpanTestUtilities.SimpleTempoMap,
                                                   ShortTime);
        }

        [Test]
        public void Add_LengthLength_Simple_6()
        {
            TimeSpanTestUtilities.Add_LengthLength(LongSpan,
                                                   MusicalSpan,
                                                   TimeSpanTestUtilities.SimpleTempoMap,
                                                   LargeTime);
        }

        [Test]
        public void Add_LengthLength_Complex_1()
        {
            TimeSpanTestUtilities.Add_LengthLength(ShortSpan,
                                                   MusicalSpan,
                                                   TimeSpanTestUtilities.ComplexTempoMap,
                                                   ZeroTime);
        }

        [Test]
        public void Add_LengthLength_Complex_2()
        {
            TimeSpanTestUtilities.Add_LengthLength(ShortSpan,
                                                   MusicalSpan,
                                                   TimeSpanTestUtilities.ComplexTempoMap,
                                                   ShortTime);
        }

        [Test]
        public void Add_LengthLength_Complex_3()
        {
            TimeSpanTestUtilities.Add_LengthLength(ShortSpan,
                                                   MusicalSpan,
                                                   TimeSpanTestUtilities.ComplexTempoMap,
                                                   LargeTime);
        }

        [Test]
        public void Add_LengthLength_Complex_4()
        {
            TimeSpanTestUtilities.Add_LengthLength(LongSpan,
                                                   MusicalSpan,
                                                   TimeSpanTestUtilities.ComplexTempoMap,
                                                   ZeroTime);
        }

        [Test]
        public void Add_LengthLength_Complex_5()
        {
            TimeSpanTestUtilities.Add_LengthLength(LongSpan,
                                                   MusicalSpan,
                                                   TimeSpanTestUtilities.ComplexTempoMap,
                                                   ShortTime);
        }

        [Test]
        public void Add_LengthLength_Complex_6()
        {
            TimeSpanTestUtilities.Add_LengthLength(LongSpan,
                                                   MusicalSpan,
                                                   TimeSpanTestUtilities.ComplexTempoMap,
                                                   LargeTime);
        }

        #endregion

        #region Subtract

        [Test]
        public void Subtract_SameType_1()
        {
            TimeSpanTestUtilities.Subtract_SameType(new MetricTimeSpan(),
                                                    new MetricTimeSpan(),
                                                    new MetricTimeSpan());
        }

        [Test]
        public void Subtract_SameType_2()
        {
            TimeSpanTestUtilities.Subtract_SameType(new MetricTimeSpan(0, 3, 8, 9),
                                                    new MetricTimeSpan(),
                                                    new MetricTimeSpan(0, 3, 8, 9));
        }

        [Test]
        public void Subtract_SameType_3()
        {
            TimeSpanTestUtilities.Subtract_SameType(new MetricTimeSpan(2, 3, 5),
                                                    new MetricTimeSpan(1, 0, 8, 460),
                                                    new MetricTimeSpan(1, 2, 56, 540));
        }

        [Test]
        public void Subtract_TimeTime_Default_1()
        {
            TimeSpanTestUtilities.Subtract_TimeTime(MusicalSpan,
                                                    ShortSpan,
                                                    TimeSpanTestUtilities.DefaultTempoMap);
        }

        [Test]
        public void Subtract_TimeTime_Simple_1()
        {
            TimeSpanTestUtilities.Subtract_TimeTime(MusicalSpan,
                                                    ShortSpan,
                                                    TimeSpanTestUtilities.SimpleTempoMap);
        }

        [Test]
        public void Subtract_TimeTime_Complex_1()
        {
            TimeSpanTestUtilities.Subtract_TimeTime(MusicalSpan,
                                                    ShortSpan,
                                                    TimeSpanTestUtilities.ComplexTempoMap);
        }

        [Test]
        public void Subtract_TimeLength_Default_1()
        {
            TimeSpanTestUtilities.Subtract_TimeLength(MusicalSpan,
                                                      ShortSpan,
                                                      TimeSpanTestUtilities.DefaultTempoMap);
        }

        [Test]
        public void Subtract_TimeLength_Simple_1()
        {
            TimeSpanTestUtilities.Subtract_TimeLength(MusicalSpan,
                                                      ShortSpan,
                                                      TimeSpanTestUtilities.SimpleTempoMap);
        }

        [Test]
        public void Subtract_TimeLength_Complex_1()
        {
            TimeSpanTestUtilities.Subtract_TimeLength(MusicalSpan,
                                                      ShortSpan,
                                                      TimeSpanTestUtilities.ComplexTempoMap);
        }

        [Test]
        public void Subtract_LengthLength_Default_1()
        {
            TimeSpanTestUtilities.Subtract_LengthLength(MusicalSpan,
                                                        ShortSpan,
                                                        TimeSpanTestUtilities.DefaultTempoMap,
                                                        ZeroTime);
        }

        [Test]
        public void Subtract_LengthLength_Default_2()
        {
            TimeSpanTestUtilities.Subtract_LengthLength(MusicalSpan,
                                                        ShortSpan,
                                                        TimeSpanTestUtilities.DefaultTempoMap,
                                                        ShortTime);
        }

        [Test]
        public void Subtract_LengthLength_Default_3()
        {
            TimeSpanTestUtilities.Subtract_LengthLength(MusicalSpan,
                                                        ShortSpan,
                                                        TimeSpanTestUtilities.DefaultTempoMap,
                                                        LargeTime);
        }

        [Test]
        public void Subtract_LengthLength_Simple_1()
        {
            TimeSpanTestUtilities.Subtract_LengthLength(MusicalSpan,
                                                        ShortSpan,
                                                        TimeSpanTestUtilities.SimpleTempoMap,
                                                        ZeroTime);
        }

        [Test]
        public void Subtract_LengthLength_Simple_2()
        {
            TimeSpanTestUtilities.Subtract_LengthLength(MusicalSpan,
                                                        ShortSpan,
                                                        TimeSpanTestUtilities.SimpleTempoMap,
                                                        ShortTime);
        }

        [Test]
        public void Subtract_LengthLength_Simple_3()
        {
            TimeSpanTestUtilities.Subtract_LengthLength(MusicalSpan,
                                                        ShortSpan,
                                                        TimeSpanTestUtilities.SimpleTempoMap,
                                                        LargeTime);
        }

        [Test]
        public void Subtract_LengthLength_Complex_1()
        {
            TimeSpanTestUtilities.Subtract_LengthLength(MusicalSpan,
                                                        ShortSpan,
                                                        TimeSpanTestUtilities.ComplexTempoMap,
                                                        ZeroTime);
        }

        [Test]
        public void Subtract_LengthLength_Complex_2()
        {
            TimeSpanTestUtilities.Subtract_LengthLength(MusicalSpan,
                                                        ShortSpan,
                                                        TimeSpanTestUtilities.ComplexTempoMap,
                                                        ShortTime);
        }

        [Test]
        public void Subtract_LengthLength_Complex_3()
        {
            TimeSpanTestUtilities.Subtract_LengthLength(MusicalSpan,
                                                        ShortSpan,
                                                        TimeSpanTestUtilities.ComplexTempoMap,
                                                        LargeTime);
        }

        #endregion

        #region Multiply

        [Test]
        [Description("Multiply zero time span by zero.")]
        public void Multiply_1()
        {
            Assert.AreEqual(new MetricTimeSpan(),
                            new MetricTimeSpan().Multiply(0));
        }

        [Test]
        [Description("Multiply arbitrary time span by zero.")]
        public void Multiply_2()
        {
            Assert.AreEqual(new MetricTimeSpan(),
                            new MetricTimeSpan(2, 6, 8, 9).Multiply(0));
        }

        [Test]
        [Description("Multiply by integer number.")]
        public void Multiply_3()
        {
            Assert.AreEqual(new MetricTimeSpan(0, 4, 0, 10),
                            new MetricTimeSpan(0, 2, 0, 5).Multiply(2));
        }

        [Test]
        [Description("Multiply by non-integer number.")]
        public void Multiply_4()
        {
            Assert.AreEqual(new MetricTimeSpan(0, 3, 0, 12),
                            new MetricTimeSpan(0, 2, 0, 8).Multiply(1.5));
        }

        [Test]
        [Description("Multiply by negative number.")]
        public void Multiply_5()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MetricTimeSpan().Multiply(-5));
        }

        #endregion

        #region Divide

        [Test]
        [Description("Divide arbitrary time span by one.")]
        public void Divide_1()
        {
            Assert.AreEqual(new MetricTimeSpan(1234),
                            new MetricTimeSpan(1234).Divide(1));
        }

        [Test]
        [Description("Divide arbitrary time span by integer number.")]
        public void Divide_2()
        {
            Assert.AreEqual(new MetricTimeSpan(0, 1, 0, 0),
                            new MetricTimeSpan(0, 2, 0, 0).Divide(2));
        }

        [Test]
        [Description("Divide by non-integer number.")]
        public void Divide_3()
        {
            Assert.AreEqual(new MetricTimeSpan(824),
                            new MetricTimeSpan(1236).Divide(1.5));
        }

        [Test]
        [Description("Divide by zero.")]
        public void Divide_4()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MetricTimeSpan().Divide(0));
        }

        [Test]
        [Description("Divide by negative number.")]
        public void Divide_5()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MetricTimeSpan().Divide(-8));
        }

        [Test]
        [Description("Divide zero time span by one.")]
        public void Divide_6()
        {
            Assert.AreEqual(new MetricTimeSpan(),
                            new MetricTimeSpan().Divide(1));
        }

        #endregion

        #region Clone

        [Test]
        public void Clone_1()
        {
            TimeSpanTestUtilities.TestClone(new MetricTimeSpan());
        }

        [Test]
        public void Clone_2()
        {
            TimeSpanTestUtilities.TestClone(new MetricTimeSpan(5, 4, 6, 8));
        }

        [Test]
        public void Clone_3()
        {
            TimeSpanTestUtilities.TestClone(new MetricTimeSpan(new TimeSpan(1)));
        }

        #endregion

        #region Compare

        [Test]
        [Description("Compare two time spans where first one is less than second one.")]
        public void Compare_Less()
        {
            foreach (var timeSpansPair in TimeSpansForComparison_Less)
            {
                var timeSpan1 = timeSpansPair.Item1;
                var timeSpan2 = timeSpansPair.Item2;

                Assert.IsTrue(timeSpan1 < timeSpan2,
                              $"{timeSpan1} isn't less than {timeSpan2} using <.");
                Assert.IsTrue(timeSpan1.CompareTo(timeSpan2) < 0,
                              $"{timeSpan1} isn't less than {timeSpan2} using typed CompareTo.");
                Assert.IsTrue(timeSpan1.CompareTo((object)timeSpan2) < 0,
                              $"{timeSpan1} isn't less than {timeSpan2} using CompareTo(object).");
            }
        }

        [Test]
        [Description("Compare two time spans where first one is greater than second one.")]
        public void Compare_Greater()
        {
            foreach (var timeSpansPair in TimeSpansForComparison_Less)
            {
                var timeSpan1 = timeSpansPair.Item2;
                var timeSpan2 = timeSpansPair.Item1;

                Assert.IsTrue(timeSpan1 > timeSpan2,
                              $"{timeSpan1} isn't greater than {timeSpan2} using >.");
                Assert.IsTrue(timeSpan1.CompareTo(timeSpan2) > 0,
                              $"{timeSpan1} isn't greater than {timeSpan2} using typed CompareTo.");
                Assert.IsTrue(timeSpan1.CompareTo((object)timeSpan2) > 0,
                              $"{timeSpan1} isn't greater than {timeSpan2} using CompareTo(object).");
            }
        }

        [Test]
        [Description("Compare two time spans where first one is less than or equal to second one.")]
        public void Compare_LessOrEqual()
        {
            foreach (var timeSpansPair in TimeSpansForComparison_Less.Concat(TimeSpansForComparison_Equal))
            {
                var timeSpan1 = timeSpansPair.Item1;
                var timeSpan2 = timeSpansPair.Item2;

                Assert.IsTrue(timeSpan1 <= timeSpan2,
                              $"{timeSpan1} isn't less than or equal to {timeSpan2} using <=.");
                Assert.IsTrue(timeSpan1.CompareTo(timeSpan2) <= 0,
                              $"{timeSpan1} isn't less than or equal to {timeSpan2} using typed CompareTo.");
                Assert.IsTrue(timeSpan1.CompareTo((object)timeSpan2) <= 0,
                              $"{timeSpan1} isn't less than or equal to {timeSpan2} using CompareTo(object).");
            }
        }

        [Test]
        [Description("Compare two time spans where first one is greater than or equal to second one.")]
        public void Compare_GreaterOrEqual()
        {
            foreach (var timeSpansPair in TimeSpansForComparison_Less.Concat(TimeSpansForComparison_Equal))
            {
                var timeSpan1 = timeSpansPair.Item2;
                var timeSpan2 = timeSpansPair.Item1;

                Assert.IsTrue(timeSpan1 >= timeSpan2,
                              $"{timeSpan1} isn't greater than or equal to {timeSpan2} using >=.");
                Assert.IsTrue(timeSpan1.CompareTo(timeSpan2) >= 0,
                              $"{timeSpan1} isn't greater than {timeSpan2} using typed CompareTo.");
                Assert.IsTrue(timeSpan1.CompareTo((object)timeSpan2) >= 0,
                              $"{timeSpan1} isn't greater than {timeSpan2} using CompareTo(object).");
            }
        }

        [Test]
        [Description("Compare two time spans using CompareTo where second time span is of different type.")]
        public void Compare_TypesMismatch()
        {
            var timeSpansPairs = new[]
            {
                Tuple.Create<MetricTimeSpan, ITimeSpan>(new MetricTimeSpan(), new MidiTimeSpan(100)),
                Tuple.Create<MetricTimeSpan, ITimeSpan>(new MetricTimeSpan(), new MusicalTimeSpan(1, 1000)),
                Tuple.Create<MetricTimeSpan, ITimeSpan>(new MetricTimeSpan(), new BarBeatTicksTimeSpan(1, 2, 3))
            };

            foreach (var timeSpansPair in timeSpansPairs)
            {
                var timeSpan1 = timeSpansPair.Item1;
                var timeSpan2 = timeSpansPair.Item2;

                Assert.Throws<ArgumentException>(() => timeSpan1.CompareTo(timeSpan2));
            }
        }

        [Test]
        [Description("Compare two time spans for equality: true expected.")]
        public void Compare_Equal_True()
        {
            foreach (var timeSpansPair in TimeSpansForComparison_Equal)
            {
                var timeSpan1 = timeSpansPair.Item2;
                var timeSpan2 = timeSpansPair.Item1;

                Assert.IsTrue(timeSpan1 == timeSpan2,
                              $"{timeSpan1} isn't equal to {timeSpan2} using ==.");
                Assert.IsTrue(timeSpan1.Equals(timeSpan2),
                              $"{timeSpan1} isn't equal to {timeSpan2} using typed Equals.");
                Assert.IsTrue(timeSpan1.Equals((object)timeSpan2),
                              $"{timeSpan1} isn't equal to {timeSpan2} using Equals(object).");
            }
        }

        [Test]
        [Description("Compare two time spans for equality: false expected.")]
        public void Compare_Equal_False()
        {
            foreach (var timeSpansPair in TimeSpansForComparison_Less)
            {
                var timeSpan1 = timeSpansPair.Item2;
                var timeSpan2 = timeSpansPair.Item1;

                Assert.IsFalse(timeSpan1 == timeSpan2,
                               $"{timeSpan1} equal to {timeSpan2} using ==.");
                Assert.IsFalse(timeSpan1.Equals(timeSpan2),
                               $"{timeSpan1} equal to {timeSpan2} using typed Equals.");
                Assert.IsFalse(timeSpan1.Equals((object)timeSpan2),
                               $"{timeSpan1} equal to {timeSpan2} using Equals(object).");
            }
        }

        [Test]
        [Description("Compare two time spans for inequality: true expected.")]
        public void Compare_DoesNotEqual_True()
        {
            foreach (var timeSpansPair in TimeSpansForComparison_Less)
            {
                var timeSpan1 = timeSpansPair.Item2;
                var timeSpan2 = timeSpansPair.Item1;

                Assert.IsTrue(timeSpan1 != timeSpan2,
                              $"{timeSpan1} equal to {timeSpan2} using !=.");
                Assert.IsTrue(!timeSpan1.Equals(timeSpan2),
                              $"{timeSpan1} equal to {timeSpan2} using typed Equals.");
                Assert.IsTrue(!timeSpan1.Equals((object)timeSpan2),
                              $"{timeSpan1} equal to {timeSpan2} using Equals(object).");
            }
        }

        [Test]
        [Description("Compare two time spans for inequality: false expected.")]
        public void Compare_DoesNotEqual_False()
        {
            foreach (var timeSpansPair in TimeSpansForComparison_Equal)
            {
                var timeSpan1 = timeSpansPair.Item2;
                var timeSpan2 = timeSpansPair.Item1;

                Assert.IsFalse(timeSpan1 != timeSpan2,
                               $"{timeSpan1} isn't equal to {timeSpan2} using !=.");
                Assert.IsFalse(!timeSpan1.Equals(timeSpan2),
                               $"{timeSpan1} isn't equal to {timeSpan2} using typed Equals.");
                Assert.IsFalse(!timeSpan1.Equals((object)timeSpan2),
                               $"{timeSpan1} isn't equal to {timeSpan2} using Equals(object).");
            }
        }

        #endregion

        #region Divide

        [Test]
        [Description("Divide metric time span by another one.")]
        public void Divide()
        {
            Assert.AreEqual(1, new MetricTimeSpan(0, 0, 2).Divide(new MetricTimeSpan(0, 0, 2)));
            Assert.AreEqual(1.5, new MetricTimeSpan(0, 0, 3).Divide(new MetricTimeSpan(0, 0, 2)));
            Assert.AreEqual(0.5, new MetricTimeSpan(0, 0, 2).Divide(new MetricTimeSpan(0, 0, 4)));

            Assert.Throws<DivideByZeroException>(() => new MetricTimeSpan().Divide(new MetricTimeSpan()));
        }

        #endregion

        #region Properties

        [Test]
        public void CheckTotalMicroseconds_FromTotalMicroseconds([Values(0, 100)] long totalMicroseconds)
        {
            var metricTimeSpan = new MetricTimeSpan(totalMicroseconds);
            Assert.AreEqual(totalMicroseconds, metricTimeSpan.TotalMicroseconds, "Total microseconds value is invalid.");
        }

        [TestCaseSource(nameof(Parameters_CheckTotalMicroseconds_FromTimeSpan))]
        public void CheckTotalMicroseconds_FromTimeSpan(TimeSpan timeSpan, long expectedTotalMicroseconds)
        {
            var metricTimeSpan = new MetricTimeSpan(timeSpan);
            Assert.AreEqual(expectedTotalMicroseconds, metricTimeSpan.TotalMicroseconds, "Total microseconds value is invalid.");
        }

        [TestCaseSource(nameof(Parameters_CheckTotalMicroseconds_FromFields))]
        public void CheckTotalMicroseconds_FromFields(MetricTimeSpan timeSpan, long expectedTotalMicroseconds)
        {
            Assert.AreEqual(expectedTotalMicroseconds, timeSpan.TotalMicroseconds, "Total microseconds value is invalid.");
        }

        [TestCaseSource(nameof(Parameters_CheckTotalMilliseconds_FromTimeSpan))]
        public void CheckTotalMilliseconds_FromTimeSpan(TimeSpan timeSpan, double expectedTotalMilliseconds)
        {
            var metricTimeSpan = new MetricTimeSpan(timeSpan);
            Assert.AreEqual(expectedTotalMilliseconds, metricTimeSpan.TotalMilliseconds, DoubleEpsilon, "Total milliseconds value is invalid.");
        }

        [TestCaseSource(nameof(Parameters_CheckTotalMilliseconds_FromFields))]
        public void CheckTotalMilliseconds_FromFields(MetricTimeSpan timeSpan, double expectedTotalMilliseconds)
        {
            Assert.AreEqual(expectedTotalMilliseconds, timeSpan.TotalMilliseconds, DoubleEpsilon, "Total milliseconds value is invalid.");
        }

        [TestCaseSource(nameof(Parameters_CheckTotalSeconds_FromTimeSpan))]
        public void CheckTotalSeconds_FromTimeSpan(TimeSpan timeSpan, double expectedTotalSeconds)
        {
            var metricTimeSpan = new MetricTimeSpan(timeSpan);
            Assert.AreEqual(expectedTotalSeconds, metricTimeSpan.TotalSeconds, DoubleEpsilon, "Total seconds value is invalid.");
        }

        [TestCaseSource(nameof(Parameters_CheckTotalSeconds_FromFields))]
        public void CheckTotalSeconds_FromFields(MetricTimeSpan timeSpan, double expectedTotalSeconds)
        {
            Assert.AreEqual(expectedTotalSeconds, timeSpan.TotalSeconds, DoubleEpsilon, "Total seconds value is invalid.");
        }

        [TestCaseSource(nameof(Parameters_CheckTotalMinutes_FromTimeSpan))]
        public void CheckTotalMinutes_FromTimeSpan(TimeSpan timeSpan, double expectedTotalMinutes)
        {
            var metricTimeSpan = new MetricTimeSpan(timeSpan);
            Assert.AreEqual(expectedTotalMinutes, metricTimeSpan.TotalMinutes, DoubleEpsilon, "Total minutes value is invalid.");
        }

        [TestCaseSource(nameof(Parameters_CheckTotalMinutes_FromFields))]
        public void CheckTotalMinutes_FromFields(MetricTimeSpan timeSpan, double expectedTotalMinutes)
        {
            Assert.AreEqual(expectedTotalMinutes, timeSpan.TotalMinutes, DoubleEpsilon, "Total minutes value is invalid.");
        }

        [TestCaseSource(nameof(Parameters_CheckTotalHours_FromTimeSpan))]
        public void CheckTotalHours_FromTimeSpan(TimeSpan timeSpan, double expectedTotalHours)
        {
            var metricTimeSpan = new MetricTimeSpan(timeSpan);
            Assert.AreEqual(expectedTotalHours, metricTimeSpan.TotalHours, DoubleEpsilon, "Total hours value is invalid.");
        }

        [TestCaseSource(nameof(Parameters_CheckTotalHours_FromFields))]
        public void CheckTotalHours_FromFields(MetricTimeSpan timeSpan, double expectedTotalHours)
        {
            Assert.AreEqual(expectedTotalHours, timeSpan.TotalHours, DoubleEpsilon, "Total hours value is invalid.");
        }

        [TestCaseSource(nameof(Parameters_CheckTotalDays_FromTimeSpan))]
        public void CheckTotalDays_FromTimeSpan(TimeSpan timeSpan, double expectedTotalDays)
        {
            var metricTimeSpan = new MetricTimeSpan(timeSpan);
            Assert.AreEqual(expectedTotalDays, metricTimeSpan.TotalDays, DoubleEpsilon, "Total days value is invalid.");
        }

        [TestCaseSource(nameof(Parameters_CheckTotalDays_FromFields))]
        public void CheckTotalDays_FromFields(MetricTimeSpan timeSpan, double expectedTotalDays)
        {
            Assert.AreEqual(expectedTotalDays, timeSpan.TotalDays, DoubleEpsilon, "Total days value is invalid.");
        }

        #endregion

        #endregion

        #region Private methods

        private static MidiTimeSpan GetDefaultMidiTimeSpan(MetricTimeSpan timeSpan)
        {
            return new MidiTimeSpan((timeSpan.TotalMicroseconds * TimeSpanTestUtilities.TicksPerQuarterNote) / Tempo.Default.MicrosecondsPerQuarterNote);
        }

        #endregion
    }
}
