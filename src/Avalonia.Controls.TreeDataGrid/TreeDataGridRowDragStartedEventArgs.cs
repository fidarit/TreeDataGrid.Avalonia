using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    /// <summary>
    ///   Provides data for the <see cref="TreeDataGrid.RowDragStarted" /> event.
    /// </summary>
    /// <remarks>
    ///   This event is raised when a drag operation in a <see cref="TreeDataGrid" /> control begins.
    ///   It contains information about the models being dragged and allows for configuring the
    ///   permitted drag effects.
    /// </remarks>
    public class TreeDataGridRowDragStartedEventArgs : RoutedEventArgs
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="TreeDataGridRowDragStartedEventArgs" />
        ///   class.
        /// </summary>
        /// <param name="models">
        ///   A collection of model objects that represent the rows being dragged.
        /// </param>
        public TreeDataGridRowDragStartedEventArgs(IEnumerable<object> models)
            : base(TreeDataGrid.RowDragStartedEvent)
        {
            Models = models;
        }

        /// <summary>
        ///   Gets or sets the allowed effects for the drag operation.
        /// </summary>
        /// <remarks>
        ///   This property can be used to control what operations (move, copy, etc.) are permitted
        ///   during the drag operation. By default, when <see cref="TreeDataGrid.AutoDragDropRows" />
        ///   is enabled, this is set to <see cref="DragDropEffects.Move" />. Setting this to
        ///   <see cref="DragDropEffects.None" /> will cancel the drag operation.
        /// </remarks>
        public DragDropEffects AllowedEffects { get; set; }
        /// <summary>
        ///   Gets the collection of model objects that represent the rows being dragged.
        /// </summary>
        /// <remarks>
        ///   These are the underlying data models for the selected rows, not the visual row
        ///   elements.
        /// </remarks>
        public IEnumerable<object> Models { get; }
    }
}
