using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Input;

namespace Avalonia.Controls.Models.TreeDataGrid
{
    public static class TreeDataGridDragFormat
    {
        /// <summary>
        /// Defines the data format in an <see cref="Avalonia.Input.DataFormat"/>.
        /// </summary>
        public static DataFormat<string> Instance = DataFormat<string>.CreateStringApplicationFormat(nameof(TreeDataGridDragFormat));
    }
}
