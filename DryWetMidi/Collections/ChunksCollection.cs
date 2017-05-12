﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Melanchall.DryWetMidi
{
    /// <summary>
    /// Collection of <see cref="MidiChunk"/> objects.
    /// </summary>
    public sealed class ChunksCollection : IEnumerable<MidiChunk>
    {
        #region Fields

        private readonly List<MidiChunk> _chunks = new List<MidiChunk>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the chunk at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the chunk to get or set.</param>
        /// <returns>The chunk at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index"/> is less than 0; or <paramref name="index"/> is equal to or greater than
        /// <see cref="Count"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">value is null</exception>
        public MidiChunk this[int index]
        {
            get
            {
                if (index < 0 || index >= _chunks.Count)
                    throw new ArgumentOutOfRangeException(nameof(index), index, "Index is out of range.");

                return _chunks[index];
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (index < 0 || index >= _chunks.Count)
                    throw new ArgumentOutOfRangeException(nameof(index), index, "Index is out of range.");

                _chunks[index] = value;
            }
        }

        /// <summary>
        /// Gets the number of chunks contained in the collection.
        /// </summary>
        public int Count => _chunks.Count;

        #endregion

        #region Methods

        /// <summary>
        /// Adds a chunk to the end of the collection.
        /// </summary>
        /// <param name="chunk">The chunk to be added to the end of the collection.</param>
        /// <remarks>
        /// Note that header chunks cannot be added into the collection since it may cause inconsistence in the file structure.
        /// Header chunk with appropriate information will be written to a file automatically on
        /// <see cref="MidiFile.Write(string, bool, MidiFileFormat, WritingSettings)"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="chunk"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="chunk"/> is an instance of <see cref="HeaderChunk"/>.</exception>
        public void Add(MidiChunk chunk)
        {
            if (chunk == null)
                throw new ArgumentNullException(nameof(chunk));

            if (chunk is HeaderChunk)
                throw new ArgumentException("Header chunk cannot be added to chunks collection.", nameof(chunk));

            _chunks.Add(chunk);
        }

        /// <summary>
        /// Adds chunks the end of the collection.
        /// </summary>
        /// <param name="chunks">Chunks to add to the collection.</param>
        /// <remarks>
        /// Note that header chunks cannot be added into the collection since it may cause inconsistence in the file structure.
        /// Header chunk with appropriate information will be written to a file automatically on
        /// <see cref="MidiFile.Write(string, bool, MidiFileFormat, WritingSettings)"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="chunks"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="chunks"/> contain an instance of <see cref="HeaderChunk"/>; or
        /// <paramref name="chunks"/> contain null.</exception>
        public void AddRange(IEnumerable<MidiChunk> chunks)
        {
            if (chunks == null)
                throw new ArgumentNullException(nameof(chunks));

            if (chunks.Any(c => c is HeaderChunk))
                throw new ArgumentException("Header chunk cannot be added to chunks collection.", nameof(chunks));

            if (chunks.Any(c => c == null))
                throw new ArgumentException("Null cannot be added to chunks collection.", nameof(chunks));

            _chunks.AddRange(chunks);
        }

        /// <summary>
        /// Inserts a chunk into the collection at the specified index.
        /// </summary>
        /// <remarks>
        /// Note that header chunks cannot be inserted into the collection since it may cause inconsistence in the file structure.
        /// Header chunk with appropriate information will be written to a file automatically on
        /// <see cref="MidiFile.Write(string, bool, MidiFileFormat, WritingSettings)"/>.
        /// </remarks>
        /// <param name="index">The zero-based index at which the chunk should be inserted.</param>
        /// <param name="chunk">The chunk to be added to the end of the collection.</param>
        /// <exception cref="ArgumentNullException"><paramref name="chunk"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="chunk"/> is an instance of <see cref="HeaderChunk"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0; or
        /// <paramref name="index"/> is greater than <see cref="Count"/>.</exception>
        public void Insert(int index, MidiChunk chunk)
        {
            if (chunk == null)
                throw new ArgumentNullException(nameof(chunk));

            if (chunk is HeaderChunk)
                throw new ArgumentException("Header chunk cannot be inserted to chunks collection.", nameof(chunk));

            if (index < 0 || index >= _chunks.Count)
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index is out of range.");

            _chunks.Insert(index, chunk);
        }


        /// <summary>
        /// Removes the first occurrence of a specific chunk from the collection.
        /// </summary>
        /// <param name="chunk">The chunk to remove from the collection. The value cannot be null.</param>
        /// <returns>true if chunk is successfully removed; otherwise, false. This method also returns
        /// false if chunk was not found in the collection.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="chunk"/> is null.</exception>
        public bool Remove(MidiChunk chunk)
        {
            if (chunk == null)
                throw new ArgumentNullException(nameof(chunk));

            return _chunks.Remove(chunk);
        }

        /// <summary>
        /// Removes the chunk at the specified index of the collection.
        /// </summary>
        /// <param name="index">The zero-based index of the chunk to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0; or <paramref name="index"/>
        /// is equal to or greater than <see cref="Count"/>.</exception>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _chunks.Count)
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index is out of range.");

            _chunks.RemoveAt(index);
        }

        /// <summary>
        /// Removes all the chunks that match the conditions defined by the specified predicate.
        /// </summary>
        /// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the conditions
        /// of the chunks to remove.</param>
        /// <returns>The number of chunks removed from the <see cref="ChunksCollection"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="match"/> is null.</exception>
        public int RemoveAll(Predicate<MidiChunk> match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));

            return _chunks.RemoveAll(match);
        }

        #endregion

        #region IEnumerable<Chunk>

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="ChunksCollection"/>.
        /// </summary>
        /// <returns>An enumerator for the <see cref="ChunksCollection"/>.</returns>
        public IEnumerator<MidiChunk> GetEnumerator()
        {
            return _chunks.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="ChunksCollection"/>.
        /// </summary>
        /// <returns>An enumerator for the <see cref="ChunksCollection"/>.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _chunks.GetEnumerator();
        }

        #endregion
    }
}
