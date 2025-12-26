using System;
using System.Collections;
using System.Collections.Generic;

namespace Avalonia.Controls.Models
{
    /// <summary>
    ///   Base class implementing a read-only list collection.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <remarks>
    ///   <para>
    ///     This class should be considered internal to the TreeDataGrid package and is not intended
    ///     for use outside of this package.
    ///   </para>
    ///   <para>
    ///     Provides a base implementation for read-only list collections, implementing both
    ///     <see cref="IReadOnlyList{T}" /> and <see cref="IList" /> interfaces.
    ///   </para>
    /// </remarks>
    public abstract class ReadOnlyListBase<T> : IReadOnlyList<T>, IList
    {
        /// <summary>
        ///   Gets the element at the specified index in the read-only list.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <returns>The element at the specified index.</returns>
        public abstract T this[int index] { get; }
        
        /// <summary>
        ///   Gets the element at the specified index in the read-only list.
        /// </summary>
        /// <exception cref="NotSupportedException">
        ///   Always thrown from the setter as the list is read-only.
        /// </exception>
        object? IList.this[int index] 
        {
            get => this[index];
            set => throw new NotSupportedException();
        }

        /// <summary>
        ///   Gets the number of elements in the collection.
        /// </summary>
        public abstract int Count { get; }

        bool IList.IsFixedSize => false;
        bool IList.IsReadOnly => true;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public abstract IEnumerator<T> GetEnumerator();

        int IList.Add(object? value) => throw new NotSupportedException();
        void IList.Clear() => throw new NotSupportedException();
        void IList.Insert(int index, object? value) => throw new NotSupportedException();
        void IList.Remove(object? value) => throw new NotSupportedException();
        void IList.RemoveAt(int index) => throw new NotSupportedException();
        bool IList.Contains(object? value) => ((IList)this).IndexOf(value) != -1;
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void ICollection.CopyTo(Array array, int index)
        {
            for (var i = 0; i < Count; ++i)
                array.SetValue(this[i], i + index);
        }

        int IList.IndexOf(object? value)
        {
            for (var i = 0; i < Count; ++i)
            {
                if (Equals(this[i], value))
                    return i;
            }

            return -1;
        }
    }
}
