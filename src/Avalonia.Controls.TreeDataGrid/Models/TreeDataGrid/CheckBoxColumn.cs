using System;
using System.Linq.Expressions;
using Avalonia.Experimental.Data;

namespace Avalonia.Controls.Models.TreeDataGrid
{
    /// <summary>
    ///   A column in an <see cref="ITreeDataGridSource" /> which displays a check box.
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <remarks>
    ///   <para>
    ///     The <see cref="CheckBoxColumn{TModel}" /> class provides a column that displays boolean
    ///     values as checkboxes in a TreeDataGrid. It supports both two-state (checked/unchecked)
    ///     and three-state (checked/unchecked/indeterminate) checkbox behavior.
    ///   </para>
    ///   <para>
    ///     This column creates <see cref="CheckBoxCell" /> instances for each row, which are backed by
    ///     <see cref="Primitives.TreeDataGridCheckBoxCell" /> primitive controls that
    ///     handle the UI rendering and user interactions.
    ///   </para>
    ///   <para>
    ///     The column can be configured to be read-only by not providing a setter function, and
    ///     additional options can be specified through <see cref="CheckBoxColumnOptions{TModel}" />.
    ///   </para>
    /// </remarks>
    public class CheckBoxColumn<TModel> : ColumnBase<TModel, bool?>
        where TModel : class
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="CheckBoxColumn{TModel}" /> class.
        /// </summary>
        /// <param name="header">The column header.</param>
        /// <param name="getter">
        ///   An expression which given a row model, returns a boolean cell value for the column.
        /// </param>
        /// <param name="setter">
        ///   A method which given a row model and a cell value, writes the cell value to the
        ///   row model. If not supplied then the column will be read-only.
        /// </param>
        /// <param name="width">
        ///   The column width. If null defaults to <see cref="GridLength.Auto" />.
        /// </param>
        /// <param name="options">Additional column options.</param>
        /// <remarks>
        ///   This constructor creates a two-state checkbox column that can be checked or unchecked.
        ///   The checkbox will be read-only if no setter is provided.
        /// </remarks>
        public CheckBoxColumn(
            object? header,
            Expression<Func<TModel, bool>> getter,
            Action<TModel, bool>? setter = null,
            GridLength? width = null,
            CheckBoxColumnOptions<TModel>? options = null)
            : base(header, ToNullable(getter), ToNullable(getter, setter), width, options)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="CheckBoxColumn{TModel}" /> class that
        ///   displays a three-state check box.
        /// </summary>
        /// <param name="header">The column header.</param>
        /// <param name="getter">
        ///   An expression which given a row model, returns a nullable boolean cell value for the
        ///   column.
        /// </param>
        /// <param name="setter">
        ///   A method which given a row model and a cell value, writes the cell value to the
        ///   row model. If not supplied then the column will be read-only.
        /// </param>
        /// <param name="width">
        ///   The column width. If null defaults to <see cref="GridLength.Auto" />.
        /// </param>
        /// <param name="options">Additional column options.</param>
        /// <remarks>
        ///   <para>
        ///     This constructor creates a three-state checkbox column that can be checked, unchecked,
        ///     or indeterminate (null).
        ///   </para>
        ///   <para>
        ///     The checkbox will be read-only if no setter is provided.
        ///   </para>
        /// </remarks>
        public CheckBoxColumn(
            object? header,
            Expression<Func<TModel, bool?>> getter,
            Action<TModel, bool?>? setter = null,
            GridLength? width = null,
            CheckBoxColumnOptions<TModel>? options = null)
            : base(header, getter, setter, width, options ?? new())
        {
            IsThreeState = true;
        }

        /// <summary>
        ///   Gets a value indicating whether the column displays a three-state checkbox.
        /// </summary>
        /// <remarks>
        ///   When true, the checkboxes in this column can be in three states: checked (true),
        ///   unchecked (false), or indeterminate (null). When false, only checked and unchecked
        ///   states are supported.
        /// </remarks>
        public bool IsThreeState { get; }

        /// <summary>
        ///   Creates a cell for this column on the specified row.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <returns>
        ///   A <see cref="CheckBoxCell" /> instance for the row.
        /// </returns>
        /// <remarks>
        ///   This method creates a <see cref="CheckBoxCell" /> that is bound to the property
        ///   specified in  the column's getter expression. The cell will be read-only if no setter
        ///   was provided during column construction.
        /// </remarks>
        public override ICell CreateCell(IRow<TModel> row)
        {
            var expression = CreateBindingExpression(row.Model);
            var isReadOnlyObservable = BuildIsReadOnlyObservable(row.Model, Binding.Write is null);
            return new CheckBoxCell(expression, expression, isReadOnlyObservable, IsThreeState);
        }

        private static Func<TModel, bool?> ToNullable(Expression<Func<TModel, bool>> getter)
        {
            var c = getter.Compile();
            return x => c(x);
        }

        private static TypedBinding<TModel, bool?> ToNullable(
            Expression<Func<TModel, bool>> getter,
            Action<TModel, bool>? setter)
        {
            var g = Expression.Lambda<Func<TModel, bool?>>(
                Expression.Convert(getter.Body, typeof(bool?)),
                getter.Parameters);

            return setter is null ?
                TypedBinding<TModel>.OneWay(g) :
                TypedBinding<TModel>.TwoWay(g, (m, v) => setter(m, v ?? false));
        }
    }
}
