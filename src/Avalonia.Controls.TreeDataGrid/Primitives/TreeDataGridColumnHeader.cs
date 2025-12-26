using System;
using System.ComponentModel;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Input;
using Avalonia.Utilities;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    ///   A control which displays a column header in a <see cref="TreeDataGrid" /> control.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TreeDataGridColumnHeader is responsible for displaying column headers in a TreeDataGrid and
    ///     provides functionality for column resizing and sorting.
    ///   </para>
    ///   <para>
    ///     Column headers can be clicked to sort the data in the TreeDataGrid, and if resizing is
    ///     enabled, they can be resized by dragging the edge of the header.
    ///   </para>
    ///   <para>
    ///     This class supports the following pseudo-classes:
    ///     <list type="bullet">
    ///       <item>
    ///         <description>
    ///           :resizable - Set when the column can be resized by the user
    ///         </description>
    ///       </item>
    ///     </list>
    ///   </para>
    /// </remarks>
    public class TreeDataGridColumnHeader : Button
    {
        /// <summary>
        ///   Defines the <see cref="CanUserResize" /> property.
        /// </summary>
        public static readonly DirectProperty<TreeDataGridColumnHeader, bool> CanUserResizeProperty =
            AvaloniaProperty.RegisterDirect<TreeDataGridColumnHeader, bool>(
                nameof(CanUserResize),
                x => x.CanUserResize);

        /// <summary>
        ///   Defines the <see cref="Header" /> property.
        /// </summary>
        public static readonly DirectProperty<TreeDataGridColumnHeader, object?> HeaderProperty =
            AvaloniaProperty.RegisterDirect<TreeDataGridColumnHeader, object?>(
                nameof(Header),
                o => o.Header);

        /// <summary>
        ///   Defines the <see cref="SortDirection" /> property.
        /// </summary>
        public static readonly DirectProperty<TreeDataGridColumnHeader, ListSortDirection?> SortDirectionProperty =
            AvaloniaProperty.RegisterDirect<TreeDataGridColumnHeader, ListSortDirection?>(
                nameof(SortDirection),
                o => o.SortDirection);

        private bool _canUserResize;
        private IColumns? _columns;
        private object? _header;
        private IColumn? _model;
        private ListSortDirection? _sortDirection;
        private TreeDataGrid? _owner;
        private Thumb? _resizer;

        /// <summary>
        ///   Gets a value indicating whether the column can be resized by the user.
        /// </summary>
        /// <value>
        ///   true if the column can be resized by the user; otherwise, false.
        /// </value>
        /// <remarks>
        ///   This value is derived from the column model's <see cref="IColumn.CanUserResize" />
        ///   property and the owning <see cref="TreeDataGrid.CanUserResizeColumns" /> property.
        /// </remarks>
        public bool CanUserResize
        {
            get => _canUserResize;
            private set => SetAndRaise(CanUserResizeProperty, ref _canUserResize, value);
        }

        /// <summary>
        ///   Gets the index of the column in the <see cref="TreeDataGrid" />.
        /// </summary>
        /// <value>
        ///   The zero-based index of the column, or -1 if the column header is not realized.
        /// </value>
        public int ColumnIndex { get; private set; }

        /// <summary>
        ///   Gets the content to display in the column header.
        /// </summary>
        /// <value>
        ///   The header content, which can be a string, a data template, or any other object.
        /// </value>
        /// <remarks>
        ///   This value is obtained from the <see cref="IColumn.Header" /> property of the column
        ///   model.
        /// </remarks>
        public object? Header
        {
            get => _header;
            private set => SetAndRaise(HeaderProperty, ref _header, value);
        }

        /// <summary>
        ///   Gets the current sort direction of the column.
        /// </summary>
        /// <value>
        ///   The sort direction (ascending or descending), or null if the column is not sorted.
        /// </value>
        /// <remarks>
        ///   This value is obtained from the <see cref="IColumn.SortDirection" /> property of the
        ///   column model. The sort direction is updated when the user clicks on the column header
        ///   and sorting is enabled.
        /// </remarks>
        public ListSortDirection? SortDirection
        {
            get => _sortDirection;
            private set => SetAndRaise(SortDirectionProperty, ref _sortDirection, value);
        }

        /// <summary>
        ///   Prepares the column header for display with the specified data.
        /// </summary>
        /// <param name="columns">The columns collection.</param>
        /// <param name="columnIndex">The index of the column.</param>
        /// <exception cref="InvalidOperationException">
        ///   The column header is already realized.
        /// </exception>
        /// <remarks>
        ///   This method is called by the <see cref="TreeDataGridColumnHeadersPresenter" /> when a
        ///   column header  needs to be prepared for display. It initializes the header with the
        ///   column data and subscribes to property change notifications from the column model.
        /// </remarks>
        public void Realize(IColumns columns, int columnIndex)
        {
            if (_model is object)
                throw new InvalidOperationException("Column header is already realized.");

            _columns = columns;
            _model = columns[columnIndex];
            ColumnIndex = columnIndex;
            UpdatePropertiesFromModel();

            if (_model is INotifyPropertyChanged newInpc)
                newInpc.PropertyChanged += OnModelPropertyChanged;
        }

        /// <summary>
        ///   Updates the column index of this header.
        /// </summary>
        /// <param name="columnIndex">The new column index.</param>
        /// <remarks>
        ///   This method is called when the index of the column changes, such as when columns are
        ///   inserted or removed to the left of this column.
        /// </remarks>
        public void UpdateColumnIndex(int columnIndex)
        {
            ColumnIndex = columnIndex;
        }

        /// <summary>
        ///   Releases resources used by the column header and prepares it for reuse.
        /// </summary>
        /// <remarks>
        ///   This method is called by the <see cref="TreeDataGridColumnHeadersPresenter" /> when a
        ///   column header is no longer needed for display. It unsubscribes from property change
        ///   notifications from the column model and clears the header's state.
        /// </remarks>
        public void Unrealize()
        {
            if (_model is INotifyPropertyChanged oldInpc)
                oldInpc.PropertyChanged -= OnModelPropertyChanged;

            _columns = null;
            _model = null;
            ColumnIndex = -1;
            UpdatePropertiesFromModel();
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _resizer = e.NameScope.Find<Thumb>("PART_Resizer");

            if (_resizer is not null)
            {
                _resizer.DragDelta += ResizerDragDelta;
                _resizer.DoubleTapped += ResizerDoubleTapped;
            }
        }

        private void ResizerDoubleTapped(object? sender, Interactivity.RoutedEventArgs e)
        {
            _columns?.SetColumnWidth(ColumnIndex, GridLength.Auto);
        }

        /// <inheritdoc />
        protected override Size MeasureOverride(Size availableSize)
        {
            var result = base.MeasureOverride(availableSize);

            // HACKFIX for #83. Seems that cells are getting truncated at times due to DPI scaling.
            // New text stack in Avalonia 11.0 should fix this but until then a hack to add a pixel
            // to cell size seems to fix it.
            result = result.Inflate(new Thickness(1, 0));

            return result;
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            if (change.Property == CanUserResizeProperty)
            {
                PseudoClasses.Set(":resizable", change.GetNewValue<bool>());
            }
            else if (change.Property == DataContextProperty)
            {
                var oldModel = change.GetOldValue<object?>() as IColumn;
                var newModel = change.GetNewValue<object?>() as IColumn;

                if (oldModel is INotifyPropertyChanged oldInpc)
                    oldInpc.PropertyChanged -= OnModelPropertyChanged;
                if (newModel is INotifyPropertyChanged newInpc)
                    newInpc.PropertyChanged += OnModelPropertyChanged;

                UpdatePropertiesFromModel();
            }
            else if (change.Property == ParentProperty)
            {
                if (_owner is not null)
                    _owner.PropertyChanged -= OnOwnerPropertyChanged;
                _owner = change.GetNewValue<StyledElement>()?.TemplatedParent as TreeDataGrid;
                if (_owner is not null)
                    _owner.PropertyChanged += OnOwnerPropertyChanged;
                UpdatePropertiesFromModel();
            }

            base.OnPropertyChanged(change);
        }

        private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IColumn.CanUserResize) ||
                e.PropertyName == nameof(IColumn.Header) ||
                e.PropertyName == nameof(IColumn.SortDirection)
                || e.PropertyName == nameof(IColumn.IsVisible)
                )
                UpdatePropertiesFromModel();
        }

        private void OnOwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (_owner is null)
                return;
            if (e.Property == TreeDataGrid.CanUserResizeColumnsProperty)
                CanUserResize = _model?.CanUserResize ?? _owner.CanUserResizeColumns;
        }

        private void ResizerDragDelta(object? sender, VectorEventArgs e)
        {
            if (_columns is null || _model is null || MathUtilities.IsZero(e.Vector.X))
                return;

            var pixelWidth = _model.Width.IsAbsolute ? _model.Width.Value : Bounds.Width;

            if (double.IsNaN(pixelWidth) || double.IsInfinity(pixelWidth) || pixelWidth + e.Vector.X < 0)
                return;

            var width = new GridLength(pixelWidth + e.Vector.X, GridUnitType.Pixel);
            _columns.SetColumnWidth(ColumnIndex, width);
        }

        private void UpdatePropertiesFromModel()
        {
            var oldVisibility = IsVisible;
            CanUserResize = _model?.CanUserResize ?? _owner?.CanUserResizeColumns ?? false;
            Header = _model?.Header;
            Tag = _model?.Tag;
            SortDirection = _model?.SortDirection;
            IsVisible = _model?.IsVisible == true;
            if (IsVisible != oldVisibility)
            {
                _columns?.InvalidateLayout();
            }            
        }
    }
}
