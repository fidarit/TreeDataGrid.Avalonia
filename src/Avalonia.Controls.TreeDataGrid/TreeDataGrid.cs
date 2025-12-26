using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Selection;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Utilities;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    ///   Represents a control that displays hierarchical and tabular data together in a single view.
    ///   It is a combination of a TreeView and DataGrid control.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The TreeDataGrid supports both flat (2D table) and hierarchical (tree with columns) layouts,
    ///     making it suitable for a variety of data visualization needs.
    ///   </para>
    ///   <para>
    ///     Key features include:
    ///     <list type="bullet">
    ///       <item>
    ///         <description>Supports both row selection and cell selection modes</description>
    ///       </item>
    ///       <item>
    ///         <description>Column sorting capabilities</description>
    ///       </item>
    ///       <item>
    ///         <description>Support for various column types (Text, CheckBox, Template)</description>
    ///       </item>
    ///       <item>
    ///         <description>MVVM-first data model</description>
    ///       </item>
    ///       <item>
    ///         <description>Drag and drop functionality for rows</description>
    ///       </item>
    ///     </list>
    ///   </para>
    /// </remarks>
    public class TreeDataGrid : TemplatedControl
    {
        /// <summary>
        ///   Defines the <see cref="AutoDragDropRows" /> property.
        /// </summary>
        public static readonly StyledProperty<bool> AutoDragDropRowsProperty =
            AvaloniaProperty.Register<TreeDataGrid, bool>(nameof(AutoDragDropRows));

        /// <summary>
        ///   Defines the <see cref="CanUserResizeColumns" /> property.
        /// </summary>
        public static readonly StyledProperty<bool> CanUserResizeColumnsProperty =
            AvaloniaProperty.Register<TreeDataGrid, bool>(nameof(CanUserResizeColumns), true);

        /// <summary>
        ///   Defines the <see cref="CanUserSortColumns" /> property.
        /// </summary>
        public static readonly StyledProperty<bool> CanUserSortColumnsProperty =
            AvaloniaProperty.Register<TreeDataGrid, bool>(nameof(CanUserSortColumns), true);

        /// <summary>
        ///   Defines the <see cref="Columns" /> property.
        /// </summary>
        public static readonly DirectProperty<TreeDataGrid, IColumns?> ColumnsProperty =
            AvaloniaProperty.RegisterDirect<TreeDataGrid, IColumns?>(
                nameof(Columns),
                o => o.Columns);

        /// <summary>
        ///   Defines the <see cref="ElementFactory" /> property.
        /// </summary>
        public static readonly DirectProperty<TreeDataGrid, TreeDataGridElementFactory> ElementFactoryProperty =
            AvaloniaProperty.RegisterDirect<TreeDataGrid, TreeDataGridElementFactory>(
                nameof(ElementFactory),
                o => o.ElementFactory,
                (o, v) => o.ElementFactory = v);

        /// <summary>
        ///   Defines the <see cref="Rows" /> property.
        /// </summary>
        public static readonly DirectProperty<TreeDataGrid, IRows?> RowsProperty =
            AvaloniaProperty.RegisterDirect<TreeDataGrid, IRows?>(
                nameof(Rows),
                o => o.Rows,
                (o, v) => o.Rows = v);

        /// <summary>
        ///   Defines the <see cref="Scroll" /> property.
        /// </summary>
        public static readonly DirectProperty<TreeDataGrid, IScrollable?> ScrollProperty =
            AvaloniaProperty.RegisterDirect<TreeDataGrid, IScrollable?>(
                nameof(Scroll),
                o => o.Scroll);

        /// <summary>
        ///   Defines the <see cref="ShowColumnHeaders" /> property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowColumnHeadersProperty =
            AvaloniaProperty.Register<TreeDataGrid, bool>(nameof(ShowColumnHeaders), true);

        /// <summary>
        ///   Defines the <see cref="Source" /> property.
        /// </summary>
        public static readonly DirectProperty<TreeDataGrid, ITreeDataGridSource?> SourceProperty =
            AvaloniaProperty.RegisterDirect<TreeDataGrid, ITreeDataGridSource?>(
                nameof(Source),
                o => o.Source,
                (o, v) => o.Source = v);

        /// <summary>
        ///   Defines the <see cref="RowDragStarted" /> event.
        /// </summary>
        public static readonly RoutedEvent<TreeDataGridRowDragStartedEventArgs> RowDragStartedEvent =
            RoutedEvent.Register<TreeDataGrid, TreeDataGridRowDragStartedEventArgs>(
                nameof(RowDragStarted),
                RoutingStrategies.Bubble);

        /// <summary>
        ///   Defines the <see cref="RowDragOver" /> event.
        /// </summary>
        public static readonly RoutedEvent<TreeDataGridRowDragEventArgs> RowDragOverEvent =
            RoutedEvent.Register<TreeDataGrid, TreeDataGridRowDragEventArgs>(
                nameof(RowDragOver),
                RoutingStrategies.Bubble);

        /// <summary>
        ///   Defines the <see cref="RowDrop" /> event.
        /// </summary>
        public static readonly RoutedEvent<TreeDataGridRowDragEventArgs> RowDropEvent =
            RoutedEvent.Register<TreeDataGrid, TreeDataGridRowDragEventArgs>(
                nameof(RowDrop),
                RoutingStrategies.Bubble);

        private const double AutoScrollMargin = 60;
        private const int AutoScrollSpeed = 50;
        private TreeDataGridElementFactory? _elementFactory;
        private ITreeDataGridSource? _source;
        private IColumns? _columns;
        private IRows? _rows;
        private IScrollable? _scroll;
        private IScrollable? _headerScroll;
        private ITreeDataGridSelectionInteraction? _selection;
        private Control? _userSortColumn;
        private ListSortDirection _userSortDirection;
        private TreeDataGridCellEventArgs? _cellArgs;
        private TreeDataGridRowEventArgs? _rowArgs;
        private Canvas? _dragAdorner;
        private bool _hideDragAdorner;
        private DispatcherTimer? _autoScrollTimer;
        private bool _autoScrollDirection;

        /// <summary>
        ///   Initializes a new instance of the <see cref="TreeDataGrid" /> class.
        /// </summary>
        public TreeDataGrid()
        {
            AddHandler(TreeDataGridColumnHeader.ClickEvent, OnClick);
            AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
        }

        static TreeDataGrid()
        {
            DragDrop.DragOverEvent.AddClassHandler<TreeDataGrid>((x, e) => x.OnDragOver(e));
            DragDrop.DragLeaveEvent.AddClassHandler<TreeDataGrid>((x, e) => x.OnDragLeave(e));
            DragDrop.DropEvent.AddClassHandler<TreeDataGrid>((x, e) => x.OnDrop(e));
        }

        /// <summary>
        ///   Gets or sets a value indicating whether rows can be automatically dragged and dropped
        ///   within the TreeDataGrid.
        /// </summary>
        /// <remarks>
        ///   When enabled, users can reorder rows by dragging them to a new position. Changes will
        ///   be reflected in the underlying data source. The default value is false.
        /// </remarks>
        public bool AutoDragDropRows
        {
            get => GetValue(AutoDragDropRowsProperty);
            set => SetValue(AutoDragDropRowsProperty, value);
        }

        /// <summary>
        ///   Gets or sets a value indicating whether users can resize columns.
        /// </summary>
        /// <remarks>
        ///   When true, users can adjust column widths by dragging the column dividers. The default
        ///   value is true.
        /// </remarks>
        public bool CanUserResizeColumns
        {
            get => GetValue(CanUserResizeColumnsProperty);
            set => SetValue(CanUserResizeColumnsProperty, value);
        }

        /// <summary>
        ///   Gets or sets a value indicating whether users can sort columns.
        /// </summary>
        /// <remarks>
        ///   When true, clicking on a column header will sort the data by that column. The default
        ///   value is true.
        /// </remarks>
        public bool CanUserSortColumns
        {
            get => GetValue(CanUserSortColumnsProperty);
            set => SetValue(CanUserSortColumnsProperty, value);
        }

        /// <summary>
        ///   Gets the columns collection of the TreeDataGrid.
        /// </summary>
        /// <remarks>
        ///   This property is automatically populated from the <see cref="Source" />.
        /// </remarks>
        public IColumns? Columns
        {
            get => _columns;
            private set => SetAndRaise(ColumnsProperty, ref _columns, value);
        }

        /// <summary>
        ///   Gets or sets the element factory used to create UI elements for the TreeDataGrid.
        /// </summary>
        /// <remarks>
        ///   The element factory is responsible for creating the visual elements that represent
        ///   rows, cells, and other components of the TreeDataGrid.
        /// </remarks>
        public TreeDataGridElementFactory ElementFactory
        {
            get => _elementFactory ??= CreateDefaultElementFactory();
            set
            {
                _ = value ?? throw new ArgumentNullException(nameof(value));
                SetAndRaise(ElementFactoryProperty, ref _elementFactory!, value);
            }
        }

        /// <summary>
        ///   Gets the rows collection of the TreeDataGrid.
        /// </summary>
        /// <remarks>
        ///   This property is automatically populated from the <see cref="Source" />.
        /// </remarks>
        public IRows? Rows
        {
            get => _rows;
            private set => SetAndRaise(RowsProperty, ref _rows, value);
        }

        /// <summary>
        ///   Gets the presenter that displays the column headers.
        /// </summary>
        /// <remarks>
        ///   The column headers presenter is defined in the control template and is responsible
        ///   for rendering the column headers at the top of the TreeDataGrid.
        /// </remarks>
        public TreeDataGridColumnHeadersPresenter? ColumnHeadersPresenter { get; private set; }
        /// <summary>
        ///   Gets the presenter that displays the rows.
        /// </summary>
        /// <remarks>
        ///   The rows presenter is defined in the control template and is responsible for rendering
        ///   the rows of data in the TreeDataGrid.
        /// </remarks>
        public TreeDataGridRowsPresenter? RowsPresenter { get; private set; }

        /// <summary>
        ///   Gets the scroll viewer that enables scrolling through the TreeDataGrid content.
        /// </summary>
        /// <remarks>
        ///   The scroll viewer is defined in the control template and provides vertical and
        ///   horizontal scrolling capabilities for the TreeDataGrid.
        /// </remarks>
        public IScrollable? Scroll
        {
            get => _scroll;
            private set => SetAndRaise(ScrollProperty, ref _scroll, value);
        }

        /// <summary>
        ///   Gets or sets a value indicating whether column headers should be displayed.
        /// </summary>
        /// <remarks>
        ///   The default value is true.
        /// </remarks>
        public bool ShowColumnHeaders
        {
            get => GetValue(ShowColumnHeadersProperty);
            set => SetValue(ShowColumnHeadersProperty, value);
        }

        /// <summary>
        ///   Gets the column selection model when the TreeDataGrid is in cell selection mode.
        /// </summary>
        /// <remarks>
        ///   Returns null if the TreeDataGrid is not in cell selection mode.
        /// </remarks>
        public ITreeDataGridCellSelectionModel? ColumnSelection => Source?.Selection as ITreeDataGridCellSelectionModel;
        /// <summary>
        ///   Gets the row selection model when the TreeDataGrid is in row selection mode.
        /// </summary>
        /// <remarks>
        ///   Returns null if the TreeDataGrid is not in row selection mode.
        /// </remarks>
        public ITreeDataGridRowSelectionModel? RowSelection => Source?.Selection as ITreeDataGridRowSelectionModel;

        /// <summary>
        ///   Gets or sets the data source for the TreeDataGrid.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     The source provides the data model, columns, and rows to be displayed in the TreeDataGrid.
        ///   </para>
        ///   <para>
        ///     Use <see cref="FlatTreeDataGridSource{TModel}" /> for flat data or
        ///     <see cref="HierarchicalTreeDataGridSource{TModel}" /> for hierarchical data.
        ///   </para>
        /// </remarks>
        public ITreeDataGridSource? Source
        {
            get => _source;
            set
            {
                if (_source != value)
                {
                    if (_source != null)
                    {
                        _source.PropertyChanged -= OnSourcePropertyChanged;
                        _source.Sorted -= OnSourceSorted;
                    }

                    var oldSource = _source;
                    _source = value;
                    Columns = _source?.Columns;
                    Rows = _source?.Rows;
                    SelectionInteraction = _source?.Selection as ITreeDataGridSelectionInteraction;

                    if (_source != null)
                    {
                        _source.PropertyChanged += OnSourcePropertyChanged;
                        _source.Sorted += OnSourceSorted;
                    }

                    RaisePropertyChanged(
                        SourceProperty,
                        oldSource,
                        _source);
                }
            }
        }

        internal ITreeDataGridSelectionInteraction? SelectionInteraction
        {
            get => _selection;
            set
            {
                if (_selection != value)
                {
                    if (_selection != null)
                        _selection.SelectionChanged -= OnSelectionInteractionChanged;
                    _selection = value;
                    if (_selection != null)
                        _selection.SelectionChanged += OnSelectionInteractionChanged;
                }
            }
        }

        /// <summary>
        ///   Occurs when a cell is about to be cleared from the visual tree.
        /// </summary>
        /// <remarks>
        ///   A cell is cleared when it is no longer needed for display, such as when it is scrolled
        ///   out of view or when the data is removed from the data source. This event can be used
        ///   to perform any necessary cleanup or state management before a cell is removed from the
        ///   visual tree.
        /// </remarks>
        public event EventHandler<TreeDataGridCellEventArgs>? CellClearing;
        /// <summary>
        ///   Occurs when a cell has been prepared for display.
        /// </summary>
        /// <remarks>
        ///   A cell is prepared when it is created and bound to data for display. This event can be
        ///   used to customize the appearance or behavior of the cell before it is rendered.
        /// </remarks>
        public event EventHandler<TreeDataGridCellEventArgs>? CellPrepared;
        /// <summary>
        ///   Occurs when a cell's value has changed.
        /// </summary>
        /// <remarks>
        ///   This event is raised after the cell's value has been updated, either through user
        ///   interaction or programmatically. It can be used to respond to changes in cell values,
        ///   such as updating related data or triggering validation.
        /// </remarks>
        public event EventHandler<TreeDataGridCellEventArgs>? CellValueChanged;
        /// <summary>
        ///   Occurs when a row is about to be cleared from the visual tree.
        /// </summary>
        /// <remarks>
        ///   A row is cleared when it is no longer needed for display, such as when it is scrolled
        ///   out of view or when the data is removed from the data source. This event can be used
        ///   to perform any necessary cleanup or state management before a row is removed from the
        ///   visual tree.
        /// </remarks>
        public event EventHandler<TreeDataGridRowEventArgs>? RowClearing;
        /// <summary>
        ///   Occurs when a row has been prepared for display.
        /// </summary>
        /// <remarks>
        ///   A row is prepared when it is created and bound to data for display. This event can be
        ///   used to customize the appearance or behavior of the row before it is rendered.
        /// </remarks>
        public event EventHandler<TreeDataGridRowEventArgs>? RowPrepared;

        /// <summary>
        ///   Occurs when the user starts to drag a row.
        /// </summary>
        public event EventHandler<TreeDataGridRowDragStartedEventArgs>? RowDragStarted
        {
            add => AddHandler(RowDragStartedEvent, value!);
            remove => RemoveHandler(RowDragStartedEvent, value!);
        }

        /// <summary>
        ///   Occurs when a drag operation is performed over a row.
        /// </summary>
        public event EventHandler<TreeDataGridRowDragEventArgs>? RowDragOver
        {
            add => AddHandler(RowDragOverEvent, value!);
            remove => RemoveHandler(RowDragOverEvent, value!);
        }

        /// <summary>
        ///   Occurs when a drop operation is performed on a row.
        /// </summary>
        public event EventHandler<TreeDataGridRowDragEventArgs>? RowDrop
        {
            add => AddHandler(RowDropEvent, value!);
            remove => RemoveHandler(RowDropEvent, value!);
        }

        /// <summary>
        ///   Occurs before the selection in the TreeDataGrid changes.
        /// </summary>
        /// <remarks>
        ///   This event can be cancelled to prevent the selection from changing.
        /// </remarks>
        public event CancelEventHandler? SelectionChanging;

        /// <summary>
        ///   Attempts to retrieve a realized cell at the specified column and row indices.
        /// </summary>
        /// <param name="columnIndex">The index of the column.</param>
        /// <param name="rowIndex">The index of the row.</param>
        /// <returns>
        ///   The cell at the specified indices, or null if no cell exists at that position.
        /// </returns>
        /// <remarks>
        ///   The row and column indices are based upon the data currently displayed in the
        ///   TreeDataGrid, and not the underlying data source. For example, if the data is sorted,
        ///   filtered or contains expanded hierarchical rows, the indices may not correspond
        ///   directly to the data source. In addition, if the specified cell is not currently
        ///   visible (e.g. scrolled out of view), this method will return null.
        /// </remarks>
        public Control? TryGetCell(int columnIndex, int rowIndex)
        {
            if (TryGetRow(rowIndex) is TreeDataGridRow row &&
                row.TryGetCell(columnIndex) is Control cell)
            {
                return cell;
            }

            return null;
        }

        /// <summary>
        ///   Attempts to retrieve a realized row at the specified index.
        /// </summary>
        /// <param name="rowIndex">The index of the row.</param>
        /// <returns>
        ///   The row at the specified index, or null if no row exists at that position.
        /// </returns>
        /// <remarks>
        ///   The row index is based upon the data currently displayed in the TreeDataGrid, and not
        ///   the underlying data source. For example, if the data is sorted or filtered, the indices
        ///   may not correspond directly to the data source. In addition, if the specified cell is
        ///   not currently visible (e.g. scrolled out of view), this method will return null.
        /// </remarks>
        public TreeDataGridRow? TryGetRow(int rowIndex)
        {
            return RowsPresenter?.TryGetElement(rowIndex) as TreeDataGridRow;
        }

        /// <summary>
        ///   Attempts to find a <see cref="TreeDataGridCell" /> from a UI element.
        /// </summary>
        /// <param name="element">The UI element to start the search from.</param>
        /// <param name="result">
        ///   When this method returns, contains the containing cell if found; otherwise, null.
        /// </param>
        /// <returns>true if a cell was found; otherwise, false.</returns>
        /// <remarks>
        ///   This method searches up the visual tree from the specified element to find any containing
        ///   cell. This can be useful for determining which cell a user interacted with when handling
        ///   events originating from child elements within a cell.
        /// </remarks>
        public bool TryGetCell(Control? element, [NotNullWhen(true)] out TreeDataGridCell? result)
        {
            if (element.FindAncestorOfType<TreeDataGridCell>(true) is { } cell &&
                cell.ColumnIndex >= 0 &&
                cell.RowIndex >= 0)
            {
                result = cell;
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        ///   Attempts to find a <see cref="TreeDataGridRow" /> from a UI element.
        /// </summary>
        /// <param name="element">The UI element to start the search from.</param>
        /// <param name="result">
        ///   When this method returns, contains the row if found; otherwise, null.
        /// </param>
        /// <returns>true if a row was found; otherwise, false.</returns>
        /// <remarks>
        ///   This method searches up the visual tree from the specified element to find any containing
        ///   row. This can be useful for determining which row a user interacted with when handling
        ///   events originating from child elements within a row.
        /// </remarks>
        public bool TryGetRow(Control? element, [NotNullWhen(true)] out TreeDataGridRow? result)
        {
            if (element is TreeDataGridRow row && row.RowIndex >= 0)
            {
                result = row;
                return true;
            }

            do
            {
                result = element?.FindAncestorOfType<TreeDataGridRow>();
                if (result?.RowIndex >= 0)
                    break;
                element = result;
            } while (result is not null);

            return result is not null;
        }

        /// <summary>
        ///   Attempts to retrieve the model associated with a row containing the specified element.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="element">The UI element to start the search from.</param>
        /// <param name="result">
        ///   When this method returns, contains the model if found; otherwise, the default value
        ///   for the type.
        /// </param>
        /// <returns>true if a model was found; otherwise, false.</returns>
        /// <remarks>
        ///   This method searches up the visual tree from the specified element to find any containing
        ///   row. From that row, it attempts to retrieve the associated model from the data source.
        ///   This can be useful for determining which row a user interacted with when handling
        ///   events originating from child elements within a row.
        /// </remarks>
        public bool TryGetRowModel<TModel>(Control element, [NotNullWhen(true)] out TModel? result)
            where TModel : notnull
        {
            if (Source is object &&
                TryGetRow(element, out var row) &&
                row.RowIndex < Source.Rows.Count &&
                Source.Rows[row.RowIndex] is IRow<TModel> rowWithModel)
            {
                result = rowWithModel.Model;
                return true;
            }

            result = default;
            return false;
        }

        public bool QueryCancelSelection()
        {
            if (SelectionChanging is null)
                return false;
            var e = new CancelEventArgs();
            SelectionChanging(this, e);
            return e.Cancel;
        }

        /// <summary>
        ///   Creates the default element factory for the TreeDataGrid.
        /// </summary>
        /// <remarks>
        ///   This method returns the default value for <see cref="ElementFactory" />. Subclasses can
        ///   override this method to provide a custom default element factory.
        /// </remarks>
        /// <returns>
        ///   A new instance of <see cref="TreeDataGridElementFactory" /> to be used as the default
        ///   element factory.
        /// </returns>
        protected virtual TreeDataGridElementFactory CreateDefaultElementFactory() => new TreeDataGridElementFactory();

        /// <inheritdoc />
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            if (Scroll is ScrollViewer s && _headerScroll is ScrollViewer h)
            {
                s.ScrollChanged -= OnScrollChanged;
                h.ScrollChanged -= OnHeaderScrollChanged;
            }

            base.OnApplyTemplate(e);
            ColumnHeadersPresenter = e.NameScope.Find<TreeDataGridColumnHeadersPresenter>("PART_ColumnHeadersPresenter");
            RowsPresenter = e.NameScope.Find<TreeDataGridRowsPresenter>("PART_RowsPresenter");
            Scroll = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
            _headerScroll = e.NameScope.Find<ScrollViewer>("PART_HeaderScrollViewer");

            if (Scroll is ScrollViewer s1 && _headerScroll is ScrollViewer h1)
            {
                s1.ScrollChanged += OnScrollChanged;
                h1.ScrollChanged += OnHeaderScrollChanged;
            }
        }

        /// <inheritdoc />
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            StopDrag();
        }

        protected void OnPreviewKeyDown(object? o, KeyEventArgs e)
        {
            _selection?.OnPreviewKeyDown(this, e);
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == AutoDragDropRowsProperty)
            {
                DragDrop.SetAllowDrop(this, change.GetNewValue<bool>());
            }
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            _selection?.OnKeyDown(this, e);
        }

        /// <inheritdoc />
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            _selection?.OnKeyUp(this, e);
        }

        /// <inheritdoc />
        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);

            if (e.Text is { Length: > 0 } && char.IsControl(e.Text[0]))
                return;

            _selection?.OnTextInput(this, e);
        }

        /// <inheritdoc />
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            _selection?.OnPointerPressed(this, e);
        }

        /// <inheritdoc />
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            _selection?.OnPointerMoved(this, e);
        }

        /// <inheritdoc />
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            _selection?.OnPointerReleased(this, e);
        }

        internal void RaiseCellClearing(TreeDataGridCell cell, int columnIndex, int rowIndex)
        {
            if (CellClearing is not null)
            {
                _cellArgs ??= new TreeDataGridCellEventArgs();
                _cellArgs.Update(cell, columnIndex, rowIndex);
                CellClearing(this, _cellArgs);
                _cellArgs.Update(null, -1, -1);
            }
        }

        internal void RaiseCellPrepared(TreeDataGridCell cell, int columnIndex, int rowIndex)
        {
            if (CellPrepared is not null)
            {
                _cellArgs ??= new TreeDataGridCellEventArgs();
                _cellArgs.Update(cell, columnIndex, rowIndex);
                CellPrepared(this, _cellArgs);
                _cellArgs.Update(null, -1, -1);
            }
        }

        internal void RaiseCellValueChanged(TreeDataGridCell cell, int columnIndex, int rowIndex)
        {
            if (CellValueChanged is not null)
            {
                _cellArgs ??= new TreeDataGridCellEventArgs();
                _cellArgs.Update(cell, columnIndex, rowIndex);
                CellValueChanged(this, _cellArgs);
                _cellArgs.Update(null, -1, -1);
            }
        }

        internal void RaiseRowClearing(TreeDataGridRow row, int rowIndex)
        {
            if (RowClearing is not null)
            {
                _rowArgs ??= new TreeDataGridRowEventArgs();
                _rowArgs.Update(row, rowIndex);
                RowClearing(this, _rowArgs);
                _rowArgs.Update(null, -1);
            }
        }

        internal void RaiseRowPrepared(TreeDataGridRow row, int rowIndex)
        {
            if (RowPrepared is not null)
            {
                _rowArgs ??= new TreeDataGridRowEventArgs();
                _rowArgs.Update(row, rowIndex);
                RowPrepared(this, _rowArgs);
                _rowArgs.Update(null, -1);
            }
        }

        internal void RaiseRowDragStarted(PointerEventArgs trigger)
        {
            if (_source is null || RowSelection is null)
                return;

            var allowedEffects = AutoDragDropRows && !_source.IsSorted ?
                DragDropEffects.Move :
                DragDropEffects.None;
            var route = BuildEventRoute(RowDragStartedEvent);

            if (route.HasHandlers)
            {
                var e = new TreeDataGridRowDragStartedEventArgs(RowSelection.SelectedItems!);
                e.AllowedEffects = allowedEffects;
                RaiseEvent(e);
                allowedEffects = e.AllowedEffects;
            }

            if (allowedEffects != DragDropEffects.None)
            {
                var data = new DataObject();
                var info = new DragInfo(_source, RowSelection.SelectedIndexes.ToList());
                data.Set(DragInfo.DataFormat, info);
                DragDrop.DoDragDrop(trigger, data, allowedEffects);
            }
        }

        private void OnClick(object? sender, RoutedEventArgs e)
        {
            if (_source is object &&
                e.Source is TreeDataGridColumnHeader columnHeader &&
                columnHeader.ColumnIndex >= 0 &&
                columnHeader.ColumnIndex < _source.Columns.Count &&
                CanUserSortColumns)
            {
                if (_userSortColumn != columnHeader)
                {
                    _userSortColumn = columnHeader;
                    _userSortDirection = ListSortDirection.Ascending;
                }
                else
                {
                    _userSortDirection = _userSortDirection == ListSortDirection.Ascending ?
                        ListSortDirection.Descending : ListSortDirection.Ascending;
                }

                var column = _source.Columns[columnHeader.ColumnIndex];
                _source.SortBy(column, _userSortDirection);
            }
        }

        private Canvas? GetOrCreateDragAdorner()
        {
            _hideDragAdorner = false;

            if (_dragAdorner is not null)
                return _dragAdorner;

            var adornerLayer = AdornerLayer.GetAdornerLayer(this);

            if (adornerLayer is null)
                return null;

            _dragAdorner ??= new Canvas
            {
                Children =
                {
                    new Rectangle
                    {
                        Stroke = TextElement.GetForeground(this),
                        StrokeThickness = 2,
                    },
                },
                IsHitTestVisible = false,
            };

            adornerLayer.Children.Add(_dragAdorner);
            AdornerLayer.SetAdornedElement(_dragAdorner, this);
            return _dragAdorner;
        }

        private void ShowDragAdorner(TreeDataGridRow row, TreeDataGridRowDropPosition position)
        {
            if (position == TreeDataGridRowDropPosition.None ||
                row.TransformToVisual(this) is not { } transform)
            {
                HideDragAdorner();
                return;
            }

            var adorner = GetOrCreateDragAdorner();
            if (adorner is null)
                return;

            var rectangle = (Rectangle)adorner.Children[0];
            var rowBounds = new Rect(row.Bounds.Size).TransformToAABB(transform);

            Canvas.SetLeft(rectangle, rowBounds.Left);
            rectangle.Width = rowBounds.Width;

            switch (position)
            {
                case TreeDataGridRowDropPosition.Before:
                    Canvas.SetTop(rectangle, rowBounds.Top);
                    rectangle.Height = 0;
                    break;
                case TreeDataGridRowDropPosition.After:
                    Canvas.SetTop(rectangle, rowBounds.Bottom);
                    rectangle.Height = 0;
                    break;
                case TreeDataGridRowDropPosition.Inside:
                    Canvas.SetTop(rectangle, rowBounds.Top);
                    rectangle.Height = rowBounds.Height;
                    break;
            }
        }

        private void HideDragAdorner()
        {
            _hideDragAdorner = true;

            DispatcherTimer.RunOnce(() =>
            {
                if (_hideDragAdorner && _dragAdorner?.Parent is AdornerLayer layer)
                {
                    layer.Children.Remove(_dragAdorner);
                    _dragAdorner = null;
                }
            }, TimeSpan.FromMilliseconds(50));
        }

        private void StopDrag()
        {
            HideDragAdorner();
            _autoScrollTimer?.Stop();
        }

        private void AutoScroll(bool direction)
        {
            if (_autoScrollTimer is null)
            {
                _autoScrollTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(AutoScrollSpeed),
                };
                _autoScrollTimer.Tick += OnAutoScrollTick;
            }

            _autoScrollDirection = direction;

            if (!_autoScrollTimer.IsEnabled)
                OnAutoScrollTick(null, EventArgs.Empty);

            _autoScrollTimer.Start();
        }

#if NET5_0_OR_GREATER
        [MemberNotNullWhen(true, nameof(_source))]
#endif
        private bool CalculateAutoDragDrop(
            TreeDataGridRow? targetRow,
            DragEventArgs e,
            [NotNullWhen(true)] out DragInfo? data,
            out TreeDataGridRowDropPosition position)
        {
            if (!AutoDragDropRows ||
                e.Data.Get(DragInfo.DataFormat) is not DragInfo di ||
                _source is null ||
                _source.IsSorted ||
                targetRow is null ||
                di.Source != _source)
            {
                data = null;
                position = TreeDataGridRowDropPosition.None;
                return false;
            }

            var targetIndex = _source.Rows.RowIndexToModelIndex(targetRow.RowIndex);
            position = GetDropPosition(_source, e, targetRow);

            // We can't drop rows into themselves or their descendents.
            foreach (var sourceIndex in di.Indexes)
            {
                if (sourceIndex.IsAncestorOf(targetIndex) ||
                    (sourceIndex == targetIndex && position == TreeDataGridRowDropPosition.Inside))
                {
                    data = null;
                    position = TreeDataGridRowDropPosition.None;
                    return false;
                }
            }

            data = di;
            return true;
        }

        private void OnDragOver(DragEventArgs e)
        {
            if (!TryGetRow(e.Source as Control, out var row))
            {
                e.DragEffects = DragDropEffects.None;
            }

            if (!CalculateAutoDragDrop(row, e, out _, out var adorner))
                e.DragEffects = DragDropEffects.None;

            var route = BuildEventRoute(RowDragOverEvent);

            if (route.HasHandlers)
            {
                var ev = new TreeDataGridRowDragEventArgs(RowDragOverEvent, row, e);
                ev.Position = adorner;
                RaiseEvent(ev);
                adorner = ev.Position;
            }

            if (row != null)
            {
                ShowDragAdorner(row, adorner);
            }

            if (Scroll is ScrollViewer scroller)
            {
                var rowsPosition = e.GetPosition(scroller);

                if (rowsPosition.Y < AutoScrollMargin)
                    AutoScroll(false);
                else if (rowsPosition.Y > Bounds.Height - AutoScrollMargin)
                    AutoScroll(true);
                else
                    _autoScrollTimer?.Stop();
            }
        }

        private void OnDragLeave(RoutedEventArgs e)
        {
            StopDrag();
        }

        private void OnDrop(DragEventArgs e)
        {
            StopDrag();

            TryGetRow(e.Source as Control, out var row);

            var autoDrop = CalculateAutoDragDrop(row, e, out var data, out var position);
            var route = BuildEventRoute(RowDropEvent);

            if (route.HasHandlers)
            {
                var ev = new TreeDataGridRowDragEventArgs(RowDropEvent, row, e);
                ev.Position = position;
                RaiseEvent(ev);

                if (ev.Handled || e.DragEffects != DragDropEffects.Move)
                    return;

                position = ev.Position;
            }

            if (autoDrop &&
                _source is not null &&
                row is not null &&
                position != TreeDataGridRowDropPosition.None)
            {
                var targetIndex = _source.Rows.RowIndexToModelIndex(row.RowIndex);
                _source.DragDropRows(_source, data!.Indexes, targetIndex, position, e.DragEffects);
            }
        }

        private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            if (Scroll is not null && _headerScroll is not null && !MathUtilities.IsZero(e.OffsetDelta.X))
                _headerScroll.Offset = _headerScroll.Offset.WithX(Scroll.Offset.X);
        }

        private void OnHeaderScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            if (Scroll is not null && _headerScroll is not null && !MathUtilities.IsZero(e.OffsetDelta.X))
                Scroll.Offset = Scroll.Offset.WithX(_headerScroll.Offset.X);
        }

        private void OnAutoScrollTick(object? sender, EventArgs e)
        {
            if (Scroll is ScrollViewer scroll)
            {
                if (!_autoScrollDirection)
                    scroll.LineUp();
                else
                    scroll.LineDown();
            }
        }

        private void OnSelectionInteractionChanged(object? sender, EventArgs e)
        {
            RowsPresenter?.UpdateSelection(SelectionInteraction);
        }

        private void OnSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ITreeDataGridSource.Selection))
            {
                SelectionInteraction = Source?.Selection as ITreeDataGridSelectionInteraction;
                RowsPresenter?.UpdateSelection(SelectionInteraction);
            }
        }

        private void OnSourceSorted()
        {
            RowsPresenter?.RecycleAllElements();
            RowsPresenter?.InvalidateMeasure();
        }

        private static TreeDataGridRowDropPosition GetDropPosition(
            ITreeDataGridSource source,
            DragEventArgs e,
            TreeDataGridRow row)
        {
            var rowY = e.GetPosition(row).Y / row.Bounds.Height;

            if (source.IsHierarchical)
            {
                if (rowY < 0.33)
                    return TreeDataGridRowDropPosition.Before;
                else if (rowY > 0.66)
                    return TreeDataGridRowDropPosition.After;
                else
                    return TreeDataGridRowDropPosition.Inside;
            }
            else
            {
                if (rowY < 0.5)
                    return TreeDataGridRowDropPosition.Before;
                else
                    return TreeDataGridRowDropPosition.After;
            }
        }
    }
}
