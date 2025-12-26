using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Controls.Templates;

namespace Avalonia.Controls.Models.TreeDataGrid
{
    /// <summary>
    ///   A column in an <see cref="ITreeDataGridSource" /> which displays its values using a data
    ///   template.
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    public class TemplateColumn<TModel> : ColumnBase<TModel>, ITextSearchableColumn<TModel>
        where TModel : class
    {
        protected readonly Func<Control, IDataTemplate> _getCellTemplate;
        protected readonly Func<Control, IDataTemplate>? _getEditingCellTemplate;
        private IDataTemplate? _cellTemplate;
        private IDataTemplate? _cellEditingTemplate;
        private object? _cellTemplateResourceKey;
        private object? _cellEditingTemplateResourceKey;

        /// <summary>
        ///   Initializes a new instance of the <see cref="TemplateColumn{TModel}" /> class.
        /// </summary>
        /// <param name="header">The header content for the column.</param>
        /// <param name="cellTemplate">
        ///   The data template used to render the content of each cell in the column.
        /// </param>
        /// <param name="cellEditingTemplate">
        ///   The data template used to render the content of each cell in editing mode. If null, the
        ///   <paramref name="cellTemplate" /> will be used for both display and editing.
        /// </param>
        /// <param name="width">
        ///   The column width. If null defaults to <see cref="GridLength.Auto" />.
        /// </param>
        /// <param name="options">
        ///   Additional configuration options for the column. If <see langword="null" />, default
        ///   options will be applied.
        /// </param>
        public TemplateColumn(
            object? header,
            IDataTemplate cellTemplate,
            IDataTemplate? cellEditingTemplate = null,
            GridLength? width = null,
            TemplateColumnOptions<TModel>? options = null)
            : base(header, width, options ?? new())
        {
            _getCellTemplate = GetCellTemplate;
            _cellTemplate = cellTemplate;
            _cellEditingTemplate = cellEditingTemplate;
            _getEditingCellTemplate = cellEditingTemplate is not null ?
                GetCellEditingTemplate : null;
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="TemplateColumn{TModel}" /> class using resource keys.
        /// </summary>
        /// <param name="header">The column header.</param>
        /// <param name="cellTemplateResourceKey">The resource key for the cell template.</param>
        /// <param name="cellEditingTemplateResourceKey">The resource key for the cell editing template, or null for read-only.</param>
        /// <param name="width">The column width, or null for auto width.</param>
        /// <param name="options">Additional column options.</param>
        public TemplateColumn(
            object? header,
            object cellTemplateResourceKey,
            object? cellEditingTemplateResourceKey = null,
            GridLength? width = null,
            TemplateColumnOptions<TModel>? options = null)
            : base(header, width, options ?? new())
        {
            _cellTemplateResourceKey = cellTemplateResourceKey ??
                throw new ArgumentNullException(nameof(cellTemplateResourceKey));
            _cellEditingTemplateResourceKey = cellEditingTemplateResourceKey;
            _getCellTemplate = GetCellTemplate;
            _getEditingCellTemplate = cellEditingTemplateResourceKey is not null ?
                GetCellEditingTemplate : null;
        }

        /// <summary>
        ///   Gets the column options.
        /// </summary>
        public new TemplateColumnOptions<TModel> Options => (TemplateColumnOptions<TModel>)base.Options;

        bool ITextSearchableColumn<TModel>.IsTextSearchEnabled => Options.IsTextSearchEnabled;

        /// <summary>
        ///   Gets the template to use to display the contents of a cell that is not in editing mode.
        /// </summary>
        public IDataTemplate GetCellTemplate(Control anchor)
        {
            if (_cellTemplate is not null)
                return _cellTemplate;
            
            _cellTemplate = anchor.FindResource(_cellTemplateResourceKey!) as IDataTemplate;

            if (_cellTemplate is null)
                throw new KeyNotFoundException(
                    $"No data template resource with the key of '{_cellTemplateResourceKey}' " +
                    $"could be found for the template column '{Header}'.");

            return _cellTemplate;
        }

        /// <summary>
        ///   Gets the template to use to display the contents of a cell that is in editing mode.
        /// </summary>
        public IDataTemplate GetCellEditingTemplate(Control anchor)
        {
            if (_cellEditingTemplate is not null)
                return _cellEditingTemplate;

            _cellEditingTemplate = anchor.FindResource(_cellEditingTemplateResourceKey!) as IDataTemplate;

            if (_cellEditingTemplate is null)
                throw new KeyNotFoundException(
                    $"No data template resource with the key of '{_cellEditingTemplateResourceKey}' " +
                    "could be found for the template column '{Header}'.");

            return _cellEditingTemplate;
        }

        /// <summary>
        ///   Creates a cell for this column on the specified row.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <returns>The cell.</returns>
        public override ICell CreateCell(IRow<TModel> row)
        {
            var isReadOnlyObservable = BuildIsReadOnlyObservable(row.Model, _getEditingCellTemplate is null);
            return new TemplateCell(row.Model, _getCellTemplate, _getEditingCellTemplate, isReadOnlyObservable, Options);
        }

        /// <summary>
        ///   Gets a comparison function for the specified sort direction.
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public override Comparison<TModel?>? GetComparison(ListSortDirection direction)
        {
            return direction switch
            {
                ListSortDirection.Ascending => Options.CompareAscending,
                ListSortDirection.Descending => Options.CompareDescending,
                _ => null,
            };
        }

        string? ITextSearchableColumn<TModel>.SelectValue(TModel model) => Options.TextSearchValueSelector?.Invoke(model);
    }
}
