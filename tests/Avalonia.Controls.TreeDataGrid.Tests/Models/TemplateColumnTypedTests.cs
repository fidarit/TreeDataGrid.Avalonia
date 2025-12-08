using Avalonia.Controls.Models.TreeDataGrid;
using Xunit;

namespace Avalonia.Controls.TreeDataGridTests.Models;

public class TemplateColumnTypedTests
{
    [Fact]
    public void CreateCell_Returns_TemplateCell_With_Correct_Value_From_DataTemplate()
    {
        var (row, model) = GetRow();

        var column = new TemplateColumn<Row, Cell>(
            header: "Name",
            getter: row => row.Cells[0],
            cellTemplateResourceKey: "Cell");

        var cell = column.CreateCell(row);
        Assert.IsType<TemplateCell>(cell);
        Assert.Equal(model.Cells[0], cell.Value);
    }

    [Fact]
    public void Cell_Is_Not_Editable_When_No_EditingTemplate()
    {
        var (row, model) = GetRow();

        var column = new TemplateColumn<Row, Cell>(
            header: "Name",
            getter: row => row.Cells[0],
            cellTemplateResourceKey: "Cell");

        var cell = column.CreateCell(row);
        var templateCell = Assert.IsType<TemplateCell>(cell);
        Assert.False(templateCell.CanEdit);
    }

    [Fact]
    public void Cell_Is_Editable_When_EditingTemplate_Is_Provided()
    {
        var (row, model) = GetRow();

        var column = new TemplateColumn<Row, Cell>(
            header: "Age",
            getter: row => row.Cells[1],
            cellTemplateResourceKey: "Cell",
            cellEditingTemplateResourceKey: "CellEditing");

        var cell = column.CreateCell(row);
        var templateCell = Assert.IsType<TemplateCell>(cell);
        Assert.True(templateCell.CanEdit);
    }

    private record Row(Cell[] Cells);
    private record Cell(string? Value, bool ReadOnly);

    private static (IRow<Row>, Row) GetRow()
    {
        var model = new Row([new("111", false), new("222", true)]);
        var row = new AnonymousRow<Row>();
        row.Update(0, model);
        return (row, model);
    }

}
