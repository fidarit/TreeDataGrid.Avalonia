using System;

namespace Avalonia.Controls
{
    /// <summary>
    ///   Provides data for events related to cell operations in a <see cref="TreeDataGrid" />
    ///   control.
    /// </summary>
    /// <remarks>
    ///   This class is used in events such as <see cref="TreeDataGrid.CellPrepared" />,
    ///   <see cref="TreeDataGrid.CellClearing" />,  and <see cref="TreeDataGrid.CellValueChanged" />
    ///   to provide information about the cell that triggered the event.
    /// </remarks>
    public class TreeDataGridCellEventArgs
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="TreeDataGridCellEventArgs" /> class with
        ///   the specified cell, column index, and row index.
        /// </summary>
        /// <param name="cell">The cell control that triggered the event.</param>
        /// <param name="columnIndex">The index of the column that contains the cell.</param>
        /// <param name="rowIndex">The index of the row that contains the cell.</param>
        public TreeDataGridCellEventArgs(Control cell, int columnIndex, int rowIndex)
        {
            Cell = cell;
            ColumnIndex = columnIndex;
            RowIndex = rowIndex;
        }

        internal TreeDataGridCellEventArgs()
        {
            Cell = null!;
        }

        /// <summary>
        ///   Gets the cell control that triggered the event.
        /// </summary>
        /// <value>
        ///   A <see cref="Control" /> that represents the cell in the TreeDataGrid.
        /// </value>
        public Control Cell { get; private set; }
        /// <summary>
        ///   Gets the index of the column that contains the cell.
        /// </summary>
        /// <value>The zero-based index of the column.</value>
        public int ColumnIndex { get; private set; }
        /// <summary>
        ///   Gets the index of the row that contains the cell.
        /// </summary>
        /// <value>The zero-based index of the row.</value>
        public int RowIndex { get; private set; }

        internal void Update(Control? cell, int columnIndex, int rowIndex)
        {
            if (cell is object && Cell is object)
                throw new NotSupportedException("Nested TreeDataGrid cell prepared/clearing detected.");

            Cell = cell!;
            ColumnIndex = columnIndex;
            RowIndex = rowIndex;
        }
    }
}
