# Editing and Read-Only State

## Overview

The TreeDataGrid provides flexible control over which cells can be edited. This can be controlled at the column level or at the individual row level.

## Column-Level Read-Only State

By default, whether a column is editable or read-only depends on how it was created:

- **TextColumn**: Editable if a setter expression is provided, read-only otherwise
  ```csharp
  // Read-only (no setter)
  new TextColumn<Person, string>("First Name", x => x.FirstName)
  
  // Editable (setter provided)
  new TextColumn<Person, string>("First Name", x => x.FirstName, (o, v) => o.FirstName = v)
  ```

- **CheckBoxColumn**: Editable if a setter expression is provided, read-only otherwise
  ```csharp
  // Read-only (no setter)
  new CheckBoxColumn<Person>("IsActive", x => x.IsActive)
  
  // Editable (setter provided)
  new CheckBoxColumn<Person>("IsActive", x => x.IsActive, (o, v) => o.IsActive = v)
  ```

- **TemplateColumn**: Editable if an `cellEditingTemplate` is provided, read-only otherwise
  ```csharp
  // Read-only (no editing template)
  new TemplateColumn<Person>(
      "Selected",
      "SelectedTemplate")

  // Editable (editing template provided)
  new TemplateColumn<Person>(
      "Selected",
      "SelectedTemplate",
      "SelectedEditingTemplate")
  ```

## Row-Level Read-Only State

The `IsReadOnlyGetter` option allows you to specify which rows should be read-only, even if the column is editable:

```csharp
var column = new TextColumn<Person, string>(
    "First Name",
    x => x.FirstName,
    (o, v) => o.FirstName = v,
    options: new TextColumnOptions<Person>
    {
        IsReadOnlyGetter = x => x.IsLocked  // Make cells read-only for locked rows
    });
```

The expression provided to `IsReadOnlyGetter` is evaluated for each row, allowing you to use any property from your model to determine the read-only state.

For detailed information about this and other column options, see [Column Options](column-options.md).

### Precedence Rules

The read-only state is determined as follows:

1. If the column itself is read-only (no setter/editing template), all cells are read-only
2. If the column is editable but `IsReadOnlyGetter` returns `true` for a row, that cell is read-only
3. Otherwise, the cell is editable

### Example: Conditional Editing

```csharp
var data = new ObservableCollection<Person>
{
    new Person { Name = "Alice", Status = "Active", IsAdmin = false },
    new Person { Name = "Bob", Status = "Locked", IsAdmin = true },
    new Person { Name = "Charlie", Status = "Active", IsAdmin = false },
};

var source = new FlatTreeDataGridSource<Person>(data)
{
    Columns =
    {
        new TextColumn<Person, string>(
            "Name",
            x => x.Name,
            (o, v) => o.Name = v,
            options: new TextColumnOptions<Person>
            {
                IsReadOnlyGetter = x => x.IsAdmin || x.Status == "Locked"
            }),
        new TextColumn<Person, string>(
            "Status",
            x => x.Status,
            (o, v) => o.Status = v,
            options: new TextColumnOptions<Person>
            {
                IsReadOnlyGetter = x => x.IsAdmin  // Admin rows can't change their own status
            }),
    }
};
```

In this example:
- The "Name" column is editable for regular users, but read-only for admins and locked users
- The "Status" column is editable for regular users, but read-only for admins

## Dynamic Read-Only State Changes

The read-only state is evaluated whenever the expression is requested, so if you modify the model properties that the `IsReadOnlyGetter` depends on, the cells will automatically update their editable state.

However, if a cell is currently being edited and becomes read-only, the editing will be automatically cancelled.
