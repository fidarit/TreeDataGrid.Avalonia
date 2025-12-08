using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Bogus.DataSets;

namespace TreeDataGridDemo.Models;

internal class DataRow
{
    private static Database _database = new() { Random = new(0) };
    private ObservableCollection<DataRow>? _children;

    public string Name { get; } = _database.Random.Word();
    public List<DataCell> Cells { get; } = [];
    public ObservableCollection<DataRow> Children
    {
        get
        {
            if (_children == null)
                SetColumnCount(_children = CreateRandomItems(), Cells.Count);

            return _children;
        }
    }

    public static ObservableCollection<DataRow> CreateRandomItems()
    {
        return new ObservableCollection<DataRow>(Enumerable.Range(0, _database.Random.Int(0, 15))
            .Select(x => new DataRow()));
    }

    public static string NewColumnName() => _database.Column();

    public static void SetColumnCount(IEnumerable<DataRow> rows, int count)
    {
        foreach (var row in rows)
            row.SetColumnCount(count);
    }

    public void SetColumnCount(int count)
    {
        for (int i = Cells.Count - 1; i > count; i--)
            Cells.RemoveAt(i);

        for (int i = Cells.Count; i < count; i++)
            Cells.Add(new()
            {
                Value = _database.Random.Word(),
                ReadOnly = _database.Random.Bool(0.15f)
            });

        if (_children != null)
            SetColumnCount(_children, count);
    }
}
