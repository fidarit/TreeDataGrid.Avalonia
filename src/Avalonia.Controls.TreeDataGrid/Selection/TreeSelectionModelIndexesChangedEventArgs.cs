using System;

namespace Avalonia.Controls.Selection
{
    /// <summary>
    ///   Holds data for the <see cref="ITreeSelectionModel.IndexesChanged" /> event.
    /// </summary>
    /// <remarks>
    ///   This event is raised when indexes in the selection model change due to insertions,
    ///   removals, or reordering in the underlying data source. It provides information
    ///   about which range of indexes was affected and how they were shifted.
    /// </remarks>
    public class TreeSelectionModelIndexesChangedEventArgs : EventArgs
    {
        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="TreeSelectionModelIndexesChangedEventArgs" /> class.
        /// </summary>
        /// <param name="parentIndex">
        ///   The index path of the parent item whose children were affected.
        /// </param>
        /// <param name="startIndex">The index at which the change started.</param>
        /// <param name="endIndex">The index at which the change ended.</param>
        /// <param name="delta">The number of items added or removed.</param>
        /// <remarks>
        ///   <para>
        ///     The <paramref name="delta" /> parameter will be positive when items are added and
        ///     negative when items are removed.
        ///   </para>
        ///   <para>
        ///     The affected range is from <paramref name="startIndex" /> (inclusive) to
        ///     <paramref name="endIndex" /> (exclusive).
        ///   </para>
        /// </remarks>
        public TreeSelectionModelIndexesChangedEventArgs(
            IndexPath parentIndex,
            int startIndex,
            int endIndex,
            int delta)
        {
            ParentIndex = parentIndex;
            StartIndex = startIndex;
            EndIndex = endIndex;
            Delta = delta;
        }

        /// <summary>
        ///   Gets the index of the parent item.
        /// </summary>
        /// <remarks>
        ///   For changes at the root level, this will be an empty index path.
        /// </remarks>
        public IndexPath ParentIndex { get; }

        /// <summary>
        ///   Gets the inclusive start index of the range of indexes that changed.
        /// </summary>
        /// <remarks>
        ///   This is the first index affected by the change.
        /// </remarks>
        public int StartIndex { get; }

        /// <summary>
        ///   Gets the exclusive end index of the range of indexes that changed.
        /// </summary>
        /// <remarks>
        ///   This is one past the last index affected by the change. The range of affected
        ///   indexes is from <see cref="StartIndex" /> (inclusive) to <see cref="EndIndex" />
        ///   (exclusive).
        /// </remarks>
        public int EndIndex { get; }

        /// <summary>
        ///   Gets the delta of the change; i.e. the number of indexes added or removed.
        /// </summary>
        /// <value>
        ///   A positive value indicates items were added; a negative value indicates items were
        ///   removed.
        /// </value>
        /// <remarks>
        ///   <para>
        ///     When items are added, <see cref="Delta" /> represents the number of items inserted.
        ///   </para>
        ///   <para>
        ///     When items are removed, <see cref="Delta" /> is negative and its absolute value
        ///     represents the number of items removed. This value is used to adjust index paths that
        ///     point to items after the change.
        ///   </para>
        /// </remarks>
        public int Delta { get; }
    }
}
