using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    /// <summary>
    ///   Specifies the position where a dragged row should be dropped relative to the target row.
    /// </summary>
    /// <remarks>
    ///   This enum is used during drag and drop operations in <see cref="TreeDataGrid" /> to indicate
    ///   where a dragged row should be positioned in relation to the target row.
    /// </remarks>
    public enum TreeDataGridRowDropPosition
    {
        /// <summary>
        ///   No drop position is specified or the drop operation is not allowed.
        /// </summary>
        None,
        /// <summary>
        ///   The row should be dropped before (above) the target row.
        /// </summary>
        Before,
        /// <summary>
        ///   The row should be dropped after (below) the target row.
        /// </summary>
        After,
        /// <summary>
        ///   The row should be dropped as a child of the target row.
        /// </summary>
        /// <remarks>
        ///   This option is only meaningful for hierarchical data structures. When used,
        ///   the dragged row becomes a child node of the target row.
        /// </remarks>
        Inside,
    }

    /// <summary>
    ///   Provides data for the <see cref="TreeDataGrid.RowDragOver" /> and
    ///   <see cref="TreeDataGrid.RowDrop" /> events.
    /// </summary>
    public class TreeDataGridRowDragEventArgs : RoutedEventArgs
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="TreeDataGridRowDragEventArgs" /> class.
        /// </summary>
        /// <param name="routedEvent">The event being raised.</param>
        /// <param name="row">The row that is being dragged over.</param>
        /// <param name="inner">The inner drag event args.</param>
        public TreeDataGridRowDragEventArgs(RoutedEvent routedEvent, TreeDataGridRow? row, DragEventArgs inner)
            : base(routedEvent)
        {
            TargetRow = row;
            Inner = inner;
        }

        /// <summary>
        ///   Gets the <see cref="DragEventArgs" /> that describes the drag/drop operation.
        /// </summary>
        public DragEventArgs Inner { get; }

        /// <summary>
        ///   Gets the row being dragged over.
        /// </summary>
        public TreeDataGridRow? TargetRow { get; }

        /// <summary>
        ///   Gets or sets a value indicating the how the data should be dropped into
        ///   the <see cref="TargetRow" />.
        /// </summary>
        /// <remarks>
        ///   For drag operations, the value of this property controls the adorner displayed when
        ///   dragging. For drop operations, controls the final location of the drop.
        /// </remarks>
        public TreeDataGridRowDropPosition Position { get; set; }
    }
}
