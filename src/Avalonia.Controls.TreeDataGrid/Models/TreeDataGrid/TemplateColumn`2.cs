using System;
using Avalonia.Data;
using Avalonia.Experimental.Data;

namespace Avalonia.Controls.Models.TreeDataGrid
{
    /// <summary>
    /// A column in an <see cref="ITreeDataGridSource"/> which displays its values using a data
    /// template.
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <typeparam name="TValue">The column data type.</typeparam>
    public class TemplateColumn<TModel, TValue> : TemplateColumn<TModel>
        where TModel : class
        where TValue : class
    {
        private readonly Func<TModel, TValue> _getter;
        private TypedBinding<TValue, bool>? _isReadOnlyBinding;

        public TemplateColumn(
            object? header,
            Func<TModel, TValue> getter,
            object cellTemplateResourceKey,
            object? cellEditingTemplateResourceKey = null,
            GridLength? width = null, 
            TemplateColumnOptions<TValue>? options = null) 
            : base(header, cellTemplateResourceKey, cellEditingTemplateResourceKey, width, ConvertOptions(options, getter))
        {
            _getter = getter;

            if (options?.IsReadOnlyGetter is { } isReadOnlyGetter)
                _isReadOnlyBinding = TypedBinding<TValue>.OneWay(isReadOnlyGetter);
        }
        
        private static TemplateColumnOptions<TModel>? ConvertOptions(TemplateColumnOptions<TValue>? src, Func<TModel, TValue> valueSelector)
        {
            if (src == null)
                return null;

            if (valueSelector == null)
                throw new ArgumentNullException(nameof(valueSelector));

            return new TemplateColumnOptions<TModel>
            {
                BeginEditGestures = src.BeginEditGestures,
                CanUserResizeColumn = src.CanUserResizeColumn,
                CanUserSortColumn = src.CanUserSortColumn,
                CompareAscending = ConvertComparison(src.CompareAscending, valueSelector),
                CompareDescending = ConvertComparison(src.CompareDescending, valueSelector),
                IsTextSearchEnabled = src.IsTextSearchEnabled,
                IsVisible = src.IsVisible,
                MaxWidth = src.MaxWidth,
                MinWidth = src.MinWidth,
                TextSearchValueSelector = src.TextSearchValueSelector == null ? null : t => src.TextSearchValueSelector(valueSelector(t)),
            };

            static Comparison<TModel?>? ConvertComparison(Comparison<TValue?>? comparison, Func<TModel, TValue> valueSelector)
            {
                if (comparison == null)
                    return null;

                return (x, y) =>
                {
                    if (x is null && y is null)
                        return 0;

                    var cellX = x == null ? default : valueSelector(x);
                    var cellY = y == null ? default : valueSelector(y);

                    return comparison(cellX, cellY);
                };
            }
        }

        /// <summary>
        /// Creates a cell for this column on the specified row.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <returns>The cell.</returns>
        public override ICell CreateCell(IRow<TModel> row)
        {
            var value = _getter(row.Model);
            var isReadOnlyObservable = BuildIsReadOnlyObservable(row.Model, value, _getEditingCellTemplate is null);
            return new TemplateCell(value, _getCellTemplate, _getEditingCellTemplate, isReadOnlyObservable, Options);
        }

        private IObservable<BindingValue<bool>> BuildIsReadOnlyObservable(TModel model, TValue value, bool isCollumnReadOnly)
        {
            if (!isCollumnReadOnly && _isReadOnlyBinding is not null)
                return _isReadOnlyBinding.Instance(value);

            return BuildIsReadOnlyObservable(model, isCollumnReadOnly);
        }
    }
}
