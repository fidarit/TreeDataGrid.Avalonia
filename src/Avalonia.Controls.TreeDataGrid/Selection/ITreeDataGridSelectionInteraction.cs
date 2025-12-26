using System;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Input;

namespace Avalonia.Controls.Selection
{
    /// <summary>
    ///   Defines the interaction between a <see cref="TreeDataGrid" /> and an
    ///   <see cref="ITreeDataGridSelection" /> model.
    /// </summary>
    /// <remarks>
    ///   This interface is implemented by selection models to handle user interactions with the
    ///   TreeDataGrid, such as keyboard navigation, pointer events, and selection state changes. It
    ///   provides the connection between user input and the actual selection behavior.
    /// </remarks>
    public interface ITreeDataGridSelectionInteraction
    {
        /// <summary>
        ///   Occurs when the selection in the TreeDataGrid changes.
        /// </summary>
        /// <remarks>
        ///   This event is raised by the selection model when the selection changes, allowing the
        ///   TreeDataGrid to update its visual representation accordingly.
        /// </remarks>
        public event EventHandler? SelectionChanged;

        /// <summary>
        ///   Determines whether a specific cell is selected.
        /// </summary>
        /// <param name="columnIndex">The index of the column.</param>
        /// <param name="rowIndex">The index of the row.</param>
        /// <returns>true if the cell is selected; otherwise, false.</returns>
        /// <remarks>
        ///   This method is used by the TreeDataGrid to determine whether to display a cell with
        ///   the selected visual state.
        /// </remarks>
        bool IsCellSelected(int columnIndex, int rowIndex);
        /// <summary>
        ///   Determines whether a specific row model is selected.
        /// </summary>
        /// <param name="rowModel">The row model to check.</param>
        /// <returns>true if the row is selected; otherwise, false.</returns>
        /// <remarks>
        ///   This method is used by the TreeDataGrid to determine whether to display a row with
        ///   the selected visual state.
        /// </remarks>
        bool IsRowSelected(IRow rowModel);
        /// <summary>
        ///   Determines whether a specific row is selected.
        /// </summary>
        /// <param name="rowIndex">The index of the row to check.</param>
        /// <returns>true if the row is selected; otherwise, false.</returns>
        /// <remarks>
        ///   This method is used by the TreeDataGrid to determine whether to display a row with
        ///   the selected visual state.
        /// </remarks>
        bool IsRowSelected(int rowIndex);
        /// <summary>
        ///   Handles the KeyDown event from the TreeDataGrid.
        /// </summary>
        /// <param name="sender">The TreeDataGrid that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        /// <remarks>
        ///   This method handles key navigation and selection, such as using arrow keys to move the
        ///   selection or using Space/Enter to select items.
        /// </remarks>
        void OnKeyDown(TreeDataGrid sender, KeyEventArgs e);
        /// <summary>
        ///   Handles the PreviewKeyDown event from the TreeDataGrid.
        /// </summary>
        /// <param name="sender">The TreeDataGrid that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        /// <remarks>
        ///   This method is called in the tunneling phase of the event, allowing the  selection
        ///   model to handle key presses before they reach other controls.
        /// </remarks>
        void OnPreviewKeyDown(TreeDataGrid sender, KeyEventArgs e);
        /// <summary>
        ///   Handles the KeyUp event from the TreeDataGrid.
        /// </summary>
        /// <param name="sender">The TreeDataGrid that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        /// <remarks>
        ///   This method allows the selection model to respond to key releases.
        /// </remarks>
        void OnKeyUp(TreeDataGrid sender, KeyEventArgs e);
        /// <summary>
        ///   Handles the TextInput event from the TreeDataGrid.
        /// </summary>
        /// <param name="sender">The TreeDataGrid that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        /// <remarks>
        ///   This method allows the selection model to respond to text input, which can be used for
        ///   features like type-to-select.
        /// </remarks>
        void OnTextInput(TreeDataGrid sender, TextInputEventArgs e);
        /// <summary>
        ///   Handles the PointerPressed event from the TreeDataGrid.
        /// </summary>
        /// <param name="sender">The TreeDataGrid that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        /// <remarks>
        ///   This method handles selection when the user presses a pointer (mouse, touch, etc.)
        ///   on a row or cell. It typically manages behaviors like click-to-select and tracks
        ///   the starting point for range selections with Shift key.
        /// </remarks>
        void OnPointerPressed(TreeDataGrid sender, PointerPressedEventArgs e);
        /// <summary>
        ///   Handles the PointerMoved event from the TreeDataGrid.
        /// </summary>
        /// <param name="sender">The TreeDataGrid that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        /// <remarks>
        ///   This method can be used to handle behaviors like drag selection.
        /// </remarks>
        void OnPointerMoved(TreeDataGrid sender, PointerEventArgs e);
        /// <summary>
        ///   Handles the PointerReleased event from the TreeDataGrid.
        /// </summary>
        /// <param name="sender">The TreeDataGrid that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        /// <remarks>
        ///   This method handles completing a selection when the user releases a pointer (mouse
        ///   button, touch, etc.). It's particularly important for selection gestures that start
        ///   with PointerPressed and complete with PointerReleased.
        /// </remarks>
        void OnPointerReleased(TreeDataGrid sender, PointerReleasedEventArgs e);
    }
}
