using System;
using System.Collections.Generic;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Layout;
using Avalonia.LogicalTree;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    ///   Presents and manages column headers in a <see cref="TreeDataGrid" /> control.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TreeDataGridColumnHeadersPresenter is responsible for creating, recycling, and arranging
    ///     column headers horizontally at the top of a TreeDataGrid. It handles the virtualization of
    ///     column headers, ensuring that only visible headers are realized in the visual tree to
    ///     improve performance.
    ///   </para>
    ///   <para>
    ///     This presenter coordinates with the <see cref="IColumns" /> collection to manage column
    ///     widths, measurement, and layout. It also handles column resizing by committing the actual
    ///     column widths during arrangement.
    ///   </para>
    /// </remarks>
    public class TreeDataGridColumnHeadersPresenter : TreeDataGridColumnarPresenterBase<IColumn>, IChildIndexProvider
    {
        /// <summary>
        ///   Occurs when the index of a child element in the presenter changes.
        /// </summary>
        /// <remarks>
        ///   This event is raised when column headers are realized, unrealized, or when their
        ///   indices change due to column insertions or removals.
        /// </remarks>
        public event EventHandler<ChildIndexChangedEventArgs>? ChildIndexChanged;

        /// <summary>
        ///   Gets the orientation in which the column headers are arranged.
        /// </summary>
        /// <value>
        ///   Always <see cref="Orientation.Horizontal" />.
        /// </value>
        /// <remarks>
        ///   Column headers are always arranged horizontally, with each header representing a
        ///   different column.
        /// </remarks>
        protected override Orientation Orientation => Orientation.Horizontal;

        /// <summary>
        ///   Arranges column headers and commits their actual widths.
        /// </summary>
        /// <param name="finalSize">
        ///   The final area within the parent that this element should use to arrange itself and its
        ///   children.
        /// </param>
        /// <returns>The actual size used by the element.</returns>
        /// <remarks>
        ///   Before arranging the column headers, this method calls
        ///   <see cref="IColumns.CommitActualWidths()" /> to ensure that column widths are finalized
        ///   based on their measured sizes.
        /// </remarks>
        protected override Size ArrangeOverride(Size finalSize)
        {
            (Items as IColumns)?.CommitActualWidths();
            return base.ArrangeOverride(finalSize);
        }

        /// <summary>
        ///   Measures a column header and notifies the columns collection of the measured size.
        /// </summary>
        /// <param name="index">The index of the column header.</param>
        /// <param name="element">The column header element.</param>
        /// <param name="availableSize">The available size for the column header.</param>
        /// <returns>The size that the column header will use.</returns>
        /// <remarks>
        ///   This method measures the column header and then calls
        ///   <see cref="IColumns.CellMeasured(int, int, Size)" /> to allow
        ///   the columns collection to adjust the size based on column width constraints.
        /// </remarks>
        protected override Size MeasureElement(int index, Control element, Size availableSize)
        {
            if (Items is IColumns columns)
            {
                element.Measure(availableSize);
                return columns.CellMeasured(index, -1, element.DesiredSize);
            }
            return default;
        }

        /// <summary>
        ///   Prepares a column header for display.
        /// </summary>
        /// <param name="element">The column header element.</param>
        /// <param name="column">The column model.</param>
        /// <param name="index">The index of the column.</param>
        protected override void RealizeElement(Control element, IColumn column, int index)
        {
            ((TreeDataGridColumnHeader)element).Realize((IColumns)Items!, index);
            ChildIndexChanged?.Invoke(this, new ChildIndexChangedEventArgs(element, index));
        }

        /// <summary>
        ///   Updates the index of a column header when its position changes.
        /// </summary>
        /// <param name="element">The column header element.</param>
        /// <param name="oldIndex">The old index of the column.</param>
        /// <param name="newIndex">The new index of the column.</param>
        protected override void UpdateElementIndex(Control element, int oldIndex, int newIndex)
        {
            ((TreeDataGridColumnHeader)element).UpdateColumnIndex(newIndex);
            ChildIndexChanged?.Invoke(this, new ChildIndexChangedEventArgs(element, newIndex));
        }

        /// <summary>
        ///   Releases resources used by a column header and prepares it for reuse.
        /// </summary>
        /// <param name="element">The column header element to unrealize.</param>
        protected override void UnrealizeElement(Control element)
        {
            ((TreeDataGridColumnHeader)element).Unrealize();
            ChildIndexChanged?.Invoke(this, new ChildIndexChangedEventArgs(element, ((TreeDataGridColumnHeader)element).ColumnIndex));
        }

        /// <summary>
        ///   Handles changes to the presenter's properties.
        /// </summary>
        /// <param name="change">Information about the property that changed.</param>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            if (change.Property == ItemsProperty)
            {
                var oldValue = change.GetOldValue<IReadOnlyList<IColumn>?>();
                var newValue = change.GetNewValue<IReadOnlyList<IColumn>?>();

                if (oldValue is IColumns oldColumns)
                    oldColumns.LayoutInvalidated -= OnColumnLayoutInvalidated;
                if (newValue is IColumns newColumns)
                    newColumns.LayoutInvalidated += OnColumnLayoutInvalidated;
            }

            base.OnPropertyChanged(change);
        }

        private void OnColumnLayoutInvalidated(object? sender, EventArgs e)
        {
            InvalidateMeasure();
        }

        /// <summary>
        ///   Gets the index of a logical child element.
        /// </summary>
        /// <param name="child">The logical child element.</param>
        /// <returns>
        ///   The index of the child element, or -1 if the element is not a column header or is not found.
        /// </returns>
        /// <remarks>
        ///   This method is part of the <see cref="IChildIndexProvider" /> interface implementation,
        ///   which allows the presenter to provide indices for its logical children.
        /// </remarks>
        public int GetChildIndex(ILogical child)
        {
            if (child is TreeDataGridColumnHeader header)
            {
                return header.ColumnIndex;
            }
            return -1;
        }

        /// <summary>
        ///   Tries to get the total count of child elements.
        /// </summary>
        /// <param name="count">When this method returns, contains the total count of child elements if available.</param>
        /// <returns>
        ///   true if the count is available; otherwise, false.
        /// </returns>
        /// <remarks>
        ///   This method is part of the <see cref="IChildIndexProvider" /> interface implementation,
        ///   which allows the presenter to provide information about its logical children.
        /// </remarks>
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
