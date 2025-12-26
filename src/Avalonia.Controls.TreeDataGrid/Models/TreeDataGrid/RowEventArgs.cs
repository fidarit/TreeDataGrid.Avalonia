using System;

namespace Avalonia.Controls.Models.TreeDataGrid
{
    /// <summary>
    ///   Provides a base class for events related to row operations in a TreeDataGrid.
    /// </summary>
    /// <remarks>
    ///   This base class provides non-generic access to selection change information, while the
    ///   generic version <see cref="RowEventArgs{TRow}" /> provides strongly-typed access to the
    ///   same information.
    /// </remarks>
    public abstract class RowEventArgs : EventArgs
    {
        /// <summary>
        ///   Gets the row associated with this event.
        /// </summary>
        /// <value>
        ///   An <see cref="IRow" /> instance representing the row involved in the event.
        /// </value>
        public IRow Row => GetUntypedRow();
        /// <summary>
        ///   When overridden in a derived class, returns the untyped row associated with this event.
        /// </summary>
        /// <returns>
        ///   An <see cref="IRow" /> instance representing the row involved in the event.
        /// </returns>
        protected abstract IRow GetUntypedRow();

        /// <summary>
        ///   Creates a new instance of <see cref="RowEventArgs{TRow}" /> for the specified row.
        /// </summary>
        /// <typeparam name="T">
        ///   The type of the row, which must implement <see cref="IRow" />.
        /// </typeparam>
        /// <param name="row">The row instance to associate with the event arguments.</param>
        /// <returns>
        ///   A new <see cref="RowEventArgs{TRow}" /> instance containing the specified row.
        /// </returns>
        public static RowEventArgs<T> Create<T>(T row) where T : IRow
        {
            return new RowEventArgs<T>(row);
        }
    }

    /// <summary>
    ///   Provides strongly typed event data for row-related events in a TreeDataGrid.
    /// </summary>
    /// <typeparam name="TRow">
    ///   The specific type of row, which must implement <see cref="IRow" />.
    /// </typeparam>
    /// <remarks>
    ///   <para>
    ///     The <see cref="RowEventArgs{TRow}" /> class extends <see cref="RowEventArgs" /> to provide
    ///     type-specific access to the row involved in an event. This allows event handlers to
    ///     work with the concrete row type without casting.
    ///   </para>
    /// </remarks>
    public class RowEventArgs<TRow> : RowEventArgs
        where TRow : IRow
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="RowEventArgs{TRow}" /> class with the specified row.
        /// </summary>
        /// <param name="row">The row instance to associate with the event arguments.</param>
        public RowEventArgs(TRow row) => Row = row;
        /// <summary>
        ///   Gets the row associated with this event.
        /// </summary>
        /// <value>
        ///   A <typeparamref name="TRow" /> instance representing the row involved in the event.
        /// </value>
        /// <remarks>
        ///   This property hides the base <see cref="RowEventArgs.Row" /> property to provide
        ///   strongly-typed access to the row.
        /// </remarks>
        public new TRow Row { get; }
        /// <summary>
        ///   Returns the row as an <see cref="IRow" /> instance.
        /// </summary>
        /// <returns>
        ///   The row associated with this event as an <see cref="IRow" /> instance.
        /// </returns>
        /// <remarks>
        ///   This method implements the abstract method from the base class to provide access to
        ///   the typed row through the non-generic interface.
        /// </remarks>
        protected override IRow GetUntypedRow() => Row;
    }
}
