using System;
using System.Collections;
using System.Collections.Generic;

namespace Avalonia.Controls.Models
{
    /// <summary>
    /// Provides a base class for read-only lists that implement both generic and non-generic list interfaces.
    /// </summary>
    /// <typeparam name="T">The type of elements contained in the list.</typeparam>
    public abstract class ReadOnlyListBase<T> : IReadOnlyList<T>, IList
    {
        /// <inheritdoc />
        public abstract T this[int index] { get; }

        object? IList.this[int index]
        {
            get => this[index];
            set => throw new NotSupportedException();
        }

        /// <inheritdoc />
        public abstract int Count { get; }

        bool IList.IsFixedSize => false;
        bool IList.IsReadOnly => true;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;

        /// <inheritdoc />
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
