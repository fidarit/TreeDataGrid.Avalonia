using System;
using System.Xml.Linq;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    ///   Presents and manages cells within a row of a <see cref="TreeDataGrid" /> control.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TreeDataGridCellsPresenter is responsible for creating, recycling, and arranging cells
    ///     horizontally  within a row of a TreeDataGrid. It handles the virtualization of cells,
    ///     ensuring that only visible cells are realized in the visual tree to improve performance.
    ///   </para>
    ///   <para>
    ///     This presenter is typically used within a <see cref="TreeDataGridRow" /> control template to
    ///     display the cells for that row. It coordinates with its parent row and the TreeDataGrid to
    ///     manage cell lifecycle, selection state, and layout.
    ///   </para>
    /// </remarks>
    public class TreeDataGridCellsPresenter : TreeDataGridColumnarPresenterBase<IColumn>, IChildIndexProvider
    {
        /// <summary>
        ///   Defines the <see cref="Rows" /> property.
        /// </summary>
        public static readonly DirectProperty<TreeDataGridCellsPresenter, IRows?> RowsProperty =
            AvaloniaProperty.RegisterDirect<TreeDataGridCellsPresenter, IRows?>(
                nameof(Rows),
                o => o.Rows,
                (o, v) => o.Rows = v);

        private IRows? _rows;

        /// <summary>
        ///   Occurs when the index of a child element in the presenter changes.
        /// </summary>
        public event EventHandler<ChildIndexChangedEventArgs>? ChildIndexChanged;

        /// <summary>
        ///   Gets or sets the rows collection from which to obtain cell data.
        /// </summary>
        /// <value>
        ///   The collection of rows used to populate cells in this presenter.
        /// </value>
        public IRows? Rows
        {
            get => _rows;
            set => SetAndRaise(RowsProperty, ref _rows, value);
        }

        /// <summary>
        ///   Gets the index of the row that this presenter is currently displaying.
        /// </summary>
        /// <value>
        ///   The zero-based row index, or -1 if the presenter is not realized.
        /// </value>
        public int RowIndex { get; private set; } = -1;

        /// <summary>
        ///   Gets the orientation in which the cells are arranged.
        /// </summary>
        /// <value>
        ///   Always <see cref="Orientation.Horizontal" />.
        /// </value>
        /// <remarks>
        ///   Cells in a row are always arranged horizontally, with each cell representing a different column.
        /// </remarks>
        protected override Orientation Orientation => Orientation.Horizontal;

        /// <summary>
        ///   Prepares the presenter to display cells for the specified row index.
        /// </summary>
        /// <param name="index">The index of the row to realize.</param>
        /// <exception cref="InvalidOperationException">The presenter is already realized.</exception>
        /// <remarks>
        ///   This method is called by the TreeDataGrid when a row needs to be prepared for display.
        ///   It initializes the presenter with the specified row index and triggers a measure pass
        ///   to create and arrange the cells.
        /// </remarks>
        public void Realize(int index)
        {
            if (RowIndex != -1)
                throw new InvalidOperationException("Row is already realized.");
            RowIndex = index;
            InvalidateMeasure();
        }

        /// <summary>
        ///   Releases resources used by the presenter and prepares it for reuse.
        /// </summary>
        /// <exception cref="InvalidOperationException">The presenter is not realized.</exception>
        /// <remarks>
        ///   This method is called by the TreeDataGrid when a row is no longer needed for display.
        ///   It clears the presenter's row index and recycles all cell elements.
        /// </remarks>
        public void Unrealize()
        {
            if (RowIndex == -1)
                throw new InvalidOperationException("Row is not realized.");
            RowIndex = -1;
            RecycleAllElements();
        }

        /// <summary>
        ///   Updates the row index of this presenter and all its cells.
        /// </summary>
        /// <param name="index">The new row index.</param>
        /// <exception cref="ArgumentOutOfRangeException">The index is out of range.</exception>
        /// <exception cref="InvalidOperationException">The presenter is not realized.</exception>
        /// <remarks>
        ///   This method is called when the index of the row changes, such as when rows are inserted or removed above it.
        ///   It updates the presenter's row index and updates the row index of all realized cells.
        /// </remarks>
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

        /// <inheritdoc />
        protected override Size MeasureOverride(Size availableSize)
        {
            return RowIndex == -1 ? default : base.MeasureOverride(availableSize);
        }

        /// <inheritdoc />
        protected override Size MeasureElement(int index, Control element, Size availableSize)
        {
            element.Measure(availableSize);
            return ((IColumns)Items!).CellMeasured(index, RowIndex, element.DesiredSize);
        }

        /// <inheritdoc />
        protected override Control GetElementFromFactory(IColumn column, int index)
        {
            var model = _rows!.RealizeCell(column, index, RowIndex);
            var cell = (TreeDataGridCell)GetElementFromFactory(model, index, this);
            cell.Realize(ElementFactory!, GetSelection(), model, index, RowIndex);
            return cell;
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        protected override void UnrealizeElement(Control element)
        {
            var cell = (TreeDataGridCell)element;
            _rows!.UnrealizeCell(cell.Model!, cell.ColumnIndex, cell.RowIndex);
            cell.Unrealize();
            ChildIndexChanged?.Invoke(this, new ChildIndexChangedEventArgs(element, cell.RowIndex));
        }

        /// <inheritdoc />
        protected override void UpdateElementIndex(Control element, int oldIndex, int newIndex)
        {
            ChildIndexChanged?.Invoke(this, new ChildIndexChangedEventArgs(element, newIndex));
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public int GetChildIndex(ILogical child)
        {
            if (child is TreeDataGridCell cell)
            {
                return cell.ColumnIndex;
            }

            return -1;
        }

        /// <inheritdoc />
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
