using System;
using System.Text;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using Avalonia.Input;
using Avalonia.LogicalTree;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    ///   A control which displays a row in a <see cref="TreeDataGrid" /> control.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TreeDataGridRow is responsible for displaying a single row of data in a TreeDataGrid.
    ///     It hosts a <see cref="TreeDataGridCellsPresenter" /> that displays the individual cells for
    ///     each column.
    ///   </para>
    ///   <para>
    ///     This class handles row selection, provides access to the row's cells, and supports drag
    ///     and drop operations when <see cref="TreeDataGrid.AutoDragDropRows" /> is enabled.
    ///   </para>
    ///   <para>
    ///     This class implements the following pseudo-classes:
    ///     <list type="bullet">
    ///       <item>
    ///         <description>:selected - Set when the row is selected</description>
    ///       </item>
    ///     </list>
    ///   </para>
    /// </remarks>
    [PseudoClasses(":selected")]
    public class TreeDataGridRow : TemplatedControl
    {
        private const double DragDistance = 3;
        private static readonly Point s_InvalidPoint = new(double.NegativeInfinity, double.NegativeInfinity);

        /// <summary>
        ///   Defines the <see cref="Columns" /> property.
        /// </summary>
        public static readonly DirectProperty<TreeDataGridRow, IColumns?> ColumnsProperty =
            AvaloniaProperty.RegisterDirect<TreeDataGridRow, IColumns?>(
                nameof(Columns),
                o => o.Columns);

        /// <summary>
        ///   Defines the <see cref="ElementFactory" /> property.
        /// </summary>
        public static readonly DirectProperty<TreeDataGridRow, TreeDataGridElementFactory?> ElementFactoryProperty =
            AvaloniaProperty.RegisterDirect<TreeDataGridRow, TreeDataGridElementFactory?>(
                nameof(ElementFactory),
                o => o.ElementFactory,
                (o, v) => o.ElementFactory = v);

        /// <summary>
        ///   Defines the <see cref="IsSelected" /> property.
        /// </summary>
        public static readonly DirectProperty<TreeDataGridRow, bool> IsSelectedProperty =
            AvaloniaProperty.RegisterDirect<TreeDataGridRow, bool>(
                nameof(IsSelected),
                o => o.IsSelected);

        /// <summary>
        ///   Defines the <see cref="Rows" /> property.
        /// </summary>
        public static readonly DirectProperty<TreeDataGridRow, IRows?> RowsProperty =
            AvaloniaProperty.RegisterDirect<TreeDataGridRow, IRows?>(
                nameof(Rows),
                o => o.Rows);

        private IColumns? _columns;
        private TreeDataGridElementFactory? _elementFactory;
        private bool _isSelected;
        private IRows? _rows;
        private Point _mouseDownPosition = s_InvalidPoint;
        private TreeDataGrid? _treeDataGrid;

        /// <summary>
        ///   Gets the columns collection from the owning <see cref="TreeDataGrid" />.
        /// </summary>
        public IColumns? Columns
        {
            get => _columns;
            private set => SetAndRaise(ColumnsProperty, ref _columns, value);
        }

        /// <summary>
        ///   Gets or sets the element factory used to create cells.
        /// </summary>
        public TreeDataGridElementFactory? ElementFactory
        {
            get => _elementFactory;
            set => SetAndRaise(ElementFactoryProperty, ref _elementFactory, value);
        }

        /// <summary>
        ///   Gets a value indicating whether the row is selected.
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            private set => SetAndRaise(IsSelectedProperty, ref _isSelected, value);
        }

        /// <summary>
        ///   Gets the data model associated with this row.
        /// </summary>
        /// <remarks>
        ///   When the row is realized, returns the underlying model of the row. Will be of the type
        ///   <c>TModel</c> as specified in the <see cref="ITreeDataGridSource{TModel}" />.
        /// </remarks>
        public object? Model => DataContext;

        /// <summary>
        ///   Gets the rows collection from the owning <see cref="TreeDataGrid" />.
        /// </summary>
        public IRows? Rows
        {
            get => _rows;
            private set => SetAndRaise(RowsProperty, ref _rows, value);
        }

        /// <summary>
        ///   Gets the cells presenter that displays the cells in this row.
        /// </summary>
        /// <remarks>
        ///   The cells presenter is defined in the control template and is looked up
        ///   in <see cref="OnApplyTemplate(TemplateAppliedEventArgs)" />.
        /// </remarks>
        public TreeDataGridCellsPresenter? CellsPresenter { get; private set; }
        /// <summary>
        ///   Gets the index of this row in the TreeDataGrid.
        /// </summary>
        /// <value>
        ///   The zero-based row index, or -1 if the row is not realized.
        /// </value>
        /// <remarks>
        ///   The row index is based upon the data currently displayed in the TreeDataGrid, and not
        ///   the underlying data source. For example, if the data is sorted, filtered or contains
        ///   expanded hierarchical rows, the indices may not correspond directly to the data source.
        /// </remarks>
        public int RowIndex { get; private set; }

        /// <summary>
        ///   Prepares the row for display with the specified data.
        /// </summary>
        /// <param name="elementFactory">The element factory used to create child elements.</param>
        /// <param name="selection">The selection interaction model.</param>
        /// <param name="columns">The columns collection.</param>
        /// <param name="rows">The rows collection.</param>
        /// <param name="rowIndex">The index of the row.</param>
        /// <remarks>
        ///   This method is called by the <see cref="TreeDataGridRowsPresenter" /> when a row
        ///   needs to be prepared for display. It initializes the row with the data and
        ///   updates its selection state.
        /// </remarks>
        public void Realize(
            TreeDataGridElementFactory? elementFactory,
            ITreeDataGridSelectionInteraction? selection,
            IColumns? columns,
            IRows? rows,
            int rowIndex)
        {
            ElementFactory = elementFactory;
            Columns = columns;
            Rows = rows;
            DataContext = rows?[rowIndex].Model;
            IsSelected = selection?.IsRowSelected(rowIndex) ?? false;
            RowIndex = rowIndex;
            UpdateSelection(selection);
            CellsPresenter?.Realize(rowIndex);
            _treeDataGrid?.RaiseRowPrepared(this, RowIndex);
        }

        /// <summary>
        ///   Attempts to retrieve a realized cell at the specified column index.
        /// </summary>
        /// <param name="columnIndex">The index of the column.</param>
        /// <returns>
        ///   The cell at the specified column index, or null if no cell exists at that position.
        /// </returns>
        /// <remarks>
        ///   This method delegates to the <see cref="CellsPresenter" /> to retrieve the cell.
        ///   If the specified cell is not currently realized (e.g., scrolled out of view),
        ///   this method will return null.
        /// </remarks>
        public Control? TryGetCell(int columnIndex)
        {
            return CellsPresenter?.TryGetElement(columnIndex);
        }

        /// <summary>
        ///   Updates the index of this row.
        /// </summary>
        /// <param name="index">The new row index.</param>
        /// <exception cref="InvalidOperationException">The row is not realized.</exception>
        /// <remarks>
        ///   This method is called when the index of the row changes, such as when rows are
        ///   inserted or removed above it.
        /// </remarks>
        public void UpdateIndex(int index)
        {
            if (RowIndex == -1)
                throw new InvalidOperationException("Row is not realized.");

            RowIndex = index;
            CellsPresenter?.UpdateRowIndex(index);
        }

        /// <summary>
        ///   Releases resources used by the row and prepares it for reuse.
        /// </summary>
        /// <remarks>
        ///   This method is called by the <see cref="TreeDataGridRowsPresenter" /> when a row
        ///   is no longer needed for display. It clears the row's state and raises the
        ///   <see cref="TreeDataGrid.RowClearing" /> event.
        /// </remarks>
        public void Unrealize()
        {
            _treeDataGrid?.RaiseRowClearing(this, RowIndex);
            RowIndex = -1;
            DataContext = null;
            IsSelected = false;
            CellsPresenter?.Unrealize();
        }

        /// <inheritdoc />
        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            _treeDataGrid = this.FindLogicalAncestorOfType<TreeDataGrid>();
            base.OnAttachedToLogicalTree(e);
        }

        /// <inheritdoc />
        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            _treeDataGrid = null;
            base.OnDetachedFromLogicalTree(e);
        }

        /// <inheritdoc />
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            // The row may be realized before being parented. In this case raise the RowPrepared event here.
            if (_treeDataGrid is not null && RowIndex >= 0)
                _treeDataGrid.RaiseRowPrepared(this, RowIndex);
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            CellsPresenter = e.NameScope.Find<TreeDataGridCellsPresenter>("PART_CellsPresenter");

            if (RowIndex >= 0)
                CellsPresenter?.Realize(RowIndex);
        }

        /// <inheritdoc />
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            _mouseDownPosition = !e.Handled ? e.GetPosition(this) : s_InvalidPoint;
        }

        /// <inheritdoc />
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);

            var currentPoint = e.GetCurrentPoint(this);
            var delta = currentPoint.Position - _mouseDownPosition;

            var pointerSupportsDrag = currentPoint.Pointer.Type switch
            {
                PointerType.Mouse => currentPoint.Properties.IsLeftButtonPressed,
                PointerType.Pen => currentPoint.Properties.IsRightButtonPressed,
                _ => false
            };

            if (!pointerSupportsDrag ||
                e.Handled ||
                Math.Abs(delta.X) < DragDistance && Math.Abs(delta.Y) < DragDistance ||
                _mouseDownPosition == s_InvalidPoint)
                return;

            _mouseDownPosition = s_InvalidPoint;

            var presenter = Parent as TreeDataGridRowsPresenter;
            var owner = presenter?.TemplatedParent as TreeDataGrid;
            owner?.RaiseRowDragStarted(e);
        }

        /// <inheritdoc />
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            _mouseDownPosition = s_InvalidPoint;
        }

        /// <inheritdoc />
        protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
        {
            base.OnPointerCaptureLost(e);
            _mouseDownPosition = s_InvalidPoint;
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            if (change.Property == IsSelectedProperty)
            {
                PseudoClasses.Set(":selected", IsSelected);
            }
            
            base.OnPropertyChanged(change);
        }

        internal void UpdateSelection(ITreeDataGridSelectionInteraction? selection)
        {
            IsSelected = selection?.IsRowSelected(RowIndex) ?? false;
            CellsPresenter?.UpdateSelection(selection);
        }

        public void UnrealizeOnItemRemoved()
        {
            _treeDataGrid?.RaiseRowClearing(this, RowIndex);
            RowIndex = -1;
            DataContext = null;
            IsSelected = false;
            CellsPresenter?.UnrealizeOnRowRemoved();
        }
    }
}
