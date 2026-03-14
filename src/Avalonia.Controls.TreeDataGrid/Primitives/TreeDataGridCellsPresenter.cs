using System;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Presents and manages the cells for a single row. Responsible for realizing,
    /// measuring and recycling cell elements and forwarding selection/child index
    /// information to consumers.
    /// </summary>
    public class TreeDataGridCellsPresenter : TreeDataGridColumnarPresenterBase<IColumn>, IChildIndexProvider
    {
        public static readonly DirectProperty<TreeDataGridCellsPresenter, IRows?> RowsProperty =
            AvaloniaProperty.RegisterDirect<TreeDataGridCellsPresenter, IRows?>(
                nameof(Rows),
                o => o.Rows,
                (o, v) => o.Rows = v);

        private IRows? _rows;

        /// <inheritdoc/>
        public event EventHandler<ChildIndexChangedEventArgs>? ChildIndexChanged;

        /// <summary>
        /// Rows view used by the presenter.
        /// </summary>
        public IRows? Rows
        {
            get => _rows;
            set => SetAndRaise(RowsProperty, ref _rows, value);
        }

        /// <summary>
        /// Gets the row index for which the presenter is realized, or -1 if not realized.
        /// </summary>
        public int RowIndex { get; private set; } = -1;

        /// <inheritdoc/>
        protected override Orientation Orientation => Orientation.Horizontal;

        /// <summary>
        /// Realizes the presenter for the specified row index.
        /// </summary>
        /// <param name="index">The row index to realize.</param>
        public void Realize(int index)
        {
            if (RowIndex != -1)
                throw new InvalidOperationException("Row is already realized.");
            RowIndex = index;
            InvalidateMeasure();
        }

        /// <summary>
        /// Unrealizes the presenter and recycles all realized elements.
        /// </summary>
        public void Unrealize()
        {
            if (RowIndex == -1)
                throw new InvalidOperationException("Row is not realized.");
            RowIndex = -1;
            RecycleAllElements();
        }

        /// <summary>
        /// Updates the realized row index and adjusts child cells accordingly.
        /// </summary>
        /// <param name="index">The new row index.</param>
        public void UpdateRowIndex(int index)
        {
            if (index < 0 || Rows is null || index >= Rows.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (RowIndex == -1)
                throw new InvalidOperationException("Row is not realized.");

            RowIndex = index;

            foreach (var element in RealizedElements)
            {
                if (element is TreeDataGridCell { RowIndex: >= 0, ColumnIndex: >= 0 } cell)
                    cell.UpdateRowIndex(index);
            }
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            return RowIndex == -1 ? default : base.MeasureOverride(availableSize);
        }

        /// <inheritdoc/>
        protected override Size MeasureElement(int index, Control element, Size availableSize)
        {
            element.Measure(availableSize);
            return ((IColumns)Items!).CellMeasured(index, RowIndex, element.DesiredSize);
        }

        /// <inheritdoc/>
        protected override Control GetElementFromFactory(IColumn column, int index)
        {
            var model = _rows!.RealizeCell(column, index, RowIndex);
            var cell = (TreeDataGridCell)GetElementFromFactory(model, index, this);
            cell.Realize(ElementFactory!, GetSelection(), model, index, RowIndex);
            return cell;
        }

        /// <inheritdoc/>
        protected override void RealizeElement(Control element, IColumn column, int index)
        {
            var cell = (TreeDataGridCell)element;

            if (cell.ColumnIndex == index && cell.RowIndex == RowIndex)
            {
                ChildIndexChanged?.Invoke(this, new ChildIndexChangedEventArgs(element, index));
            }
            else if (cell.ColumnIndex == -1 && cell.RowIndex == -1)
            {
                var model = _rows!.RealizeCell(column, index, RowIndex);
                ((TreeDataGridCell)element).Realize(ElementFactory!, GetSelection(), model, index, RowIndex);
                ChildIndexChanged?.Invoke(this, new ChildIndexChangedEventArgs(element, index));
            }
            else
            {
                throw new InvalidOperationException("Cell already realized");
            }
        }

        /// <inheritdoc/>
        protected override void UnrealizeElement(Control element)
        {
            var cell = (TreeDataGridCell)element;
            _rows!.UnrealizeCell(cell.Model!, cell.ColumnIndex, cell.RowIndex);
            cell.Unrealize();
            ChildIndexChanged?.Invoke(this, new ChildIndexChangedEventArgs(element, cell.RowIndex));
        }

        /// <inheritdoc/>
        protected override void UpdateElementIndex(Control element, int oldIndex, int newIndex)
        {
            ChildIndexChanged?.Invoke(this, new ChildIndexChangedEventArgs(element, newIndex));
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == BackgroundProperty)
                InvalidateVisual();
        }
        
        internal void UpdateSelection(ITreeDataGridSelectionInteraction? selection)
        {
            foreach (var element in RealizedElements)
            {
                if (element is TreeDataGridCell { RowIndex: >= 0, ColumnIndex: >= 0 } cell)
                    cell.UpdateSelection(selection);
            }
        }

        internal void UnrealizeOnRowRemoved()
        {
            if (RowIndex == -1)
                throw new InvalidOperationException("Row is not realized.");
            RowIndex = -1;
            RecycleAllElementsOnItemRemoved();
        }

        private ITreeDataGridSelectionInteraction? GetSelection()
        {
            return this.FindAncestorOfType<TreeDataGrid>()?.SelectionInteraction;
        }

        /// <inheritdoc/>
        public int GetChildIndex(ILogical child)
        {
            if (child is TreeDataGridCell cell)
            {
                return cell.ColumnIndex;
            }

            return -1;
        }

        /// <inheritdoc/>
        public bool TryGetTotalCount(out int count)
        {
            if (Items is null)
            {
                count = 0;
                return false;
            }

            count = Items.Count;
            return true;
        }
    }
}
