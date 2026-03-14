using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Models;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.TreeDataGridTests;
using Avalonia.Styling;
using Avalonia.Threading;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Controls.TreeDataGridBenchmark;

public class Benchmarks
{
    private const int RowCount = 500;
    private const int ColumnCount = 40;

    private TreeDataGrid? _target;

    [GlobalSetup]
    public void Setup()
    {
        TestApplication.BuildAvaloniaApp().SetupWithoutStarting();
        _target = CreateTarget();
    }

    [Benchmark]
    public void Scrolling_Down()
    {
        for (int i = 0; i < RowCount; i++)
        {
            _target.Scroll!.Offset = new Vector(0, 10 * i);
            Layout(_target);
        }

        for (int i = RowCount - 1; i >= 0; i--)
        {
            _target.Scroll!.Offset = new Vector(0, 10 * i);
            Layout(_target);
        }
    }

    private static TreeDataGrid CreateTarget()
    {
        var items = new AvaloniaList<Model>([.. CreateModels("Item 0-", RowCount)]);

        var source = new FlatTreeDataGridSource<Model>(items);
        source.Columns.Add(new TextColumn<Model, int>("ID", x => x.Id));

        for (int i = 0; i < ColumnCount; i++)
        {
            source.Columns.Add(new TextColumn<Model, string?>("Title", x => x.Title));
        }

        var target = new TreeDataGrid
        {
            Template = TestTemplates.TreeDataGridTemplate(),
            Source = source,
        };

        var root = new TestWindow(target)
        {
            Styles =
                {
                    TestTemplates.TreeDataGridExpanderCellStyle,
                    TestTemplates.TreeDataGridTemplateCellStyle,
                    new Style(x => x.Is<TreeDataGridRow>())
                    {
                        Setters =
                        {
                            new Setter(TreeDataGridRow.TemplateProperty, TestTemplates.TreeDataGridRowTemplate()),
                        }
                    },
                    new Style(x => x.Is<TreeDataGridCell>())
                    {
                        Setters =
                        {
                            new Setter(TreeDataGridCell.HeightProperty, 10.0),
                        }
                    }
                }
        };

        root.UpdateLayout();
        Dispatcher.UIThread.RunJobs();

        return target;
    }

    private static void Layout(TreeDataGrid target)
    {
        target.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    private static IEnumerable<Model> CreateModels(
        string titlePrefix,
        int count,
        int firstIndex = 0,
        int firstId = 100)
    {
        return Enumerable.Range(0, count).Select(x =>
            new Model
            {
                Id = firstId + firstIndex + x,
                Title = titlePrefix + (firstIndex + x),
            });
    }

    private class Model : NotifyingBase
    {
        public int Id { get; set; }
        public string? Title { get; set; }
    }
}
