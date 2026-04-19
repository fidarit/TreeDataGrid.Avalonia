using System.Collections.Generic;
using Avalonia.Input;

namespace Avalonia.Controls.Models.TreeDataGrid
{
    /// <summary>
    /// Holds information about an automatic row drag/drop operation carried out
    /// by <see cref="Avalonia.Controls.TreeDataGrid.AutoDragDropRows"/>.
    /// </summary>
    /// <param name="source">The source of the drag operation/</param>
    /// <param name="indexes">The indexes being dragged.</param>
    public class DragInfo(ITreeDataGridSource source, IEnumerable<IndexPath> indexes)
    {
        /// <summary>
        /// Defines the data format for dragging TreeDataGrid rows.
        /// </summary>
        public static readonly DataFormat<DragInfo> Format = DataFormat.CreateInProcessFormat<DragInfo>("TreeDataGridDragInfo");

        /// <summary>
        /// Gets the <see cref="ITreeDataGridSource"/> that rows are being dragged from.
        /// </summary>
        public ITreeDataGridSource Source { get; } = source;

        /// <summary>
        /// Gets or sets the model indexes of the rows being dragged.
        /// </summary>
        public IEnumerable<IndexPath> Indexes { get; } = indexes;
    }
}
