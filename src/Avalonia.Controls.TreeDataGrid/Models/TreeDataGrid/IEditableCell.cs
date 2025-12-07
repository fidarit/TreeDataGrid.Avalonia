namespace Avalonia.Controls.Models.TreeDataGrid
{
    /// <summary>
    /// Represents a cell in an <see cref="ITreeDataGridSource"/> that supports editing.
    /// </summary>
    public interface IEditableCell : ICell
    {
        /// <summary>
        /// Gets a value indicating whether the cell is read-only and cannot be edited.
        /// </summary>
        bool IsReadOnly { get; }
    }
}
