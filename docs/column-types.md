# TreeDataGrid column types

TreeDataGrid currently supports three different column types: 
- [TextColumn](https://github.com/fidarit/TreeDataGrid.Avalonia/blob/master/src/Avalonia.Controls.TreeDataGrid/Models/TreeDataGrid/TextColumn.cs) 
- [HierarchicalExpanderColumn](https://github.com/fidarit/TreeDataGrid.Avalonia/blob/master/src/Avalonia.Controls.TreeDataGrid/Models/TreeDataGrid/HierarchicalExpanderColumn.cs)
- [TemplateColumn](https://github.com/fidarit/TreeDataGrid.Avalonia/blob/master/src/Avalonia.Controls.TreeDataGrid/Models/TreeDataGrid/TemplateColumn.cs)

## TextColumn
`TextColumn` is useful when you want all cells in the column to have only text values.
Usually, everything you need to instantiate `TextColumn` class is
```csharp
new TextColumn<Person, string>("First Name", x => x.FirstName)
```
The first generic parameter here is your model type basically, the place where you want to grab data from. Person in this case. The second generic parameter here is the type of the property where you want to grab data from. In this case, it is a string, it will be used to know exactly which type your property has.

![image](https://user-images.githubusercontent.com/53405089/157456551-dd394781-903a-4c7b-8874-e631e21534a1.png)

This is the signature of the `TextColumn` constructor. There are two most important parameters. The first one will be used to define the column header, usually, it would be a string. In the sample above it is *"First Name"*. The second parameter is an expression to get the value of the property. In the sample above it is *x => x.FirstName*.

**Note**:               
The sample above is taken from [this article](https://github.com/fidarit/TreeDataGrid.Avalonia/blob/master/docs/get-started-flat.md). If you feel like you need more examples feel free to check it, there is a sample that shows how to use TextColumns and how to run a whole `TreeDataGrid` using them. 

## CheckBoxColumn

As its name suggests, `CheckBoxColumn` displays a `CheckBox` in its cells. 
The column supports both two-state (`bool`) and three-state (`bool?`) checkboxes, depending on the property type.

For a read-only checkbox:

```csharp
new CheckBoxColumn<Person>("Firstborn", x => x.IsFirstborn)
```

The first parameter defines the column header. The second parameter is an expression which gets the value of the property from the model.

For a read/write checkbox:

```csharp
new CheckBoxColumn<Person>("Firstborn", x => x.IsFirstborn, (o, v) => o.IsFirstborn = v)
```

The third parameter is an expression used to set the property value in the model.

## HierarchicalExpanderColumn
`HierarchicalExpanderColumn` can be used only with `HierarchicalTreeDataGrid` (a.k.a TreeView) thats what Hierarchical stands for in its name, also it can be used only with `HierarchicalTreeDataGridSource`. This type of columns can be useful when you want cells to show an expander to reveal nested data.

That's how you instantiate `HierarchicalExpanderColumn` class:
```csharp
new HierarchicalExpanderColumn<Person>(new TextColumn<Person, string>("First Name", x => x.FirstName), x => x.Children)
```
`HierarchicalExpanderColumn` has only one generic parameter, it is your model type, same as in `TextColumn`, Person in this case.

Lets take a look at the `HierarchicalExpanderColumn` constructor.
![image](https://user-images.githubusercontent.com/53405089/157536079-fd14f1ed-0a7d-438a-abba-fd56766709a9.png)

The first parameter in the constructor is a nested column, you would usually want to display something besides the expander and that's why you need this parameter. In the sample above, we want to display text and we use `TextColumn` for that. The second parameter is a selector of the child elements, stuff that will be displayed when `Expander` is in the expanded state below the parent element.

**Note**:               
The sample above is taken from [this article](https://github.com/fidarit/TreeDataGrid.Avalonia/blob/master/docs/get-started-hierarchical.md). If you feel like you need more examples feel free to check it, there is a sample that shows how to use `HierarchicalExpanderColumn` and how to run a whole `TreeDataGrid` using it. 

## TemplateColumn\<TModel\>

TemplateColumn is the most customizable way to create a column. Because cell contents are described by a data template, the options for how each cell is displayed is almost unlimited.

The `TemplateColumn` constructor takes the following parameters:

- `header`: The Column header
- `cellTemplate`: A data template which describes how cells in the column will be displayed
- `cellEditingTemplate`: A data template which describes how cells in the column will be displayed when in edit mode. If no `cellEditingTemplate` is provided, then edit mode will be disabled for the column.
- `width`: The width of the column
- `options`: Less frequently used options for the column

There are two overloads of the `TemplateColumn` constructor, which corresponds to the two ways in which a template can be specified:

1. Using a `FuncDataTemplate` to create a template in code:

```csharp
new TemplateColumn<Person>(
    "Selected",
    new FuncDataTemplate<Person>((_, _) => new CheckBox
    {
        [!CheckBox.IsCheckedProperty] = new Binding("IsSelected"),
    }))
```

2. Using a template defined as a XAML resource:

```xml
<TreeDataGrid Name="fileViewer" Source="{Binding Files.Source}">
    <TreeDataGrid.Resources>
           
        <!-- Defines a named template for the column -->
        <DataTemplate x:Key="CheckBoxCell">
            <CheckBox IsChecked="{Binding IsSelected}"/>
        </DataTemplate>
        
    </DataTemplate>
              
    </TreeDataGrid.Resources>
</TreeDataGrid>
```

```csharp
// CheckBoxCell is the key of the template defined in XAML.
new TemplateColumn<Person>("Selected", "CheckBoxCell");
```

`TemplateColumn` has only one generic parameter, it is your model type, the same as in `TextColumn`; `Person` in this case. The code above will create a column with header *"Selected"* and a `CheckBox` in each cell.


## TemplateColumn<TModel, TValue>

`TemplateColumn<TModel, TValue>` is an advanced version of `TemplateColumn<TModel>` that allows each cell to use a specific typed value (`TValue`) instead of the whole model. The main benefit of this class is **fine-grained control over individual cells**, allowing each cell to represent a sub-object or computed value independently from the row model.

With this column type, each row can have multiple cells.

---

### Usage

Suppose you have a `DataRow` model containing multiple cells:

```csharp
public class DataRow
{
    public DataCell[] Cells { get; set; }
}

public class DataCell
{
    public string Value { get; set; }
    public bool IsLocked { get; set; }
}
```

You can define a typed template column for individual cells like this:

```csharp
new TemplateColumn<DataRow, DataCell>(
    "Cell 0",
    row => row.Cells[0],
    "CellTemplate",
    "CellEditTemplate",
    options: new TemplateColumnOptions<DataCell>
    {
        IsReadOnlyGetter = cell => cell.IsLocked
    });
```

Here, each cell in the column uses a `DataCell` object as its data context, allowing templates to bind to `Value` and respect the `IsLocked` flag.

---

### Constructor

```csharp
TemplateColumn(
    object? header,
    Func<TModel, TValue> getter,
    object cellTemplateResourceKey,
    object? cellEditingTemplateResourceKey = null,
    GridLength? width = null,
    TemplateColumnOptions<TValue>? options = null)
```

### Parameters

- `header`: The Column header
- `getter`:Function to extract a typed value `TValue` from the row model.
- `cellTemplate`: A data template which describes how cells in the column will be displayed
- `cellEditingTemplate`: A data template which describes how cells in the column will be displayed when in edit mode. If no `cellEditingTemplate` is provided, then edit mode will be disabled for the column.
- `width` *(optional)*: The width of the column
- `options` *(optional)*: Less frequently used options for the column
