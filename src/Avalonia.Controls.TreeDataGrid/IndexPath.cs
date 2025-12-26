// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Controls
{
    /// <summary>
    ///   Represents a path of indexes used to identify a specific item in a hierarchical data
    ///   structure. Each index in the path represents a level of depth in the hierarchy.
    /// </summary>
    /// <remarks>
    ///   IndexPath is used to specify the location of an item in a hierarchical data structure.
    ///   Each index in the path represents a level of depth in the hierarchy.
    /// 
    ///   <para>
    ///     Consider the following tree structure:
    ///     <code>
    ///       Item A             IndexPath: (0)
    ///       ├─ Item A1         IndexPath: (0, 0)
    ///       └─ Item A2         IndexPath: (0, 1)
    ///       Item B             IndexPath: (1)
    ///       ├─ Item B1         IndexPath: (1, 0)
    ///       │  └─ Item B1a     IndexPath: (1, 0, 0)
    ///       └─ Item B2         IndexPath: (1, 1)
    ///       Item C             IndexPath: (2)
    ///       └─ Item C1         IndexPath: (2, 0)
    ///     </code>
    ///   </para><para>
    ///     Each IndexPath represents the sequence of child indexes needed to navigate from the
    ///     root to reach that specific item in the hierarchy.
    ///   </para>
    /// </remarks>
    public readonly struct IndexPath : IReadOnlyList<int>,
        IComparable<IndexPath>,
        IEquatable<IndexPath>
    {
        /// <summary>
        ///   Gets an empty IndexPath representing no selection.
        /// </summary>
        public static readonly IndexPath Unselected = default;

        private readonly int _index;
        private readonly int[]? _path;

        /// <summary>
        ///   Initializes a new instance of the <see cref="IndexPath" /> struct with a single
        ///   index.
        /// </summary>
        /// <param name="index">The index value.</param>
        public IndexPath(int index)
        {
            _index = index + 1;
            _path = null;
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="IndexPath" /> struct with multiple
        ///   indexes.
        /// </summary>
        /// <param name="indexes">The array of indexes representing the path.</param>
        public IndexPath(params int[] indexes)
        {
            _index = 0;
            _path = indexes;
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="IndexPath" /> struct with indexes from
        ///   an enumerable.
        /// </summary>
        /// <param name="indexes">
        ///   The enumerable collection of indexes, or null for an empty path.
        /// </param>
        public IndexPath(IEnumerable<int>? indexes)
        {
            if (indexes != null)
            {
                _index = 0;
                _path = indexes.ToArray();
            }
            else
            {
                _index = 0;
                _path = null;
            }
        }

        private IndexPath(int[] basePath, int index)
        {
            basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
            
            _index = 0;
            _path = new int[basePath.Length + 1];
            Array.Copy(basePath, _path, basePath.Length);
            _path[basePath.Length] = index;
        }

        /// <summary>
        ///   Gets the number of indexes in this path.
        /// </summary>
        public int Count => _path?.Length ?? (_index == 0 ? 0 : 1);

        /// <summary>
        ///   Gets the index at the specified position in the path.
        /// </summary>
        /// <param name="index">The zero-based position of the index to retrieve.</param>
        /// <returns>The index at the specified position.</returns>
        /// <exception cref="IndexOutOfRangeException">
        ///   Thrown when index is out of range.
        /// </exception>
        public int this[int index]
        {
            get
            {
                if (index >= Count)
                    throw new IndexOutOfRangeException();
                return _path?[index] ?? (_index - 1);
            }
        }

        /// <summary>
        ///   Compares this IndexPath with another IndexPath.
        /// </summary>
        /// <param name="other">The IndexPath to compare with.</param>
        /// <returns>
        ///   A value less than 0 if this path is less than other, 0 if equal, greater than 0
        ///   if greater.
        /// </returns>
        public int CompareTo(IndexPath other)
        {
            var rhsPath = other;
            var compareResult = 0;
            var lhsCount = Count;
            var rhsCount = rhsPath.Count;

            if (lhsCount == 0 || rhsCount == 0)
            {
                // one of the paths are empty, compare based on size
                compareResult = (lhsCount - rhsCount);
            }
            else
            {
                // both paths are non-empty, but can be of different size
                for (var i = 0; i < Math.Min(lhsCount, rhsCount); i++)
                {
                    if (this[i] < rhsPath[i])
                    {
                        compareResult = -1;
                        break;
                    }
                    else if (this[i] > rhsPath[i])
                    {
                        compareResult = 1;
                        break;
                    }
                }

                // if both match upto min(lhsCount, rhsCount), compare based on size
                compareResult = compareResult == 0 ? (lhsCount - rhsCount) : compareResult;
            }

            if (compareResult != 0)
                compareResult = compareResult > 0 ? 1 : -1;

            return compareResult;
        }

        /// <summary>
        ///   Creates a new IndexPath by appending a child index to this path.
        /// </summary>
        /// <param name="childIndex">The child index to append.</param>
        /// <returns>A new IndexPath with the child index appended.</returns>
        /// <exception cref="ArgumentException">
        ///   Thrown when childIndex is negative.
        /// </exception>
        public IndexPath Append(int childIndex)
        {
            if (childIndex < 0)
                throw new ArgumentException("Invalid child index", nameof(childIndex));

            if (_path != null)
                return new IndexPath(_path, childIndex);
            else if (_index != 0)
                return new IndexPath(_index - 1, childIndex);
            else
                return new IndexPath(childIndex);
        }

        /// <summary>
        ///   Returns a string representation of the IndexPath.
        /// </summary>
        /// <returns>
        ///   A string in the format "(index1.index2.index3)" or "()" for empty paths.
        /// </returns>
        public override string ToString()
        {
            if (_path != null)
                return $"({string.Join(".", _path)})";
            else if (_index != 0)
                return $"({_index - 1})";
            else
                return "()";
        }

        /// <summary>
        ///   Determines whether the specified object is equal to this IndexPath.
        /// </summary>
        /// <param name="obj">The object to compare with this IndexPath.</param>
        /// <returns>
        ///   true if the specified object is equal to this IndexPath; otherwise, false.
        /// </returns>
        public override bool Equals(object? obj) => obj is IndexPath other && Equals(other);

        /// <summary>
        ///   Determines whether the specified IndexPath is equal to this IndexPath.
        /// </summary>
        /// <param name="other">The IndexPath to compare with this IndexPath.</param>
        /// <returns>
        ///   true if the specified IndexPath is equal to this IndexPath; otherwise, false.
        /// </returns>
        public bool Equals(IndexPath other) => CompareTo(other) == 0;

        /// <summary>
        ///   Returns an enumerator that iterates through the indexes in this path.
        /// </summary>
        /// <returns>An enumerator for the indexes in this path.</returns>
        public IEnumerator<int> GetEnumerator()
        {
            static IEnumerator<int> EnumerateSingleOrEmpty(int index)
            {
                if (index != 0)
                    yield return index - 1;
            }

            return ((IEnumerable<int>?)_path)?.GetEnumerator() ?? EnumerateSingleOrEmpty(_index);
        }

        /// <summary>
        ///   Returns the hash code for this IndexPath.
        /// </summary>
        /// <returns>A hash code for this IndexPath.</returns>
        public override int GetHashCode()
        {
            var hashCode = -504981047;

            if (_path != null)
            {
                foreach (var i in _path)
                    hashCode = hashCode * -1521134295 + i.GetHashCode();
            }
            else
            {
                hashCode = hashCode * -1521134295 + _index.GetHashCode();
            }

            return hashCode;
        }

        /// <summary>
        ///   Determines whether this IndexPath is an ancestor of the specified IndexPath. An
        ///   ancestor path is one that is a prefix of the other path and shorter.
        /// </summary>
        /// <param name="other">The IndexPath to check.</param>
        /// <returns>
        ///   true if this IndexPath is an ancestor of the other; otherwise, false.
        /// </returns>
        public bool IsAncestorOf(in IndexPath other)
        {
            if (other.Count <= Count)
            {
                return false;
            }

            var size = Count;

            for (var i = 0; i < size; i++)
            {
                if (this[i] != other[i])
                    return false;
            }

            return true;
        }

        /// <summary>
        ///   Determines whether this IndexPath is the direct parent of the specified IndexPath.
        ///   A parent path is exactly one level shorter and matches all preceding indexes.
        /// </summary>
        /// <param name="other">The IndexPath to check.</param>
        /// <returns>
        ///   true if this IndexPath is the direct parent of the other; otherwise, false.
        /// </returns>
        public bool IsParentOf(in IndexPath other)
        {
            var size = Count;

            if (other.Count == size + 1)
            {
                for (var i = 0; i < size; ++i)
                {
                    if (this[i] != other[i])
                        return false;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        ///   Creates a new IndexPath containing a portion of this path.
        /// </summary>
        /// <param name="start">The starting position of the slice.</param>
        /// <param name="length">The number of indexes to include in the slice.</param>
        /// <returns>A new IndexPath containing the specified portion of this path.</returns>
        /// <exception cref="IndexOutOfRangeException">
        ///   Thrown when start or length are invalid.
        /// </exception>
        public IndexPath Slice(int start, int length)
        {
            if (start < 0 || start + length > Count)
                throw new IndexOutOfRangeException("Invalid IndexPath slice.");

            if (length == 0)
                return default;
            if (length == 1)
                return new(this[start]);
            else
            {
                var slice = new int[length];
                Array.Copy(_path!, start, slice, 0, length);
                return new(slice);
            }
        }

        /// <summary>
        ///   Converts this IndexPath to an array of integers.
        /// </summary>
        /// <returns>An array containing all indexes in this path.</returns>
        public int[] ToArray()
        {
            var result = new int[Count];

            if (_path is not null)
                _path.CopyTo(result, 0);
            else if (result.Length > 0)
                result[0] = _index - 1;

            return result;
        }

        /// <summary>
        ///   Returns an enumerator that iterates through the indexes in this path.
        /// </summary>
        /// <returns>An enumerator for the indexes in this path.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        ///   Implicitly converts an integer to an <see cref="IndexPath" />.
        /// </summary>
        /// <param name="index">The index value.</param>
        public static implicit operator IndexPath(int index) => new(index);
        /// <summary>
        ///   Determines whether the first <see cref="IndexPath" /> is less than the second.
        /// </summary>
        public static bool operator <(IndexPath x, IndexPath y) => x.CompareTo(y) < 0;
        /// <summary>
        ///   Determines whether the first <see cref="IndexPath" /> is greater than the second.
        /// </summary>
        public static bool operator >(IndexPath x, IndexPath y) => x.CompareTo(y) > 0;
        /// <summary>
        ///   Determines whether the first <see cref="IndexPath" /> is less than or equal to the second.
        /// </summary>
        public static bool operator <=(IndexPath x, IndexPath y) => x.CompareTo(y) <= 0;
        /// <summary>
        ///   Determines whether the first <see cref="IndexPath" /> is greater than or equal to the second.
        /// </summary>
        public static bool operator >=(IndexPath x, IndexPath y) => x.CompareTo(y) >= 0;
        /// <summary>
        ///   Determines whether two <see cref="IndexPath" /> instances are equal.
        /// </summary>
        public static bool operator ==(IndexPath x, IndexPath y) => x.CompareTo(y) == 0;
        /// <summary>
        ///   Determines whether two <see cref="IndexPath" /> instances are not equal.
        /// </summary>
        public static bool operator !=(IndexPath x, IndexPath y) => x.CompareTo(y) != 0;
        /// <summary>
        ///   Determines whether two nullable <see cref="IndexPath" /> instances are equal.
        /// </summary>
        public static bool operator ==(IndexPath? x, IndexPath? y) => (x ?? default).CompareTo(y ?? default) == 0;
        /// <summary>
        ///   Determines whether two nullable <see cref="IndexPath" /> instances are not equal.
        /// </summary>
        public static bool operator !=(IndexPath? x, IndexPath? y) => (x ?? default).CompareTo(y ?? default) != 0;
    }
}
