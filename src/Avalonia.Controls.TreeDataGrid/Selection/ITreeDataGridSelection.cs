using System.Collections;

namespace Avalonia.Controls.Selection
{
    /// <summary>
    ///   Base interface for TreeDataGrid selection models that provides common functionality
    ///   for both row and cell selection.
    /// </summary>
    /// <remarks>
    ///   ITreeDataGridSelection serves as a common base interface for various selection models
    ///   in the TreeDataGrid control, including <see cref="ITreeDataGridRowSelectionModel" />
    ///   and <see cref="ITreeDataGridCellSelectionModel" />.
    /// </remarks>
    public interface ITreeDataGridSelection
    {
        /// <summary>
        ///   Gets or sets the data source for the selection model.
        /// </summary>
        IEnumerable? Source { get; set; }
    }
}
