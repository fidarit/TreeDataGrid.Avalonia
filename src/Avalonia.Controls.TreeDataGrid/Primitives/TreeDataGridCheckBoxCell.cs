using System;
using System.ComponentModel;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    ///   A control in a <see cref="TreeDataGrid" /> that displays a cell which allows editing of
    ///   boolean values using a checkbox.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TreeDataGridCheckBoxCell displays boolean values as checkboxes in a TreeDataGrid. It supports
    ///     two-state (checked/unchecked) or three-state (checked/unchecked/indeterminate) behavior, and
    ///     can be configured as read-only to prevent user modification.
    ///   </para>
    ///   <para>
    ///     This cell type is designed to work with the <see cref="CheckBoxCell" /> data model, which provides
    ///     the underlying value and state management. Changes to the checkbox state are reflected in the
    ///     underlying model, and changes to the model are automatically reflected in the UI.
    ///   </para>
    /// </remarks>
    public class TreeDataGridCheckBoxCell : TreeDataGridEditableCell
    {
        /// <summary>
        ///   Defines the <see cref="IsThreeState" /> property.
        /// </summary>
        public static readonly DirectProperty<TreeDataGridCheckBoxCell, bool> IsThreeStateProperty =
            AvaloniaProperty.RegisterDirect<TreeDataGridCheckBoxCell, bool>(
                nameof(IsThreeState),
                o => o.IsThreeState,
                (o, v) => o.IsThreeState = v);

        /// <summary>
        ///   Defines the <see cref="Value" /> property.
        /// </summary>
        public static readonly DirectProperty<TreeDataGridCheckBoxCell, bool?> ValueProperty =
            AvaloniaProperty.RegisterDirect<TreeDataGridCheckBoxCell, bool?>(
                nameof(Value),
                o => o.Value,
                (o, v) => o.Value = v);

        private bool _isThreeState;
        private bool? _value;

        /// <summary>
        ///   Gets or sets a value indicating whether the checkbox supports three states.
        /// </summary>
        /// <remarks>
        ///   When set to true, the checkbox can display three states: checked (true), unchecked (false),
        ///   and indeterminate (null). When set to false, the checkbox only supports checked and unchecked states.
        /// </remarks>
        public bool IsThreeState
        {
            get => _isThreeState;
            set => SetAndRaise(IsThreeStateProperty, ref _isThreeState, value);
        }

        /// <summary>
        ///   Gets or sets the current value of the checkbox.
        /// </summary>
        /// <value>
        ///   true if the checkbox is checked, false if it is unchecked, or null if it is in the
        ///   indeterminate state (requires <see cref="IsThreeState" /> to be true).
        /// </value>
        /// <remarks>
        ///   Setting this property updates the visual state of the checkbox and propagates the
        ///   change to the underlying data model.
        /// </remarks>
        public bool? Value
        {
            get => _value;
            set
            {
                if (SetAndRaise(ValueProperty, ref _value, value))
                {
                    if (Model is CheckBoxCell cell)
                        cell.Value = value;
                    RaiseCellValueChanged();
                }
            }
        }

        /// <summary>
        ///   Prepares the cell for display with the specified data.
        /// </summary>
        /// <param name="factory">The element factory used to create child elements.</param>
        /// <param name="selection">The selection interaction model.</param>
        /// <param name="model">The cell's data model.</param>
        /// <param name="columnIndex">The index of the cell's column.</param>
        /// <param name="rowIndex">The index of the cell's row.</param>
        /// <exception cref="InvalidOperationException">
        ///   Thrown when the model is not a <see cref="CheckBoxCell" />.
        /// </exception>
        /// <inheritdoc />
        public override void Realize(
            TreeDataGridElementFactory factory,
            ITreeDataGridSelectionInteraction? selection,
            ICell model,
            int columnIndex,
            int rowIndex)
        {
            if (model is CheckBoxCell cell)
            {
                IsReadOnly = cell.IsReadOnly;
                IsThreeState = cell.IsThreeState;
                Value = cell.Value;
            }
            else
            {
                throw new InvalidOperationException("Invalid cell model.");
            }

            base.Realize(factory, selection, model, columnIndex, rowIndex);
        }

        /// <summary>
        ///   Handles property change notifications from the cell's model.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        /// <remarks>
        ///   Updates the cell's <see cref="Value" /> property when the underlying model's value changes.
        /// </remarks>
        protected override void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            base.OnModelPropertyChanged(sender, e);

            if (e.PropertyName == nameof(CheckBoxCell.Value) && Model is CheckBoxCell checkBoxCell)
                Value = checkBoxCell.Value;
        }
    }
}
