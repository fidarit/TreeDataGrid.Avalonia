using System;
using Avalonia.Controls.Primitives;

namespace Avalonia.Controls
{
    /// <summary>
    ///   Provides data for events related to row operations in a <see cref="TreeDataGrid" /> control.
    /// </summary>
    /// <remarks>
    ///   This class is used in events such as <see cref="TreeDataGrid.RowPrepared" /> and
    ///   <see cref="TreeDataGrid.RowClearing" /> to provide information about the row that triggered
    ///   the event.
    /// </remarks>
    public class TreeDataGridRowEventArgs
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="TreeDataGridRowEventArgs" /> class with the
        ///   specified row and row index.
        /// </summary>
        /// <param name="row">The row control that triggered the event.</param>
        /// <param name="rowIndex">The index of the row in the TreeDataGrid.</param>
        public TreeDataGridRowEventArgs(TreeDataGridRow row, int rowIndex)
        {
            Row = row;
            RowIndex = rowIndex;
        }

        internal TreeDataGridRowEventArgs()
        {
            Row = null!;
        }

        /// <summary>
        ///   Gets the row control that triggered the event.
        /// </summary>
        /// <value>
        ///   A <see cref="TreeDataGridRow" /> that represents the row in the TreeDataGrid.
        /// </value>
        public TreeDataGridRow Row { get; private set; }
        /// <summary>
        ///   Gets the index of the row that triggered the event.
        /// </summary>
        /// <value>The zero-based index of the row in the TreeDataGrid.</value>
        public int RowIndex { get; private set; }

        internal void Update(TreeDataGridRow? row, int rowIndex)
        {
            if (row is object && Row is object)
                throw new NotSupportedException("Nested TreeDataGrid row prepared/clearing detected.");

            Row = row!;
            RowIndex = rowIndex;
        }
    }
}
