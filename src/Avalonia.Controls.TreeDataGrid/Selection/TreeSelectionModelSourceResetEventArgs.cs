using System;

namespace Avalonia.Controls.Selection
{
    /// <summary>
    ///   Provides data for the <see cref="TreeSelectionModelBase{T}.SourceReset" /> event.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This event is raised when a collection in the hierarchical data structure is reset,
    ///     such as when the collection is cleared or replaced with a new collection.
    ///   </para>
    ///   <para>
    ///     The event provides information about which part of the hierarchical structure was
    ///     reset through the <see cref="ParentIndex" /> property, allowing handlers to determine
    ///     whether the entire source was reset or just a subtree within the hierarchy.
    ///   </para>
    /// </remarks>
    public class TreeSelectionModelSourceResetEventArgs : EventArgs
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="TreeSelectionModelSourceResetEventArgs" /> class.
        /// </summary>
        /// <param name="parentIndex">
        ///   The index path to the parent item whose children collection was reset.
        /// </param>
        /// <remarks>
        ///   <para>
        ///     When the root collection is reset, <paramref name="parentIndex" /> will be empty.
        ///   </para>
        ///   <para>
        ///     When a child collection is reset, <paramref name="parentIndex" /> will contain
        ///     the path to the parent item containing that child collection.
        ///   </para>
        /// </remarks>
        public TreeSelectionModelSourceResetEventArgs(IndexPath parentIndex)
        {
            ParentIndex = parentIndex;
        }

        /// <summary>
        ///   Gets the index path to the parent item whose children collection was reset.
        /// </summary>
        /// <value>
        ///   An <see cref="IndexPath" /> identifying the parent item in the hierarchy.
        /// </value>
        /// <remarks>
        ///   <para>
        ///     When the root collection is reset, this property will be an empty path.
        ///   </para>
        ///   <para>
        ///     When a child collection is reset, this property will contain the path to
        ///     the parent item containing that child collection.
        ///   </para>
        ///   <para>
        ///     This information is useful for determining which part of the selection may
        ///     have been affected by the reset and may need to be updated or restored.
        ///   </para>
        /// </remarks>
        public IndexPath ParentIndex { get; }
    }
}
