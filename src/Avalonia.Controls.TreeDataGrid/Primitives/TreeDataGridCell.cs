using System;
using System.ComponentModel;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    ///   Base class for controls which display cells in a <see cref="TreeDataGrid" /> control.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TreeDataGridCell is the base class for cells in a TreeDataGrid control. It provides core
    ///     functionality for cell realization, selection, and editing. Cells are created by the
    ///     <see cref="TreeDataGridElementFactory" /> and are reused as the user scrolls through the
    ///     grid.
    ///   </para>
    ///   <para>
    ///     This class implements the following pseudo-classes:
    ///     <list type="bullet">
    ///       <item>
    ///         <description>:editing - Set when the cell is in edit mode</description>
    ///       </item>
    ///       <item>
    ///         <description>:selected - Set when the cell is selected</description>
    ///       </item>
    ///     </list>
    ///   </para>
    ///   <para>
    ///     Cell editing can be triggered through various gestures including double-tap, F2, or single
    ///     tap, depending on the <see cref="BeginEditGestures" /> settings of the cell's model.
    ///   </para>
    /// </remarks>
    [PseudoClasses(":editing")]
    public abstract class TreeDataGridCell : TemplatedControl, ITreeDataGridCell
    {
        /// <summary>
        ///   Defines the <see cref="IsSelected" /> property.
        /// </summary>
        public static readonly DirectProperty<TreeDataGridCell, bool> IsSelectedProperty =
            AvaloniaProperty.RegisterDirect<TreeDataGridCell, bool>(
                nameof(IsSelected),
                o => o.IsSelected);

        private static readonly Point s_invalidPoint = new Point(double.NaN, double.NaN);
        private bool _isSelected;
        private TreeDataGrid? _treeDataGrid;
        private Point _pressedPoint = s_invalidPoint;

        static TreeDataGridCell()
        {
            FocusableProperty.OverrideDefaultValue<TreeDataGridCell>(true);
            DoubleTappedEvent.AddClassHandler<TreeDataGridCell>((x, e) => x.OnDoubleTapped(e));
        }

        /// <summary>
        ///   Gets the index of the column that this cell belongs to.
        /// </summary>
        /// <value>
        ///   The zero-based column index, or -1 if the cell is not realized.
        /// </value>
        public int ColumnIndex { get; private set; } = -1;
        /// <summary>
        ///   Gets the index of the row that this cell belongs to.
        /// </summary>
        /// <value>
        ///   The zero-based row index, or -1 if the cell is not realized.
        /// </value>
        public int RowIndex { get; private set; } = -1;
        /// <summary>
        ///   Gets a value indicating whether the cell is in edit mode.
        /// </summary>
        public bool IsEditing { get; private set; }
        /// <summary>
        ///   Gets the data model for the cell.
        /// </summary>
        public ICell? Model { get; private set; }

        /// <summary>
        ///   Gets a value indicating whether the cell is selected.
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            private set => SetAndRaise(IsSelectedProperty, ref _isSelected, value);
        }

        /// <summary>
        ///   Gets a value indicating whether the cell is effectively selected, either directly
        ///   or because its containing row is selected.
        /// </summary>
        public bool IsEffectivelySelected
        {
            get => IsSelected || this.FindAncestorOfType<TreeDataGridRow>()?.IsSelected == true;
        }

        /// <summary>
        ///   Prepares the cell for display with the specified data.
        /// </summary>
        /// <param name="factory">The element factory used to create child elements.</param>
        /// <param name="selection">The selection interaction model.</param>
        /// <param name="model">The cell's data model.</param>
        /// <param name="columnIndex">The index of the cell's column.</param>
        /// <param name="rowIndex">The index of the cell's row.</param>
        /// <exception cref="InvalidOperationException">The cell is already realized.</exception>
        /// <exception cref="IndexOutOfRangeException">The column or row index is invalid.</exception>
        /// <remarks>
        ///   This method is called by the TreeDataGrid when a cell needs to be prepared for display.
        ///   Derived classes should call the base implementation and then initialize any cell-specific
        ///   content based on the provided model.
        /// </remarks>
        public virtual void Realize(
            TreeDataGridElementFactory factory,
            ITreeDataGridSelectionInteraction? selection,
            ICell model,
            int columnIndex,
            int rowIndex)
        {
            if (ColumnIndex >= 0 || RowIndex >= 0)
                throw new InvalidOperationException("Cell is already realized.");
            if (columnIndex < 0)
                throw new IndexOutOfRangeException("Invalid column index.");
            if (rowIndex < 0)
                throw new IndexOutOfRangeException("Invalid row index.");

            ColumnIndex = columnIndex;
            RowIndex = rowIndex;
            Model = model;
            IsSelected = selection?.IsCellSelected(columnIndex, rowIndex) ?? false;

            _treeDataGrid?.RaiseCellPrepared(this, columnIndex, RowIndex);
        }

        /// <summary>
        ///   Releases resources used by the cell and prepares it for reuse.
        /// </summary>
        /// <remarks>
        ///   This method is called by the TreeDataGrid when a cell is no longer needed for display.
        ///   Derived classes should call the base implementation after performing any cell-specific
        ///   cleanup.
        /// </remarks>
        public virtual void Unrealize()
        {
            _treeDataGrid?.RaiseCellClearing(this, ColumnIndex, RowIndex);
            ColumnIndex = RowIndex = -1;
            Model = null;
        }

        /// <summary>
        ///   Begins editing the cell's content.
        /// </summary>
        /// <remarks>
        ///   This method puts the cell into edit mode, allowing the user to modify its value.
        ///   If the cell's model implements <see cref="IEditableObject" />, its
        ///   <see cref="IEditableObject.BeginEdit()" /> method will be called.
        /// </remarks>
        protected internal void BeginEdit()
        {
            if (!IsEditing)
            {
                IsEditing = true;
                (Model as IEditableObject)?.BeginEdit();
                PseudoClasses.Add(":editing");
            }
        }

        /// <summary>
        ///   Cancels the current edit operation.
        /// </summary>
        /// <remarks>
        ///   This method exits edit mode and discards any changes made to the cell's value.
        ///   If the cell's model implements <see cref="IEditableObject" />, its
        ///   <see cref="IEditableObject.CancelEdit()" /> method will be called.
        /// </remarks>
        protected internal void CancelEdit()
        {
            if (EndEditCore() && Model is IEditableObject editable)
                editable.CancelEdit();
        }

        /// <summary>
        ///   Commits the current edit operation.
        /// </summary>
        /// <remarks>
        ///   This method exits edit mode and applies any changes made to the cell's value.
        ///   If the cell's model implements <see cref="IEditableObject" />, its
        ///   <see cref="IEditableObject.EndEdit()" /> method will be called and the cell's
        ///   value will be updated.
        /// </remarks>
        protected internal void EndEdit()
        {
            if (EndEditCore() && Model is IEditableObject editable)
            {
                editable.EndEdit();
                UpdateValue();
                RaiseCellValueChanged();
            }
        }

        /// <summary>
        ///   Subscribes to property change notifications from the cell's model.
        /// </summary>
        /// <remarks>
        ///   Derived classes can call this method to begin receiving property change notifications
        ///   from the cell's model if it implements <see cref="INotifyPropertyChanged" />.
        /// </remarks>
        protected void SubscribeToModelChanges()
        {
            if (Model is INotifyPropertyChanged inpc)
                inpc.PropertyChanged += OnModelPropertyChanged;
        }

        /// <summary>
        ///   Unsubscribes from property change notifications from the cell's model.
        /// </summary>
        /// <remarks>
        ///   Derived classes should call this method when they no longer need to receive
        ///   property change notifications from the cell's model.
        /// </remarks>
        protected void UnsubscribeFromModelChanges()
        {
            if (Model is INotifyPropertyChanged inpc)
                inpc.PropertyChanged -= OnModelPropertyChanged;
        }

        /// <summary>
        ///   Updates the cell's value from its model.
        /// </summary>
        /// <remarks>
        ///   Derived classes should override this method to update the cell's visual representation
        ///   based on the current value of its model.
        /// </remarks>
        protected virtual void UpdateValue()
        {
        }

        /// <summary>
        ///   Raises the <see cref="TreeDataGrid.CellValueChanged" /> event.
        /// </summary>
        /// <remarks>
        ///   Derived classes should call this method when the cell's value has changed.
        /// </remarks>
        protected void RaiseCellValueChanged()
        {
            if (!IsEditing && ColumnIndex != -1 && RowIndex != -1)
                _treeDataGrid?.RaiseCellValueChanged(this, ColumnIndex, RowIndex);
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

            // The cell may be realized before being parented. In this case raise the CellPrepared event here.
            if (_treeDataGrid is not null && ColumnIndex >= 0 && RowIndex >= 0)
                _treeDataGrid.RaiseCellPrepared(this, ColumnIndex, RowIndex);
        }

        /// <inheritdoc />
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            if (!IsKeyboardFocusWithin && IsEditing)
                EndEdit();
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

        /// <summary>
        ///   Handles double-tap events on the cell.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        /// <remarks>
        ///   If the cell can be edited and the <see cref="BeginEditGestures.DoubleTap" /> gesture
        ///   is enabled, this method will begin editing the cell.
        /// </remarks>
        protected virtual void OnDoubleTapped(TappedEventArgs e)
        {
            if (Model is not null &&
                !e.Handled &&
                !IsEditing &&
                Model.CanEdit &&
                IsEnabledEditGesture(BeginEditGestures.DoubleTap, Model.EditGestures))
            {
                BeginEdit();
                e.Handled = true;
            }
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (Model is null || e.Handled)
                return;

            if (e.Key == Key.F2 && 
                !IsEditing && 
                Model.CanEdit &&
                IsEnabledEditGesture(BeginEditGestures.F2, Model.EditGestures))
            {
                BeginEdit();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter && IsEditing)
            {
                EndEdit();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape && IsEditing)
            {
                CancelEdit();
                e.Handled = true;
            }
        }

        /// <summary>
        ///   Handles property change notifications from the cell's model.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        /// <remarks>
        ///   Derived classes should override this method to update the cell's visual representation
        ///   when its model's properties change.
        /// </remarks>
        protected virtual void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
        }

        /// <inheritdoc />
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (Model is not null &&
                !e.Handled &&
                !IsEditing &&
                Model.CanEdit &&
                IsEnabledEditGesture(BeginEditGestures.Tap, Model.EditGestures))
            {
                _pressedPoint = e.GetCurrentPoint(null).Position;
                e.Handled = true;
            }
            else
            {
                _pressedPoint = s_invalidPoint;
            }
        }

        /// <inheritdoc />
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            if (Model is not null &&
                !e.Handled &&
                !IsEditing &&
                !double.IsNaN(_pressedPoint.X) &&
                Model.CanEdit &&
                IsEnabledEditGesture(BeginEditGestures.Tap, Model.EditGestures))
            {
                var point = e.GetCurrentPoint(this);
                var settings = TopLevel.GetTopLevel(this)?.PlatformSettings;
                var tapSize = settings?.GetTapSize(point.Pointer.Type) ?? new Size(4, 4);
                var tapRect = new Rect(_pressedPoint, new Size())
                       .Inflate(new Thickness(tapSize.Width, tapSize.Height));

                if (new Rect(Bounds.Size).ContainsExclusive(point.Position) &&
                    tapRect.ContainsExclusive(e.GetCurrentPoint(null).Position))
                {
                    BeginEdit();
                    e.Handled = true;
                }
            }

            _pressedPoint = s_invalidPoint;
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

        internal void UpdateRowIndex(int index)
        {
            if (RowIndex == -1)
                throw new InvalidOperationException("Cell is not realized.");
            RowIndex = index;
        }

        internal void UpdateSelection(ITreeDataGridSelectionInteraction? selection)
        {
            IsSelected = selection?.IsCellSelected(ColumnIndex, RowIndex) ?? false;
        }

        private bool EndEditCore()
        {
            if (IsEditing)
            {
                var restoreFocus = IsKeyboardFocusWithin;
                IsEditing = false;
                PseudoClasses.Remove(":editing");
                if (restoreFocus)
                    Focus();
                return true;
            }

            return false;
        }

        private bool IsEnabledEditGesture(BeginEditGestures gesture, BeginEditGestures enabledGestures)
        {
            if (!enabledGestures.HasFlag(gesture))
                return false;

            return enabledGestures.HasFlag(BeginEditGestures.WhenSelected) ?
                IsEffectivelySelected : true;
        }
    }
}
