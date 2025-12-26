namespace Avalonia.Controls.Models.TreeDataGrid
{
    /// <summary>
    ///   Defines a column in a <see cref="TreeDataGrid" /> that supports text-based searching.
    /// </summary>
    /// <typeparam name="TModel">The model type for the rows in the TreeDataGrid.</typeparam>
    /// <remarks>
    ///   <para>
    ///     The <see cref="ITextSearchableColumn{TModel}" /> interface is implemented by column types
    ///     that can participate in text search operations within a TreeDataGrid. This allows users
    ///     to find rows by typing text that matches content in these columns.
    ///   </para>
    ///   <para>
    ///     Column implementations like <see cref="TextColumn{TModel, TValue}" /> and
    ///     <see cref="TemplateColumn{TModel}" /> implement this interface to enable their content
    ///     to be searched.
    ///   </para>
    ///   <para>
    ///     For template columns, the <see cref="TemplateColumnOptions{TModel}.TextSearchValueSelector" />
    ///     property provides a way to extract searchable text from complex content.
    ///   </para>
    /// </remarks>
    public interface ITextSearchableColumn<TModel>
    {
        /// <summary>
        ///   Gets a value indicating whether text search is enabled for this column.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     When set to true, the column's content will be included in text search operations.
        ///     When false, the column will be skipped during text searches.
        ///   </para>
        ///   <para>
        ///     This property can be configured through column options, such as
        ///     <see cref="TextColumnOptions{TModel}.IsTextSearchEnabled" /> or
        ///     <see cref="TemplateColumnOptions{TModel}.IsTextSearchEnabled" />.
        ///   </para>
        /// </remarks>
        bool IsTextSearchEnabled { get; }
        string? SelectValue(TModel model);
    }
}
