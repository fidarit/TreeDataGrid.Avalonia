using System;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    ///   Presents and manages rows in a <see cref="TreeDataGrid" /> control.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TreeDataGridRowsPresenter is responsible for creating, recycling, and arranging rows
    ///     vertically in a TreeDataGrid. It handles the virtualization of rows, ensuring that only
    ///     visible rows are realized in the visual tree to improve performance.
    ///   </para>
    ///   <para>
    ///     This presenter is typically used within a <see cref="TreeDataGrid" /> control template to
    ///     display the rows for that control.
    ///   </para>
    /// </remarks>
    public class TreeDataGridRowsPresenter : TreeDataGridPresenterBase<IRow>, IChildIndexProvider
    {
        /// <summary>
        ///   Defines the <see cref="Columns" /> property.
        /// </summary>
        public static readonly DirectProperty<TreeDataGridRowsPresenter, IColumns?> ColumnsProperty =
            AvaloniaProperty.RegisterDirect<TreeDataGridRowsPresenter, IColumns?>(
                nameof(Columns),
                o => o.Columns,
                (o, v) => o.Columns = v);

        private IColumns? _columns;

        /// <summary>
        ///   Occurs when the index of a child element in the presenter changes.
        /// </summary>
        /// <remarks>
        ///   This event is raised when rows are realized, unrealized, or when their indices change
        ///   due to insertions or removals.
        /// </remarks>
        public event EventHandler<ChildIndexChangedEventArgs>? ChildIndexChanged;

        /// <summary>
        ///   Gets or sets the columns collection used to define the structure of rows.
        /// </summary>
        public IColumns? Columns
        {
            get => _columns;
            set => SetAndRaise(ColumnsProperty, ref _columns, value);
        }

        /// <summary>
        ///   Gets the orientation in which rows are arranged.
        /// </summary>
        /// <value>
        ///   Always <see cref="Orientation.Vertical" />.
        /// </value>
        /// <remarks>
        ///   Rows in a TreeDataGrid are always arranged vertically, with each row representing
        ///   a different data item.
        /// </remarks>
        protected override Orientation Orientation => Orientation.Vertical;

        /// <summary>
        ///   Gets the element at the specified position.
        /// </summary>
        /// <param name="position">The vertical position at which to find an element.</param>
        /// <returns>
        ///   A tuple containing:
        ///   - The index of the row at the specified position
        ///   - The vertical position of the row
        /// </returns>
        /// <remarks>
        ///   This method delegates to the <see cref="IRows.GetRowAt(double)" /> method to determine
        ///   which row is at the specified vertical position.
        /// </remarks>
        protected override (int index, double position) GetElementAt(double position)
        {
            return ((IRows)Items!).GetRowAt(position);
        }

        /// <summary>
        ///   Prepares a row element for display with the specified data.
        /// </summary>
        /// <param name="element">The row element to prepare.</param>
        /// <param name="rowModel">The row model containing the data.</param>
        /// <param name="index">The index of the row.</param>
        /// <remarks>
        ///   This method initializes the row with its data, the current selection state, and
        ///   raises the <see cref="ChildIndexChanged" /> event.
        /// </remarks>
        protected override void RealizeElement(Control element, IRow rowModel, int index)
        {
            var row = (TreeDataGridRow)element;
            row.Realize(ElementFactory, GetSelection(), Columns, (IRows?)Items, index);
            ChildIndexChanged?.Invoke(this, new ChildIndexChangedEventArgs(element, index));
        }

        /// <summary>
        ///   Updates the index of a row element when its position in the collection changes.
        /// </summary>
        /// <param name="element">The row element to update.</param>
        /// <param name="oldIndex">The old index of the row.</param>
        /// <param name="newIndex">The new index of the row.</param>
        /// <remarks>
        ///   This method updates the row's index and raises the <see cref="ChildIndexChanged" /> event.
        /// </remarks>
        protected override void UpdateElementIndex(Control element, int oldIndex, int newIndex)
        {
            ((TreeDataGridRow)element).UpdateIndex(newIndex);
            ChildIndexChanged?.Invoke(this, new ChildIndexChangedEventArgs(element, newIndex));
        }

        /// <summary>
        ///   Releases resources used by a row element and prepares it for reuse.
        /// </summary>
        /// <param name="element">The row element to unrealize.</param>
        /// <remarks>
        ///   This method cleans up the row and raises the <see cref="ChildIndexChanged" /> event.
        /// </remarks>
        protected override void UnrealizeElement(Control element)
        {
            ((TreeDataGridRow)element).Unrealize();
            ChildIndexChanged?.Invoke(this, new ChildIndexChangedEventArgs(element, ((TreeDataGridRow)element).RowIndex));
        }

        /// <summary>
        ///   Releases resources used by a row element when its data item is removed from the collection.
        /// </summary>
        /// <param name="element">The row element to unrealize.</param>
        /// <remarks>
        ///   This method is called when a row's data item is removed from the source collection.
        ///   It performs special cleanup for removed items and raises the <see cref="ChildIndexChanged" /> event.
        /// </remarks>
        protected override void UnrealizeElementOnItemRemoved(Control element)
        {
            ((TreeDataGridRow)element).UnrealizeOnItemRemoved();
            ChildIndexChanged?.Invoke(this, new ChildIndexChangedEventArgs(element, ((TreeDataGridRow)element).RowIndex));
        }

        /// <inheritdoc />
        protected override Size MeasureOverride(Size availableSize)
        {
            var result = base.MeasureOverride(availableSize);

            // If we have no rows, then get the width from the columns.
            if (Columns is not null && (Items is null || Items.Count == 0))
                result = result.WithWidth(Columns.GetEstimatedWidth(availableSize.Width));

            return result;
        }

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size finalSize)
        {
            Columns?.CommitActualWidths();
            return base.ArrangeOverride(finalSize);
        }

        /// <inheritdoc />
        protected override void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
        {
            base.OnEffectiveViewportChanged(sender, e);
            Columns?.ViewportChanged(Viewport);
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            if (change.Property == ColumnsProperty)
            {
                var oldValue = change.GetOldValue<IColumns>();
                var newValue = change.GetNewValue<IColumns>();

                if (oldValue is object)
                    oldValue.LayoutInvalidated -= OnColumnLayoutInvalidated;
                if (newValue is object)
                    newValue.LayoutInvalidated += OnColumnLayoutInvalidated;

                // When for existing Presenter Columns would be recreated they won't get Viewport set so we need to track that
                // and pass Viewport for a newly created object. 
                if (oldValue != null && newValue != null)
                {
                    newValue.ViewportChanged(Viewport);
                }
            }

            base.OnPropertyChanged(change);
        }

        internal void UpdateSelection(ITreeDataGridSelectionInteraction? selection)
        {
            foreach (var element in RealizedElements)
            {
                if (element is TreeDataGridRow { RowIndex: >= 0 } row)
                    row.UpdateSelection(selection);
            }
        }

        private void OnColumnLayoutInvalidated(object? sender, EventArgs e)
        {
            InvalidateMeasure();

            foreach (var element in RealizedElements)
            {
                if (element is TreeDataGridRow row)
                    row.CellsPresenter?.InvalidateMeasure();
            }
        }

        private ITreeDataGridSelectionInteraction? GetSelection()
        {
            return this.FindAncestorOfType<TreeDataGrid>()?.SelectionInteraction;
        }

        public int GetChildIndex(ILogical child)
        {
            if (child is TreeDataGridRow row)
            {
                return row.RowIndex;
            }
            return -1;

        }

        public bool TryGetTotalCount(out int count)
        {
            if (Items != null)
            {
                count = Items.Count;
                return true;
            }
            count = 0;
            return false;
        }
    }
}
