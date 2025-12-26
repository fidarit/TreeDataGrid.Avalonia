using System;
using System.Collections.Generic;

namespace Avalonia.Controls.Selection
{
    /// <summary>
    ///   Maintains the row selection state for a <see cref="TreeDataGrid" /> control.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     ITreeDataGridRowSelectionModel provides row selection functionality for
    ///     <see cref="TreeDataGrid" />. It is most likely that you will want to use the generic
    ///     <see cref="ITreeDataGridRowSelectionModel{T}" /> instead.
    ///   </para>
    /// </remarks>
    public interface ITreeDataGridRowSelectionModel : ITreeSelectionModel, ITreeDataGridSelection
    {
    }

    /// <summary>
    ///   Provides strongly-typed access to the row selection state for a <see cref="TreeDataGrid" />
    ///   control.
    /// </summary>
    /// <typeparam name="T">The type of items in the row collection.</typeparam>
    /// <remarks>
    ///   <see cref="ITreeDataGridRowSelectionModel{T}" /> extends the non-generic
    ///   <see cref="ITreeDataGridRowSelectionModel" /> interface to provide strongly-typed access to
    ///   the selected rows.
    /// </remarks>
    public interface ITreeDataGridRowSelectionModel<T> : ITreeDataGridRowSelectionModel
    {
        /// <summary>
        ///   Gets the currently selected row item.
        /// </summary>
        /// <value>
        ///   The primary selected row item, or the default value for <typeparamref name="T" /> if no row is selected.
        /// </value>
        new T? SelectedItem { get; }
        /// <summary>
        ///   Gets the currently selected row items.
        /// </summary>
        /// <value>
        ///   A read-only list of the selected row items, with the type <typeparamref name="T" />.
        /// </value>
        new IReadOnlyList<T?> SelectedItems { get; }
        /// <summary>
        ///   Occurs when the row selection changes.
        /// </summary>
        /// <remarks>
        ///   This event is raised when the selection of rows changes, either through user interaction
        ///   or programmatically. It provides strongly-typed access to the selection changes.
        /// </remarks>
        new event EventHandler<TreeSelectionModelSelectionChangedEventArgs<T>>? SelectionChanged;
    }
}
