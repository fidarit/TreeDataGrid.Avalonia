using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls.Models;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using Avalonia.Input;

namespace Avalonia.Controls
{
    /// <summary>
    ///   A data source for a <see cref="TreeDataGrid" /> which displays a flat grid.
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    public class FlatTreeDataGridSource<TModel> : NotifyingBase,
        ITreeDataGridSource<TModel>,
        IDisposable
            where TModel: class
    {
        private IEnumerable<TModel> _items;
        private TreeDataGridItemsSourceView<TModel> _itemsView;
        private AnonymousSortableRows<TModel>? _rows;
        private IComparer<TModel>? _comparer;
        private ITreeDataGridSelection? _selection;
        private bool _isSelectionSet;

        /// <summary>
        ///   Initializes a new instance of the <see cref="FlatTreeDataGridSource{TModel}" /> class
        ///   with the specified collection of items.
        /// </summary>
        /// <param name="items">
        ///   The collection of items to be displayed in the tree data grid.  This collection serves
        ///   as the data source for the grid.
        /// </param>
        public FlatTreeDataGridSource(IEnumerable<TModel> items)
        {
            _items = items;
            _itemsView = TreeDataGridItemsSourceView<TModel>.GetOrCreate(items);
            Columns = [];
        }

        /// <summary>
        ///   Gets the columns to be displayed.
        /// </summary>
        public ColumnList<TModel> Columns { get; }
        /// <inheritdoc />
        public IRows Rows => _rows ??= CreateRows();
        IColumns ITreeDataGridSource.Columns => Columns;

        /// <inheritdoc />
        public IEnumerable<TModel> Items
        {
            get => _items;
            set
            {
                if (_items != value)
                {
                    _items = value;
                    _itemsView = TreeDataGridItemsSourceView<TModel>.GetOrCreate(value);
                    _rows?.SetItems(_itemsView);
                    if (_selection is object)
                        _selection.Source = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <inheritdoc />
        public ITreeDataGridSelection? Selection
        {
            get
            {
                if (_selection == null && !_isSelectionSet)
                    _selection = new TreeDataGridRowSelectionModel<TModel>(this);
                return _selection;
            }
            set
            {
                if (_selection != value)
                {
                    if (value?.Source != _items)
                        throw new InvalidOperationException("Selection source must be set to Items.");
                    _selection = value;
                    _isSelectionSet = true;
                    RaisePropertyChanged();
                }
            }
        }

        IEnumerable<object> ITreeDataGridSource.Items => Items;

        /// <summary>
        ///   Gets the cell selection model if cell selection is being used.
        /// </summary>
        /// <remarks>
        ///   This property is equivalent to casting the <see cref="FlatTreeDataGridSource{TModel}.Selection" /> property to
        ///   <see cref="ITreeDataGridCellSelectionModel{T}" />.
        /// </remarks>
        public ITreeDataGridCellSelectionModel<TModel>? CellSelection => Selection as ITreeDataGridCellSelectionModel<TModel>;
        /// <summary>
        ///   Gets the row selection model if row selection is being used.
        /// </summary>
        /// <remarks>
        ///   This property is equivalent to casting the <see cref="FlatTreeDataGridSource{TModel}.Selection" /> property to
        ///   <see cref="ITreeDataGridRowSelectionModel{T}" />.
        /// </remarks>
        public ITreeDataGridRowSelectionModel<TModel>? RowSelection => Selection as ITreeDataGridRowSelectionModel<TModel>;
        /// <inheritdoc />
        public bool IsHierarchical => false;
        /// <inheritdoc />
        public bool IsSorted => _comparer is not null;

        /// <inheritdoc />
        public event Action? Sorted;

        /// <inheritdoc />
        public void Dispose()
        {
            _rows?.Dispose();
            GC.SuppressFinalize(this);
        }

        void ITreeDataGridSource.DragDropRows(
            ITreeDataGridSource source,
            IEnumerable<IndexPath> indexes,
            IndexPath targetIndex,
            TreeDataGridRowDropPosition position,
            DragDropEffects effects)
        {
            if (effects != DragDropEffects.Move)
                throw new NotSupportedException("Only move is currently supported for drag/drop.");
            if (IsSorted)
                throw new NotSupportedException("Drag/drop is not supported on sorted data.");
            if (position == TreeDataGridRowDropPosition.Inside)
                throw new ArgumentException("Invalid drop position.", nameof(position));
            if (indexes.Any(x => x.Count != 1))
                throw new ArgumentException("Invalid source index.", nameof(indexes));
            if (targetIndex.Count != 1)
                throw new ArgumentException("Invalid target index.", nameof(targetIndex));
            if (_items is not IList<TModel> items)
                throw new InvalidOperationException("Items does not implement IList<T>.");

            if (position == TreeDataGridRowDropPosition.None)
                return;

            var ti = targetIndex[0];

            if (position == TreeDataGridRowDropPosition.After)
                ++ti;

            var sourceItems = new List<TModel>();

            foreach (var src in indexes.OrderByDescending(x => x))
            {
                var i = src[0];
                sourceItems.Add(items[i]);
                items.RemoveAt(i);

                if (i < ti)
                    --ti;
            }

            for (var si = sourceItems.Count - 1; si >= 0; --si)
            {
                items.Insert(ti++, sourceItems[si]);
            }
        }

        bool ITreeDataGridSource.SortBy(IColumn? column, ListSortDirection direction)
        {
            if (column is IColumn<TModel> typedColumn)
            {
                if (!Columns.Contains(typedColumn))
                    return true;

                var comparer = typedColumn.GetComparison(direction);

                if (comparer is not null)
                {
                    _comparer = new FuncComparer<TModel>(comparer);
                    _rows?.Sort(_comparer);
                    Sorted?.Invoke();
                    foreach (var c in Columns)
                        c.SortDirection = c == column ? direction : null;
                }
                return true;
            }

            return false;
        }

        IEnumerable<object> ITreeDataGridSource.GetModelChildren(object model)
        {
            return [];
        }

        private AnonymousSortableRows<TModel> CreateRows()
        {
            return new AnonymousSortableRows<TModel>(_itemsView, _comparer);
        }
    }
}
