# Column Options

All column types support a set of common options through the `ColumnOptions<TModel>` class. These options allow you to customize the behavior and appearance of columns.

## Passing Options to Columns

Options are typically passed via the `options` parameter in the column constructor:

```csharp
new TextColumn<Person, string>(
    "First Name",
    x => x.FirstName,
    (o, v) => o.FirstName = v,
    options: new TextColumnOptions<Person>
    {
        CanUserResizeColumn = true,
        CanUserSortColumn = true,
        MinWidth = new GridLength(100, GridUnitType.Pixel)
    })
```

## Available Options

### IsVisible

Controls whether the column is visible in the TreeDataGrid.

- **Type**: `bool`
- **Default**: `true`

```csharp
new TextColumn<Person, string>(
    "Email",
    x => x.Email,
    options: new TextColumnOptions<Person>
    {
        IsVisible = false  // Hide the column
    })
```

You can also toggle visibility at runtime by accessing the column's `IsVisible` property:

```csharp
var column = source.Columns[0];
column.IsVisible = false;  // Hide at runtime
```

### CanUserResizeColumn

Controls whether the user can resize the column by dragging its border.

- **Type**: `bool?` (nullable)
- **Default**: `null` (inherits from TreeDataGrid's `CanUserResizeColumns` property)

```csharp
new TextColumn<Person, string>(
    "Name",
    x => x.Name,
    options: new TextColumnOptions<Person>
    {
        CanUserResizeColumn = false  // Prevent user from resizing this column
    })
```

If set to `null`, the column will inherit the resize behavior from the parent TreeDataGrid's `CanUserResizeColumns` setting.

### CanUserSortColumn

Controls whether the user can sort the TreeDataGrid by clicking the column header.

- **Type**: `bool?` (nullable)
- **Default**: `null` (inherits from TreeDataGrid's `CanUserSortColumns` property)

```csharp
new TextColumn<Person, string>(
    "SequenceNumber",
    x => x.SequenceNumber,
    options: new TextColumnOptions<Person>
    {
        CanUserSortColumn = false  // Prevent sorting by this column
    })
```

If set to `null`, the column will inherit the sort behavior from the parent TreeDataGrid's `CanUserSortColumns` setting.

### MinWidth

Sets the minimum width for the column in pixels (or other units).

- **Type**: `GridLength`
- **Default**: `new GridLength(30, GridUnitType.Pixel)`

```csharp
new TextColumn<Person, string>(
    "Name",
    x => x.Name,
    options: new TextColumnOptions<Person>
    {
        MinWidth = new GridLength(150, GridUnitType.Pixel)  // Minimum 150 pixels
    })
```

### MaxWidth

Sets the maximum width for the column. When set to `null`, there is no maximum width limit.

- **Type**: `GridLength?` (nullable)
- **Default**: `null` (no limit)

```csharp
new TextColumn<Person, string>(
    "Description",
    x => x.Description,
    options: new TextColumnOptions<Person>
    {
        MaxWidth = new GridLength(500, GridUnitType.Pixel)  // Maximum 500 pixels
    })
```

### BeginEditGestures

Specifies which user interactions will cause a cell to enter edit mode.

- **Type**: `BeginEditGestures` (flag enum)
- **Default**: `BeginEditGestures.Default` (F2 or Double-tap)

Available gestures:
- `None` — Edit mode only via code (no user gestures)
- `F2` — Press F2 key to edit
- `Tap` — Single tap/click to edit
- `DoubleTap` — Double-tap/click to edit
- `Default` — F2 or Double-tap (combines F2 | DoubleTap)
- `WhenSelected` — Only respond to gestures when the cell or row is selected

You can combine multiple gestures using the bitwise OR operator:

```csharp
new TextColumn<Person, string>(
    "Name",
    x => x.Name,
    (o, v) => o.Name = v,
    options: new TextColumnOptions<Person>
    {
        BeginEditGestures = BeginEditGestures.Tap | BeginEditGestures.F2
    })
```

### CompareAscending and CompareDescending

Custom comparers for sorting the column in ascending and descending order.

- **Type**: `Comparison<TModel?>?` (nullable)
- **Default**: `null` (uses default comparison)

```csharp
new TextColumn<Person, string>(
    "Name",
    x => x.Name,
    options: new TextColumnOptions<Person>
    {
        CompareAscending = (a, b) => 
        {
            // Case-insensitive comparison
            return StringComparer.OrdinalIgnoreCase.Compare(a?.Name, b?.Name);
        },
        CompareDescending = (a, b) => 
        {
            // Case-insensitive comparison (reversed)
            return StringComparer.OrdinalIgnoreCase.Compare(b?.Name, a?.Name);
        }
    })
```

This is useful when you want to customize how the column sorts data, for example:
- Case-insensitive string sorting
- Custom numeric comparisons
- Sorting based on derived values rather than direct properties

### IsReadOnlyGetter

An expression that determines whether individual cells in the column are read-only based on row data.

- **Type**: `Expression<Func<TModel, bool>>?` (nullable)
- **Default**: `null` (all cells inherit column's read-only state)

See [Editing and Read-Only State](editing.md) for detailed information about this option.

```csharp
new TextColumn<Person, string>(
    "Notes",
    x => x.Notes,
    (o, v) => o.Notes = v,
    options: new TextColumnOptions<Person>
    {
        IsReadOnlyGetter = x => x.IsArchived  // Read-only for archived records
    })
```

## Complete Example

Here's a comprehensive example using multiple column options:

```csharp
var source = new FlatTreeDataGridSource<Person>(people)
{
    Columns =
    {
        // ID column: not resizable, not sortable, narrow
        new TextColumn<Person, int>(
            "ID",
            x => x.Id,
            options: new TextColumnOptions<Person>
            {
                CanUserResizeColumn = false,
                CanUserSortColumn = false,
                MinWidth = new GridLength(50, GridUnitType.Pixel),
                MaxWidth = new GridLength(50, GridUnitType.Pixel)
            }),
        
        // Name column: editable with custom sorting
        new TextColumn<Person, string>(
            "Name",
            x => x.Name,
            (o, v) => o.Name = v,
            options: new TextColumnOptions<Person>
            {
                BeginEditGestures = BeginEditGestures.Tap | BeginEditGestures.F2,
                CompareAscending = (a, b) => 
                    StringComparer.OrdinalIgnoreCase.Compare(a?.Name, b?.Name),
                IsReadOnlyGetter = x => x.IsLocked
            }),
        
        // Status column: can hide/show
        new TextColumn<Person, string>(
            "Status",
            x => x.Status,
            options: new TextColumnOptions<Person>
            {
                IsVisible = true,
                CanUserSortColumn = true
            })
    }
};
```

## Notes

- Most options can be changed at runtime through the column's properties or by updating the options object
- The `CanUserResizeColumn` and `CanUserSortColumn` options use nullable booleans to allow inheritance from the parent TreeDataGrid
- Custom comparers are only used when the user clicks the column header to sort; they don't affect the initial data order
