using System;
using Avalonia.Data;
using Avalonia.Experimental.Data;
using Avalonia.Experimental.Data.Core;

namespace Avalonia.Controls.Models.TreeDataGrid
{
    /// <summary>
    ///   Represents a cell in a <see cref="TreeDataGrid" /> that contains a checkbox control.
    /// </summary>
    /// <remarks>
    ///   The <see cref="CheckBoxCell" /> class provides checkbox functionality within a TreeDataGrid cell,
    ///   supporting both read-only and editable states, as well as two-state and three-state checkbox behavior.
    /// 
    ///   This class serves as the data model that backs the
    ///   <see cref="Primitives.TreeDataGridCheckBoxCell" /> primitive control. The
    ///   control handles UI rendering and user interactions, while this model manages the underlying
    ///   data and state.
    /// </remarks>
    public class CheckBoxCell : NotifyingBase, IEditableCell, IDisposable
    {
        private readonly IObserver<BindingValue<bool?>>? _binding;
        private readonly IDisposable? _subscription;
        private bool? _value;

        /// <summary>
        ///   Initializes a new instance of the <see cref="CheckBoxCell" /> class with a fixed value.
        /// </summary>
        /// <param name="value">The checkbox state value.</param>
        /// <remarks>
        ///   This constructor creates a read-only checkbox cell with the specified value.
        /// </remarks>
        public CheckBoxCell(bool? value)
        {
            _value = value;
            IsReadOnly = true;
        }

        public CheckBoxCell(
            IObserver<BindingValue<bool?>> bindingObserver,
            IObservable<BindingValue<bool?>> bindingObservable,
            IObservable<BindingValue<bool>> isReadOnlyObservable,
            bool isThreeState)
        {
            _binding = bindingObserver;
            IsThreeState = isThreeState;

            _subscription = new CompositeDisposable(
                bindingObservable.Subscribe(x =>
                {
                    if (x.HasValue)
                        Value = x.Value;
                }),
                isReadOnlyObservable.Subscribe(x =>
                {
                    if (x.HasValue)
                        IsReadOnly = x.Value;
                }));
        }

        /// <summary>
        ///   Gets a value indicating whether the cell can enter edit mode.
        /// </summary>
        /// <remarks>
        ///   Always returns false as checkbox cells don't support explicit edit mode.
        ///   Instead, they are toggled directly when clicked.
        /// </remarks>
        public bool CanEdit => false;
        /// <summary>
        ///   Gets the gestures that cause the cell to enter edit mode.
        /// </summary>
        /// <remarks>
        ///   Returns <see cref="BeginEditGestures.None" /> as checkbox cells don't use an edit mode.
        /// </remarks>
        public BeginEditGestures EditGestures => BeginEditGestures.None;
        public bool SingleTapEdit => false;
        /// <summary>
        ///   Gets a value indicating whether the cell is read-only.
        /// </summary>
        /// <remarks>
        ///   When true, the checkbox value cannot be changed by the user.
        /// </remarks>
        public bool IsReadOnly { get; private set; }
        /// <summary>
        ///   Gets a value indicating whether the checkbox supports three states.
        /// </summary>
        /// <remarks>
        ///   When true, the checkbox can be in checked, unchecked, or indeterminate states.
        ///   When false, only checked and unchecked states are supported.
        /// </remarks>
        public bool IsThreeState { get; }

        /// <summary>
        ///   Gets or sets the value of the checkbox.
        /// </summary>
        /// <remarks>
        ///   The value can be true (checked), false (unchecked), or null (indeterminate, if
        ///   <see cref="IsThreeState" /> is true). Setting this property will update the underlying
        ///   data if the cell is not read-only.
        /// </remarks>
        public bool? Value
        {
            get => _value;
            set
            {
                if (RaiseAndSetIfChanged(ref _value, value) && !IsReadOnly)
                    _binding!.OnNext(value);
            }
        }

        /// <summary>
        ///   Gets the cell value for the <see cref="ICell" /> interface.
        /// </summary>
        object? ICell.Value => Value;

        /// <summary>
        ///   Disposes the resources used by the <see cref="CheckBoxCell" />.
        /// </summary>
        public void Dispose()
        {
            _subscription?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
