using System;
using System.Collections;
using System.Collections.Generic;

namespace Avalonia.Controls.Selection
{
    /// <summary>
    ///   Provides data for the <see cref="ITreeSelectionModel.SelectionChanged" /> event.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is the base class for selection change events in hierarchical selection models.
    ///     It provides information about which items were selected and deselected, including both
    ///     their index paths in the hierarchy and the actual item objects.
    ///   </para>
    ///   <para>
    ///     This base class provides non-generic access to selection change information, while the
    ///     generic version <see cref="TreeSelectionModelSelectionChangedEventArgs{T}" /> provides
    ///     strongly-typed access to the same information.
    ///   </para>
    ///   <para>
    ///     Important: When items are removed from the source collection, they no longer have valid
    ///     index paths and therefore won't appear in <see cref="DeselectedIndexes" />. However, the
    ///     removed items will still be available in <see cref="DeselectedItems" />. This behavior
    ///     ensures that handlers can access the removed items even though their positions in the
    ///     collection are no longer valid.
    ///   </para>
    /// </remarks>
    public abstract class TreeSelectionModelSelectionChangedEventArgs : EventArgs
    {
        /// <summary>
        ///   Gets the indexes of the items that were removed from the selection.
        /// </summary>
        /// <value>
        ///   A read-only list of <see cref="IndexPath" /> objects representing the positions
        ///   in the hierarchy of the items that were deselected.
        /// </value>
        /// <remarks>
        ///   Note that when items are removed from the source collection, they will not appear
        ///   in this collection because they no longer have valid index paths. Use
        ///   <see cref="DeselectedItems" /> to access items that were deselected due to being removed
        ///   from the collection.
        /// </remarks>
        public abstract IReadOnlyList<IndexPath> DeselectedIndexes { get; }

        /// <summary>
        ///   Gets the indexes of the items that were added to the selection.
        /// </summary>
        /// <value>
        ///   A read-only list of <see cref="IndexPath" /> objects representing the positions
        ///   in the hierarchy of the items that were selected.
        /// </value>
        public abstract IReadOnlyList<IndexPath> SelectedIndexes { get; }

        /// <summary>
        ///   Gets the items that were removed from the selection.
        /// </summary>
        /// <value>
        ///   A read-only list of the item objects that were deselected.
        /// </value>
        /// <remarks>
        ///   <para>
        ///     This property provides non-generic access to the deselected items.
        ///   </para>
        ///   <para>
        ///     Unlike <see cref="DeselectedIndexes" />, this collection includes items that were
        ///     deselected  because they were removed from the source collection. This is important
        ///     when handling selection changes caused by collection modifications.
        ///   </para>
        /// </remarks>
        public IReadOnlyList<object?> DeselectedItems => GetUntypedDeselectedItems();

        /// <summary>
        ///   Gets the items that were added to the selection.
        /// </summary>
        /// <value>
        ///   A read-only list of the item objects that were selected.
        /// </value>
        /// <remarks>
        ///   This property provides non-generic access to the selected items.
        /// </remarks>
        public IReadOnlyList<object?> SelectedItems => GetUntypedSelectedItems();

        protected abstract IReadOnlyList<object?> GetUntypedDeselectedItems();
        protected abstract IReadOnlyList<object?> GetUntypedSelectedItems();
    }

    /// <summary>
    ///   Provides strongly-typed data for the
    ///   <see cref="TreeSelectionModelBase{T}.SelectionChanged" /> event.
    /// </summary>
    /// <typeparam name="T">The type of items in the selection model.</typeparam>
    /// <remarks>
    ///   <para>
    ///     This class extends <see cref="TreeSelectionModelSelectionChangedEventArgs" /> to provide
    ///     strongly-typed access to the selected and deselected items, making it easier to work
    ///     with the specific item types in the selection model.
    ///   </para>
    ///   <para>
    ///     It provides information about which items were selected and deselected, including both
    ///     their index paths in the hierarchy and the actual item objects.
    ///   </para>
    ///   <para>
    ///     Important: When items are removed from the source collection, they no longer have valid
    ///     index paths and therefore won't appear in <see cref="TreeSelectionModelSelectionChangedEventArgs{T}.DeselectedIndexes" />. However, the
    ///     removed items will still be available in <see cref="TreeSelectionModelSelectionChangedEventArgs{T}.DeselectedItems" />. This behavior
    ///     ensures that handlers can access the removed items even though their positions in the
    ///     collection are no longer valid.
    ///   </para>
    /// </remarks>
    public class TreeSelectionModelSelectionChangedEventArgs<T> : TreeSelectionModelSelectionChangedEventArgs
    {
        private IReadOnlyList<object?>? _deselectedItems;
        private IReadOnlyList<object?>? _selectedItems;

        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="TreeSelectionModelSelectionChangedEventArgs{T}" /> class.
        /// </summary>
        /// <param name="deselectedIndexes">
        ///   The indexes of items that were deselected, or null if none.
        /// </param>
        /// <param name="selectedIndexes">
        ///   The indexes of items that were selected, or null if none.
        /// </param>
        /// <param name="deselectedItems">The items that were deselected, or null if none.</param>
        /// <param name="selectedItems">The items that were selected, or null if none.</param>
        /// <remarks>
        ///   <para>
        ///     If any parameter is null, an empty collection will be used instead.
        ///   </para>
        ///   <para>
        ///     Note that <paramref name="deselectedItems" /> may contain items that were removed from
        ///     the source collection, which won't have corresponding entries in <paramref name="deselectedIndexes" />.
        ///   </para>
        /// </remarks>
        public TreeSelectionModelSelectionChangedEventArgs(
            IReadOnlyList<IndexPath>? deselectedIndexes = null,
            IReadOnlyList<IndexPath>? selectedIndexes = null,
            IReadOnlyList<T?>? deselectedItems = null,
            IReadOnlyList<T?>? selectedItems = null)
        {
            DeselectedIndexes = deselectedIndexes ?? [];
            SelectedIndexes = selectedIndexes ?? [];
            DeselectedItems = deselectedItems ?? [];
            SelectedItems = selectedItems ?? [];
        }

        /// <summary>
        ///   Gets the indexes of the items that were removed from the selection.
        /// </summary>
        /// <value>
        ///   A read-only list of <see cref="IndexPath" /> objects representing the positions
        ///   in the hierarchy of the items that were deselected.
        /// </value>
        /// <remarks>
        ///   Note that when items are removed from the source collection, they will not appear
        ///   in this collection because they no longer have valid index paths. Use
        ///   <see cref="TreeSelectionModelSelectionChangedEventArgs{T}.DeselectedItems" /> to access items that were deselected due to being removed
        ///   from the collection.
        /// </remarks>
        public override IReadOnlyList<IndexPath> DeselectedIndexes { get; }

        /// <summary>
        ///   Gets the indexes of the items that were added to the selection.
        /// </summary>
        /// <value>
        ///   A read-only list of <see cref="IndexPath" /> objects representing the positions
        ///   in the hierarchy of the items that were selected.
        /// </value>
        /// <remarks>
        ///   This property overrides the base class property to provide the specific
        ///   implementation for this event args class.
        /// </remarks>
        public override IReadOnlyList<IndexPath> SelectedIndexes { get; }

        /// <summary>
        ///   Gets the strongly-typed items that were removed from the selection.
        /// </summary>
        /// <value>
        ///   A read-only list of the items that were deselected, typed as <typeparamref name="T" />.
        /// </value>
        /// <remarks>
        ///   <para>
        ///     This property hides the base class property to provide strongly-typed access
        ///     to the deselected items.
        ///   </para>
        ///   <para>
        ///     Unlike <see cref="TreeSelectionModelSelectionChangedEventArgs{T}.DeselectedIndexes" />, this collection includes items that were
        ///     deselected  because they were removed from the source collection. This is important
        ///     when handling selection changes caused by collection modifications.
        ///   </para>
        /// </remarks>
        public new IReadOnlyList<T?> DeselectedItems { get; }

        /// <summary>
        ///   Gets the strongly-typed items that were added to the selection.
        /// </summary>
        /// <value>
        ///   A read-only list of the items that were selected, typed as <typeparamref name="T" />.
        /// </value>
        /// <remarks>
        ///   This property hides the base class property to provide strongly-typed access
        ///   to the selected items.
        /// </remarks>
        public new IReadOnlyList<T?> SelectedItems { get; }

        protected override IReadOnlyList<object?> GetUntypedDeselectedItems()
        {
            return _deselectedItems ??= (DeselectedItems as IReadOnlyList<object?>) ??
                new Untyped(DeselectedItems);
        }

        protected override IReadOnlyList<object?> GetUntypedSelectedItems()
        {
            return _selectedItems ??= (SelectedItems as IReadOnlyList<object?>) ??
                new Untyped(SelectedItems);
        }

        private class Untyped : IReadOnlyList<object?>
        {
            private readonly IReadOnlyList<T?> _source;
            public Untyped(IReadOnlyList<T?> source) => _source = source;
            public object? this[int index] => _source[index];
            public int Count => _source.Count;
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public IEnumerator<object?> GetEnumerator()
            {
                foreach (var i in _source)
                {
                    yield return i;
                }
            }
        }
    }
}
