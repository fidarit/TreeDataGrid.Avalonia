using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Controls.Utils;
using Avalonia.Utilities;

namespace Avalonia.Controls.Models.TreeDataGrid
{
    /// <summary>
    ///   Manages a collection of hierarchical rows in a
    ///   <see cref="HierarchicalTreeDataGridSource{TModel}" />.
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <remarks>
    ///   <para>
    ///     The <see cref="HierarchicalRows{TModel}" /> class is responsible for maintaining a flattened
    ///     view of hierarchical data for display in a TreeDataGrid. It manages the relationships
    ///     between rows, handles expansion and collapse operations, and synchronizes the view with the
    ///     underlying data source.
    ///   </para>
    ///   <para>
    ///     This class serves as both a collection of rows and a controller for row operations. It
    ///     maintains a flattened list of rows that represents the current visual state of the tree,
    ///     taking into account which nodes are expanded or collapsed.
    ///   </para>
    ///   <para>
    ///     When rows are expanded or collapsed, this class manages the insertion and removal of child
    ///     rows from the flattened list and raises the appropriate collection changed events.
    ///   </para>
    /// </remarks>
    public class HierarchicalRows<TModel> : ReadOnlyListBase<HierarchicalRow<TModel>>,
        IRows,
        IDisposable,
        IExpanderRowController<TModel>
    {
        private readonly IExpanderRowController<TModel> _controller;
        private readonly RootRows _roots;
        private readonly IExpanderColumn<TModel> _expanderColumn;
        private readonly List<HierarchicalRow<TModel>> _flattenedRows;
        private Comparison<TModel>? _comparison;
        private bool _ignoreCollectionChanges;

        /// <summary>
        ///   Initializes a new instance of the <see cref="HierarchicalRows{TModel}" /> class.
        /// </summary>
        /// <param name="controller">
        ///   The parent controller that will receive row state change notifications.
        /// </param>
        /// <param name="items">The source collection of items to display.</param>
        /// <param name="expanderColumn">The column that provides expansion functionality.</param>
        /// <param name="comparison">An optional comparison function for sorting rows.</param>
        /// <remarks>
        ///   <para>
        ///     This constructor initializes the row collection from the provided source items,
        ///     using the expander column to determine the hierarchical structure.
        ///   </para>
        ///   <para>
        ///     The <paramref name="controller" /> parameter allows the rows to notify an external
        ///     component (typically the <see cref="HierarchicalTreeDataGridSource{TModel}" />) of
        ///     expansion and collection change events.
        ///   </para>
        /// </remarks>
        public HierarchicalRows(
            IExpanderRowController<TModel> controller,
            TreeDataGridItemsSourceView<TModel> items,
            IExpanderColumn<TModel> expanderColumn,
            Comparison<TModel>? comparison)
        {
            _controller = controller;
            _flattenedRows = [];
            _roots = new RootRows(this, items, comparison);
            _roots.CollectionChanged += OnRootsCollectionChanged;
            _expanderColumn = expanderColumn;
            _comparison = comparison;
            InitializeRows();
        }

        /// <summary>
        ///   Gets the row at the specified index in the flattened collection.
        /// </summary>
        /// <param name="index">The index of the row to retrieve.</param>
        /// <returns>
        ///   The <see cref="HierarchicalRow{TModel}" /> at the specified index.
        /// </returns>
        /// <remarks>
        ///   This indexer provides access to rows as they appear in the flattened view of the tree,
        ///   where expanded parent rows are followed immediately by their children.
        /// </remarks>
        public override HierarchicalRow<TModel> this[int index] => _flattenedRows[index];
        /// <summary>
        ///   Gets the row at the specified index for the <see cref="IReadOnlyList{T}" /> interface.
        /// </summary>
        IRow IReadOnlyList<IRow>.this[int index] => _flattenedRows[index];
        /// <summary>
        ///   Gets the number of rows in the flattened collection.
        /// </summary>
        /// <remarks>
        ///   This count represents the total number of visible rows in the tree, including
        ///   both root rows and the expanded children of those rows.
        /// </remarks>
        public override int Count => _flattenedRows.Count;

        /// <summary>
        ///   Releases resources used by the <see cref="HierarchicalRows{TModel}" />.
        /// </summary>
        /// <remarks>
        ///   Disposes the root rows collection and clears all event subscriptions.
        /// </remarks>
        public void Dispose()
        {
            _ignoreCollectionChanges = true;
            _roots.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///   Expands a row identified by its index path.
        /// </summary>
        /// <param name="index">The index path identifying the row to expand.</param>
        /// <remarks>
        ///   <para>
        ///     This method navigates through the tree structure using the index path to find
        ///     the target row, then expands it to show its children.
        ///   </para>
        ///   <para>
        ///     If any part of the path cannot be found, the operation silently fails.
        ///   </para>
        /// </remarks>
        public void Expand(IndexPath index)
        {
            var count = index.Count;
            var rows = (IReadOnlyList<HierarchicalRow<TModel>>?)_roots;

            for (var i = 0; i < count; ++i)
            {
                if (rows is null)
                    break;

                var modelIndex = index[i];
                var found = false;

                foreach (var row in rows)
                {
                    if (row.ModelIndex == modelIndex)
                    {
                        row.IsExpanded = true;
                        rows = row.Children;
                        found = true;
                        break;
                    }
                }

                if (!found)
                    break;
            }
        }

        internal void ExpandCollapseRecursive(Func<TModel, bool> predicate, HierarchicalRow<TModel>? row = null)
        {
            _ignoreCollectionChanges = true;

            try 
            {
                if (row is not null)
                    row.IsExpanded = predicate(row.Model);

                var children = row is null ? _roots : row.Children;

                if (children is not null)
                    ExpandCollapseRecursiveCore(children, predicate); 
            }
            finally 
            { 
                _ignoreCollectionChanges = false; 
            }

            _flattenedRows.Clear();
            InitializeRows();
            CollectionChanged?.Invoke(this, CollectionExtensions.ResetEvent);
        }

        /// <summary>
        ///   Collapses a row identified by its index path.
        /// </summary>
        /// <param name="index">The index path identifying the row to collapse.</param>
        /// <remarks>
        ///   <para>
        ///     This method navigates through the tree structure using the index path to find the
        ///     target row, then collapses it to hide its children.
        ///   </para>
        ///   <para>
        ///     If any part of the path cannot be found, the operation silently fails.
        ///   </para>
        /// </remarks>
        public void Collapse(IndexPath index)
        {
            var count = index.Count;
            var rows = (IReadOnlyList<HierarchicalRow<TModel>>?)_roots;

            for (var i = 0; i < count; ++i)
            {
                if (rows is null)
                    break;

                var modelIndex = index[i];
                var found = false;

                foreach (var row in rows)
                {
                    if (row.ModelIndex == modelIndex)
                    {
                        if (i == count - 1)
                            row.IsExpanded = false;
                        rows = row.Children;
                        found = true;
                        break;
                    }
                }

                if (!found)
                    break;
            }
        }

        /// <summary>
        ///   Gets the row index and vertical position for a given vertical offset.
        /// </summary>
        /// <param name="y">The vertical offset in pixels.</param>
        /// <returns>
        ///   A tuple containing the row index and the vertical position of the row.
        /// </returns>
        /// <remarks>
        ///   This method is used for virtualization to determine which row should be displayed at a
        ///   particular scroll position.
        /// </remarks>
        public (int index, double y) GetRowAt(double y)
        {
            if (MathUtilities.IsZero(y))
                return (0, 0);
            return (-1, -1);
        }

        /// <summary>
        ///   Creates a cell for the specified column at the specified row index.
        /// </summary>
        /// <param name="column">The column for which to create a cell.</param>
        /// <param name="columnIndex">The index of the column.</param>
        /// <param name="rowIndex">The index of the row.</param>
        /// <returns>A new cell instance.</returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown when the column is not compatible with the model type.
        /// </exception>
        /// <remarks>
        ///   This method is used during UI virtualization to create cell instances as they become
        ///   visible. The created cells are bound to the appropriate row model.
        /// </remarks>
        public ICell RealizeCell(IColumn column, int columnIndex, int rowIndex)
        {
            if (column is IColumn<TModel> c)
                return c.CreateCell(this[rowIndex]);
            else
                throw new InvalidOperationException("Invalid column.");
        }

        /// <summary>
        ///   Updates the source collection of items.
        /// </summary>
        /// <param name="items">The new source collection.</param>
        /// <remarks>
        ///   <para>
        ///     This method replaces the current source collection with the specified new collection.
        ///     All rows are rebuilt and the collection changed event is raised.
        ///   </para>
        ///   <para>
        ///     This method is used when the entire data source is replaced.
        ///   </para>
        /// </remarks>
        public void SetItems(TreeDataGridItemsSourceView<TModel> items)
        {
            _ignoreCollectionChanges = true;
            
            try {_roots.SetItems(items); }
            finally { _ignoreCollectionChanges = false; }
            
            _flattenedRows.Clear();
            InitializeRows();
            CollectionChanged?.Invoke(this, CollectionExtensions.ResetEvent);
        }

        /// <summary>
        ///   Sorts the rows using the specified comparison function.
        /// </summary>
        /// <param name="comparison">
        ///   The comparison function to use for sorting, or null to restore the original order.
        /// </param>
        /// <remarks>
        ///   <para>
        ///     This method updates the sort criteria for all rows and rebuilds the flattened
        ///     collection.
        ///   </para>
        ///   <para>
        ///     The sort affects both the root rows and all child rows at every level of the hierarchy.
        ///   </para>
        /// </remarks>
        public void Sort(Comparison<TModel>? comparison)
        {
            _comparison = comparison;
            _roots.Sort(comparison);
            _flattenedRows.Clear();
            InitializeRows();
            CollectionChanged?.Invoke(this, CollectionExtensions.ResetEvent);

            foreach (var row in _roots)
            {
                row.SortChildren(comparison);
            }
        }

        /// <summary>
        ///   Releases resources used by a cell when it is no longer visible.
        /// </summary>
        /// <param name="cell">The cell to unrealize.</param>
        /// <param name="rowIndex">The index of the row that the cell belongs to.</param>
        /// <param name="columnIndex">The index of the column that the cell belongs to.</param>
        /// <remarks>
        ///   This method is called during UI virtualization when a cell scrolls out of view.
        ///   If the cell implements <see cref="IDisposable" />, it is disposed.
        /// </remarks>
        public void UnrealizeCell(ICell cell, int rowIndex, int columnIndex)
        {
            (cell as IDisposable)?.Dispose();
        }

        /// <summary>
        ///   Gets the row index of a parent row given a child's model index.
        /// </summary>
        /// <param name="modelIndex">The model index of a child row.</param>
        /// <returns>The row index of the parent row, or -1 if not found.</returns>
        /// <remarks>
        ///   This method is useful for operations that need to work with a child row's parent,
        ///   such as expanding to reveal a specific item.
        /// </remarks>
        public int GetParentRowIndex(IndexPath modelIndex)
        {
            return ModelIndexToRowIndex(modelIndex[..^1]);
        }

        /// <summary>
        ///   Converts a model index path to a flattened row index.
        /// </summary>
        /// <param name="modelIndex">The model index path.</param>
        /// <returns>The corresponding row index in the flattened collection, or -1 if not found.</returns>
        /// <remarks>
        ///   This method is used to locate a row in the flattened collection when only its
        ///   hierarchical position (model index path) is known.
        /// </remarks>
        public int ModelIndexToRowIndex(IndexPath modelIndex)
        {
            if (modelIndex == default)
                return -1;
            
            for (var i = 0; i < _flattenedRows.Count; ++i)
            {
                if (_flattenedRows[i].ModelIndexPath == modelIndex)
                    return i;
            }

            return -1;
        }

        /// <summary>
        ///   Converts a flattened row index to a model index path.
        /// </summary>
        /// <param name="rowIndex">The row index in the flattened collection.</param>
        /// <returns>The corresponding model index path, or an empty path if not found.</returns>
        /// <remarks>
        ///   This method is used to determine the hierarchical position of a row when only its
        ///   position in the flattened collection is known.
        /// </remarks>
        public IndexPath RowIndexToModelIndex(int rowIndex)
        {
            if (rowIndex >= 0 && rowIndex < _flattenedRows.Count)
                return _flattenedRows[rowIndex].ModelIndexPath;
            return default;
        }

        /// <inheritdoc />
        public override IEnumerator<HierarchicalRow<TModel>> GetEnumerator() => _flattenedRows.GetEnumerator();
        IEnumerator<IRow> IEnumerable<IRow>.GetEnumerator() => _flattenedRows.GetEnumerator();

        /// <summary>
        ///   Occurs when the contents of the flattened collection change.
        /// </summary>
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        void IExpanderRowController<TModel>.OnBeginExpandCollapse(IExpanderRow<TModel> row)
        {
            _controller.OnBeginExpandCollapse(row);
        }

        void IExpanderRowController<TModel>.OnEndExpandCollapse(IExpanderRow<TModel> row)
        {
            _controller.OnEndExpandCollapse(row);
        }

        void IExpanderRowController<TModel>.OnChildCollectionChanged(
            IExpanderRow<TModel> row,
            NotifyCollectionChangedEventArgs e)
        {
            if (_ignoreCollectionChanges)
                return;

            if (row is HierarchicalRow<TModel> h)
                OnCollectionChanged(h.ModelIndexPath, e);
            else
                throw new NotSupportedException("Unexpected row type.");
        }

        internal bool TryGetRowIndex(in IndexPath modelIndex, out int rowIndex, int fromRowIndex = 0)
        {
            if (modelIndex.Count == 0)
            {
                rowIndex = -1;
                return true;
            }

            for (var i = fromRowIndex; i < _flattenedRows.Count; ++i)
            {
                if (modelIndex == _flattenedRows[i].ModelIndexPath)
                {
                    rowIndex = i;
                    return true;
                }
            }

            rowIndex = -1;
            return false;
        }

        private void InitializeRows()
        {
            var i = 0;

            foreach (var model in _roots)
            {
                i += AddRowsAndDescendants(i, model);
            }
        }

        private int AddRowsAndDescendants(int index, HierarchicalRow<TModel> row)
        {
            var i = index;
            _flattenedRows.Insert(i++, row);

            if (row.Children is object)
            {
                foreach (var childRow in row.Children)
                {
                    i += AddRowsAndDescendants(i, childRow);
                }
            }

            return i - index;
        }

        private static void ExpandCollapseRecursiveCore(IReadOnlyList<HierarchicalRow<TModel>> rows, Func<TModel, bool> predicate)
        {
            for (var i = 0; i < rows.Count; ++i)
            {
                var row = rows[i];
                var expand = predicate(row.Model);

                if (expand)
                {
                    row.IsExpanded = true;
                    if (row.Children is { } children)
                        ExpandCollapseRecursiveCore(children, predicate);
                }
                else
                {
                    if (row.Children is { } children)
                        ExpandCollapseRecursiveCore(children, predicate);
                    row.IsExpanded = false;
                }
            }
        }

        private void OnCollectionChanged(in IndexPath parentIndex, NotifyCollectionChangedEventArgs e)
        {
            if (_ignoreCollectionChanges)
                return;

            void Add(int index, IEnumerable? items, bool raise)
            {
                if (items is null)
                    return;

                var start = index;

                foreach (HierarchicalRow<TModel> row in items)
                {
                    index += AddRowsAndDescendants(index, row);
                }

                if (raise && index > start)
                {
                    CollectionChanged?.Invoke(
                        this,
                        new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Add,
                            new ListSpan(_flattenedRows, start, index - start),
                            start));
                }
            }

            void Remove(int index, int count, bool raise)
            {
                if (count == 0)
                    return;

                var oldItems = raise && CollectionChanged is not null ?
                    new HierarchicalRow<TModel>[count] : null;

                for (var i = 0; i < count; ++i)
                {
                    var row = _flattenedRows[i + index];
                    if (oldItems is not null)
                        oldItems[i] = row;
                }

                _flattenedRows.RemoveRange(index, count);
                
                if (oldItems is not null)
                {
                    CollectionChanged!(
                        this,
                        new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Remove,
                            oldItems,
                            index));
                }
            }

            int Advance(int rowIndex, int count)
            {
                var i = rowIndex;

                while (count > 0)
                {
                    var row = _flattenedRows[i];
                    if (row.Children?.Count > 0)
                        i = Advance(i + 1, row.Children.Count);
                    else
                        i += + 1;
                    --count;
                }

                return i;
            }

            int GetDescendentRowCount(int rowIndex)
            {
                if (rowIndex == -1)
                    return _flattenedRows.Count;

                var row = _flattenedRows[rowIndex];
                var depth = row.ModelIndexPath.Count;
                var i = rowIndex + 1;

                while (i < _flattenedRows.Count && _flattenedRows[i].ModelIndexPath.Count > depth)
                    ++i;

                return i - (rowIndex + 1);
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (TryGetRowIndex(parentIndex, out var parentRowIndex))
                    {
                        var insert = Advance(parentRowIndex + 1, e.NewStartingIndex);
                        Add(insert, e.NewItems, true);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (TryGetRowIndex(parentIndex, out parentRowIndex))
                    {
                        var start = Advance(parentRowIndex + 1, e.OldStartingIndex);
                        var end = Advance(start, e.OldItems!.Count);
                        Remove(start, end - start, true);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    if (TryGetRowIndex(parentIndex, out parentRowIndex))
                    {
                        var start = Advance(parentRowIndex + 1, e.OldStartingIndex);
                        var end = Advance(start, e.OldItems!.Count);
                        Remove(start, end - start, true);
                        Add(start, e.NewItems, true);
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    if (TryGetRowIndex(parentIndex, out parentRowIndex))
                    {
                        var fromStart = Advance(parentRowIndex + 1, e.OldStartingIndex);
                        var fromEnd = Advance(fromStart, e.OldItems!.Count);
                        var to = Advance(parentRowIndex + 1, e.NewStartingIndex);
                        Remove(fromStart, fromEnd - fromStart, true);
                        Add(to, e.NewItems, true);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    if (TryGetRowIndex(parentIndex, out parentRowIndex))
                    {
                        var children = parentRowIndex >= 0 ? _flattenedRows[parentRowIndex].Children : _roots;
                        var count = GetDescendentRowCount(parentRowIndex);
                        Remove(parentRowIndex + 1, count, true);
                        Add(parentRowIndex + 1, children, true);
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private void OnRootsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnCollectionChanged(default, e);
        }

        private class RootRows : SortableRowsBase<TModel, HierarchicalRow<TModel>>,
            IReadOnlyList<HierarchicalRow<TModel>>
        {
            private readonly HierarchicalRows<TModel> _owner;

            public RootRows(
                HierarchicalRows<TModel> owner,
                TreeDataGridItemsSourceView<TModel> items,
                Comparison<TModel>? comparison)
                : base(items, comparison)
            {
                _owner = owner;
            }

            protected override HierarchicalRow<TModel> CreateRow(int modelIndex, TModel model)
            {
                return new HierarchicalRow<TModel>(
                    _owner,
                    _owner._expanderColumn,
                    new IndexPath(modelIndex),
                    model,
                    _owner._comparison);
            }
        }
    }
}
