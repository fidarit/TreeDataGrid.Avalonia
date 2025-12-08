using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using Xunit;

namespace Avalonia.Controls.TreeDataGridTests.Models;

public class TemplateColumnTests
{
    private class Person
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    private static IRow<Person> Row(Person model, int index = 0)
    {
        var row = new AnonymousRow<Person>();
        row.Update(index, model);
        return row;
    }

    [Fact]
    public void CreateCell_Returns_TemplateCell_With_Correct_Value_From_DataTemplate()
    {
        var person = new Person();
        var row = Row(person);

        var column = new TemplateColumn<Person>(
            header: "Name",
            cellTemplate: new FuncDataTemplate<Person>((_, _) => new TextBlock()));

        var cell = column.CreateCell(row);
        Assert.IsType<TemplateCell>(cell);
        Assert.Equal(person, cell.Value);
    }

    [Fact]
    public void Cell_Is_Not_Editable_When_No_EditingTemplate()
    {
        var column = new TemplateColumn<Person>(
            "Age",
            new FuncDataTemplate<Person>((_, __) => new TextBlock()));

        var cell = column.CreateCell(Row(new Person()));
        var templateCell = Assert.IsType<TemplateCell>(cell);
        Assert.False(templateCell.CanEdit);
    }

    [Fact]
    public void Cell_Is_Editable_When_EditingTemplate_Is_Provided()
    {
        var column = new TemplateColumn<Person>(
            "Age",
            new FuncDataTemplate<Person>((_, __) => new TextBlock()),
            new FuncDataTemplate<Person>((_, __) => new TextBox()));

        var cell = column.CreateCell(Row(new Person()));
        var templateCell = Assert.IsType<TemplateCell>(cell);
        Assert.True(templateCell.CanEdit);
    }
}
