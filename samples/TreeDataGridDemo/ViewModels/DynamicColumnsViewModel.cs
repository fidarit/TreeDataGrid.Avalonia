using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Models;
using Avalonia.Controls.Models.TreeDataGrid;
using TreeDataGridDemo.Models;

namespace TreeDataGridDemo.ViewModels
{
    internal class DynamicColumnsViewModel : NotifyingBase
    {
        public DynamicColumnsViewModel()
        {
            var _data = DataRow.CreateRandomItems();

            Source = new HierarchicalTreeDataGridSource<DataRow>(_data);
            Source.RowSelection!.SingleSelect = true;

            ColumnCount = 20;
        }

        public int ColumnCount
        {
            get => field;
            set
            {
                if (value > 0)
                    OnColumnCountChanged(field = value);
            }
        }

        public HierarchicalTreeDataGridSource<DataRow> Source { get; }

        private void OnColumnCountChanged(int newValue)
        {
            DataRow.SetColumnCount(Source.Items, newValue - 1);

            var columns = Source.Columns;
            if (columns.Count == 0)
                columns.Add(new HierarchicalExpanderColumn<DataRow>(new TextColumn<DataRow, string>("Name", row => row.Name), row => row.Children));

            for (int i = columns.Count - 1; i > newValue; i--)
                columns.RemoveAt(i);

            for (var i = columns.Count; i < newValue; i++)
            {
                var cellIndex = i - 1;
                columns.Add(new TemplateColumn<DataRow, DataCell>(DataRow.NewColumnName(), row => row.Cells[cellIndex], "DynamicCell", "DynamicEditingCell", 
                    new GridLength(1, GridUnitType.Auto), new()
                    {
                        TextSearchValueSelector = value => value.Value,
                        IsReadOnlyGetter = value => value.ReadOnly,
                        CompareAscending = (a, b) => Comparer<string>.Default.Compare(a.Value, b.Value),
                        CompareDescending = (b, a) => Comparer<string>.Default.Compare(a.Value, b.Value),
                    }));
            }
        }
    }
}
