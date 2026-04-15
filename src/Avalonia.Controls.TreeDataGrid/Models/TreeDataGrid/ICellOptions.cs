namespace Avalonia.Controls.Models.TreeDataGrid
{
    /// <summary>
    /// Holds less commonly-used options for an <see cref="IColumn"/>.
    /// </summary>
    public interface ICellOptions
    {
        /// <summary>
        /// Gets the gesture(s) that will cause the cell to enter edit mode.
        /// </summary>
        BeginEditGestures BeginEditGestures { get; }
    }
}
