using System;
using System.ComponentModel;
using Avalonia.Experimental.Data;
using Avalonia.Experimental.Data.Core;
using Avalonia.Reactive;

namespace Avalonia.Controls.Models.TreeDataGrid
{
    /// <summary>
    ///   Represents a cell in a hierarchical <see cref="TreeDataGrid" /> that can expand or collapse
    ///   to reveal nested data.
    /// </summary>
    /// <typeparam name="TModel">The type of the model object representing each row.</typeparam>
    /// <remarks>
    ///   <para>
    ///     The <see cref="ExpanderCell{TModel}" /> class wraps another cell (typically a
    ///     <see cref="TextCell{T}" />) and adds expansion functionality to display hierarchical data
    ///     in a tree structure. It manages the expanded/collapsed state of a row and the visibility of
    ///     the expander button.
    ///   </para>
    ///   <para>
    ///     This class serves as the data model that backs the
    ///     <see cref="Primitives.TreeDataGridExpanderCell" /> primitive control. The
    ///     primitive control handles UI rendering and user interactions, while this model manages the
    ///     underlying expansion state and coordinates with its parent row.
    ///   </para>
    /// </remarks>
    public class ExpanderCell<TModel> : NotifyingBase,
        IExpanderCell,
        IDisposable
        where TModel : class
    {
        private readonly ICell _inner;
        private readonly IDisposable _subscription;

        /// <summary>
        ///   Initializes a new instance of the <see cref="ExpanderCell{TModel}" /> class.
        /// </summary>
        /// <param name="inner">The inner cell wrapped by this expander cell.</param>
        /// <param name="row">The row that this cell belongs to.</param>
        /// <param name="showExpander">
        ///   An observable that signals whether the expander button should be shown.
        /// </param>
        /// <param name="isExpanded">
        ///   An optional binding expression for two-way binding of the expanded state.
        /// </param>
        /// <remarks>
        ///   <para>
        ///     The <paramref name="inner" /> cell provides the actual content display (like text),
        ///     while this expander cell adds the expansion functionality.
        ///   </para>
        ///   <para>
        ///     The <paramref name="showExpander" /> parameter is used to determine whether the row has
        ///     children and therefore whether the expander button should be visible.
        ///   </para>
        ///   <para>
        ///     When <paramref name="isExpanded" /> is provided, the expansion state is synchronized
        ///     with a property on the model object, allowing for programmatic control of the expanded
        ///     state.
        ///   </para>
        /// </remarks>
        public ExpanderCell(
            ICell inner,
            IExpanderRow<TModel> row,
            IObservable<bool> showExpander,
            TypedBindingExpression<TModel, bool>? isExpanded)
        {
            _inner = inner;
            Row = row;
            row.PropertyChanged += RowPropertyChanged;

            var expanderSubscription = showExpander.Subscribe(x => Row.UpdateShowExpander(this, x));
            if (isExpanded is not null)
            {
                var isExpandedSubscription = isExpanded.Subscribe(x =>
                {
                    if (x.HasValue)
                        IsExpanded = x.Value;
                });
                _subscription = new CompositeDisposable(expanderSubscription, isExpandedSubscription);
            }
            else
            {
                _subscription = expanderSubscription;
            }
        }

        /// <summary>
        ///   Gets a value indicating whether the cell can enter edit mode.
        /// </summary>
        /// <remarks>
        ///   This property delegates to the inner cell's <see cref="ICell.CanEdit" /> property.
        /// </remarks>
        public bool CanEdit => _inner.CanEdit;
        /// <summary>
        ///   Gets the inner cell that provides the content for this expander cell.
        /// </summary>
        /// <remarks>
        ///   The inner cell is wrapped by this expander cell and provides the actual content
        ///   display (such as text or a checkbox).
        /// </remarks>
        public ICell Content => _inner;
        /// <summary>
        ///   Gets the gestures that cause the inner cell to enter edit mode.
        /// </summary>
        /// <remarks>
        ///   This property delegates to the inner cell's <see cref="ICell.EditGestures" /> property.
        /// </remarks>
        public BeginEditGestures EditGestures => _inner.EditGestures;
        /// <summary>
        ///   Gets the row that this cell belongs to.
        /// </summary>
        /// <remarks>
        ///   The row provides context about the position of this cell in the hierarchical structure
        ///   and manages the expansion state at the row level.
        /// </remarks>
        public IExpanderRow<TModel> Row { get; }
        /// <summary>
        ///   Gets a value indicating whether the expander button should be shown.
        /// </summary>
        /// <remarks>
        ///   When true, the cell displays an expander button that can be clicked to expand or collapse the row.
        ///   When false, no expander is shown, indicating that the row has no children to display.
        ///   This property delegates to the row's <see cref="IExpander.ShowExpander" /> property.
        /// </remarks>
        public bool ShowExpander => Row.ShowExpander;
        /// <summary>
        ///   Gets the value of the cell.
        /// </summary>
        /// <remarks>
        ///   This property delegates to the inner cell's <see cref="ICell.Value" /> property.
        /// </remarks>
        public object? Value => _inner.Value;

        /// <summary>
        ///   Gets or sets a value indicating whether the row is expanded to show child rows.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     When set, this property updates the expansion state of both the cell and its associated row.
        ///     Setting it to true expands the row to show its children, while setting it to false
        ///     collapses the row to hide its children.
        ///   </para>
        ///   <para>
        ///     This property delegates to the row's <see cref="IExpander.IsExpanded" /> property.
        ///   </para>
        /// </remarks>
        public bool IsExpanded
        {
            get => Row.IsExpanded;
            set => Row.IsExpanded = value;
        }

        object IExpanderCell.Content => Content;
        IRow IExpanderCell.Row => Row;

        /// <summary>
        ///   Releases resources used by the <see cref="ExpanderCell{TModel}" />.
        /// </summary>
        /// <remarks>
        ///   Unsubscribes from events and disposes subscriptions to prevent memory leaks.
        /// </remarks>
        public void Dispose()
        {
            Row.PropertyChanged -= RowPropertyChanged;
            _subscription?.Dispose();
            (_inner as IDisposable)?.Dispose();
        }

        private void RowPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Row.IsExpanded) ||
                e.PropertyName == nameof(Row.ShowExpander))
            {
                RaisePropertyChanged(e.PropertyName);
            }
        }
    }
}
