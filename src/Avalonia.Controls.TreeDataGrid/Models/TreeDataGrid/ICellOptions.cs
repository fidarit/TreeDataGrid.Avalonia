using Avalonia.Media;

namespace Avalonia.Controls.Models.TreeDataGrid
{
    /// <summary>
    ///   Defines common options that control the behavior of cells in a <see cref="TreeDataGrid" />.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The <see cref="ICellOptions" /> interface serves as a base for various cell option interfaces
    ///     in the TreeDataGrid component. It provides the most fundamental editing behavior options
    ///     that apply across different cell types.
    ///   </para>
    ///   <para>
    ///     Specific cell types extend this interface with additional options relevant to their
    ///     particular functionality, such as <see cref="ITextCellOptions" /> for text formatting
    ///     options or <see cref="ITemplateCellOptions" /> for template-specific options.
    ///   </para>
    ///   <para>
    ///     Column implementations typically provide these options through their respective
    ///     options classes (e.g., <see cref="TextColumnOptions{TModel}" />,
    ///     <see cref="CheckBoxColumnOptions{TModel}" />, etc.).
    ///   </para>
    /// </remarks>
    public interface ICellOptions
    {
        /// <summary>
        ///   Gets the gesture(s) that will cause the cell to enter edit mode.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This property determines which user interactions will trigger the cell to enter edit mode.
        ///   </para>
        ///   <para>
        ///     The default value is <see cref="BeginEditGestures.Default" />, which includes F2 key press
        ///     and double-tap gestures. Other options include single tap, or requiring the cell to be
        ///     selected first.
        ///   </para>
        ///   <para>
        ///     To prevent a cell from entering edit mode through user interaction, use
        ///     <see cref="BeginEditGestures.None" />. The cell can still be programmatically
        ///     put into edit mode.
        ///   </para>
        /// </remarks>
        BeginEditGestures BeginEditGestures { get; }
    }
}
