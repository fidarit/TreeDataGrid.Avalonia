using System;
using System.Collections.Generic;
using Avalonia.Input;

namespace Avalonia.Controls.Models.TreeDataGrid
{
    /// <summary>
    /// Holds information about an automatic row drag/drop operation carried out
    /// by <see cref="Avalonia.Controls.TreeDataGrid.AutoDragDropRows"/>.
    /// </summary>
    public class DragInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DragInfo"/> class.
        /// </summary>
        /// <param name="indexes">The indexes being dragged.</param>
        public DragInfo(IEnumerable<IndexPath> indexes)
        {
            Indexes = indexes;
        }

        public DragInfo(string z)
        {
            Import(z);
        }

        /// <summary>
        /// Gets or sets the model indexes of the rows being dragged.
        /// </summary>
        public IEnumerable<IndexPath> Indexes { get; }

        internal string? Export()
        {
            return null;
        }

        internal void Import(string z)
        {
        }
    }
}
