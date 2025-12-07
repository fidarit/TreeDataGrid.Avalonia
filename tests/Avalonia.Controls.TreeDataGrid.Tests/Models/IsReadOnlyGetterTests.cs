using System.Linq;
using Avalonia.Controls.Models.TreeDataGrid;
using Xunit;

namespace Avalonia.Controls.TreeDataGridTests.Models
{
    public class IsReadOnlyGetterTests
    {
        [Fact]
        public void Getter_Respects_Per_Row_State()
        {
            var data = new[]
            {
                new TestModel { Name = "Editable",   Value = 1, IsReadOnly = false },
                new TestModel { Name = "Read-only",  Value = 2, IsReadOnly = true },
                new TestModel { Name = "Also RO",    Value = 3, IsReadOnly = true },
            };

            var column = new TextColumn<TestModel, int>(
                "Value",
                x => x.Value,
                (x, v) => x.Value = v,
                options: new TextColumnOptions<TestModel>
                {
                    IsReadOnlyGetter = m => m.IsReadOnly
                });

            var cells = CreateCells(data, column);

            Assert.Equal(data.Length, cells.Length);

            for (var i = 0; i < data.Length; i++)
            {
                Assert.Equal(data[i].IsReadOnly, !cells[i].CanEdit);
            }
        }

        [Fact]
        public void Column_IsReadOnly_Has_Precedence_Over_Getter()
        {
            var data = new[] { new TestModel { IsReadOnly = false } };

            var column = new TextColumn<TestModel, int>(
                "Value",
                x => x.Value,
                options: new TextColumnOptions<TestModel>
                {
                    IsReadOnlyGetter = m => m.IsReadOnly
                });

            var cells = CreateCells(data, column);

            Assert.Single(cells);

            var cell = cells.First();
            Assert.False(cell.CanEdit);
        }

        [Fact]
        public void Null_Getter_Falls_Back_To_Column_State()
        {
            var data = new[] { new TestModel() };

            var column = new TextColumn<TestModel, int>(
                "Value",
                x => x.Value,
                options: new TextColumnOptions<TestModel>
                {
                    IsReadOnlyGetter = null
                });

            var cells = CreateCells(data, column);

            Assert.Single(cells);

            var cell = cells.First();
            Assert.False(cell.CanEdit);
        }

        private static ICell[] CreateCells(TestModel[] data, IColumn<TestModel> column)
        {
            var result = new ICell[data.Length];
            for (int i = 0; i < result.Length; i++)
            {
                var row = new AnonymousRow<TestModel>();
                row.Update(i, data[i]);

                result[i] = column.CreateCell(row);
            }

            return result;
        }

        private class TestModel
        {
            public string? Name { get; set; }
            public bool IsReadOnly { get; set; }
            public int Value { get; set; }
        }
    }
}
