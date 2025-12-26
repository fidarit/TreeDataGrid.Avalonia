using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Experimental.Data;
using Avalonia.Experimental.Data.Core;
using Avalonia.Media;

namespace Avalonia.Controls.Models.TreeDataGrid
{
    /// <summary>
    ///   Represents a cell in a <see cref="TreeDataGrid" /> that displays and edits text values.
    /// </summary>
    /// <typeparam name="T">The type of the underlying value stored in the cell.</typeparam>
    /// <remarks>
    ///   <para>
    ///     The <see cref="TextCell{T}" /> class provides text display and editing functionality
    ///     within a TreeDataGrid cell, supporting various text formatting options such as trimming,
    ///     wrapping, and alignment.
    ///   </para>
    ///   <para>
    ///     This class serves as the data model that backs the
    ///     <see cref="Primitives.TreeDataGridTextCell" /> primitive control. The
    ///     primitive control handles UI rendering and user interactions, while this model manages
    ///     the underlying data, state, and edit operations.
    ///   </para>
    ///   <para>
    ///     The generic type parameter <typeparamref name="T" /> allows the cell to store values of any type,
    ///     which are converted to and from string representations as needed for display and editing.
    ///   </para>
    /// </remarks>
    public class TextCell<T> : NotifyingBase, ITextCell, IDisposable, IEditableObject
    {
        private readonly IObserver<BindingValue<T>>? _binding;
        private readonly ITextCellOptions? _options;
        private readonly IDisposable? _subscription;
        private string? _editText;
        private T? _value;
        private bool _isEditing;

        /// <summary>
        ///   Initializes a new instance of the <see cref="TextCell{T}" /> class with a fixed value.
        /// </summary>
        /// <param name="value">The value to be displayed in the cell.</param>
        /// <remarks>
        ///   This constructor creates a read-only text cell with the specified value.
        ///   The value will be converted to a string representation for display.
        /// </remarks>
        public TextCell(T? value)
        {
            _value = value;
            IsReadOnly = true;
        }

        public TextCell(
            IObserver<BindingValue<T>> bindingObserver,
            IObservable<BindingValue<T>> bindingObservable,
            IObservable<BindingValue<bool>> isReadOnlyObservable,
            ITextCellOptions? options = null)
        {
            _binding = bindingObserver;
            _options = options;

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
        ///   Returns true if the cell is not read-only; otherwise, false.
        /// </remarks>
        public bool CanEdit => !IsReadOnly;
        /// <summary>
        ///   Gets the gestures that cause the cell to enter edit mode.
        /// </summary>
        /// <remarks>
        ///   Returns the edit gestures specified in the cell options, or the default gestures if
        ///   not specified. Default gestures include F2 key press and double-tap.
        /// </remarks>
        public BeginEditGestures EditGestures => _options?.BeginEditGestures ?? BeginEditGestures.Default;
        /// <summary>
        ///   Gets the text trimming mode for the cell.
        /// </summary>
        /// <remarks>
        ///   Determines how text is trimmed when it doesn't fit within the cell's bounds. Returns
        ///   the trimming mode specified in the cell options, or <see cref="TextTrimming.None" /> if
        ///   not specified.
        /// </remarks>
        public TextTrimming TextTrimming => _options?.TextTrimming ?? TextTrimming.None;
        /// <summary>
        ///   Gets the text wrapping mode for the cell.
        /// </summary>
        /// <remarks>
        ///   Determines whether and how text wraps within the cell. Returns the wrapping mode
        ///   specified in the cell options, or <see cref="TextWrapping.NoWrap" /> if not specified.
        /// </remarks>
        public TextWrapping TextWrapping => _options?.TextWrapping ?? TextWrapping.NoWrap;
        /// <summary>
        ///   Gets the text alignment mode for the cell.
        /// </summary>
        /// <remarks>
        ///   Determines how text is aligned horizontally within the cell. Returns the alignment
        ///   specified in the cell options, or <see cref="TextAlignment.Left" /> if not specified.
        /// </remarks>
        public TextAlignment TextAlignment => _options?.TextAlignment ?? TextAlignment.Left;
        /// <summary>
        ///   Gets a value indicating whether the cell is read-only.
        /// </summary>
        /// <remarks>
        ///   When true, the cell's value cannot be changed by the user through the UI.
        /// </remarks>
        public bool IsReadOnly { get; private set; }

        /// <summary>
        ///   Gets or sets the cell's value as a string.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     When getting this property, it returns the string representation of the cell's value,
        ///     applying any string formatting specified in the cell options.
        ///   </para>
        ///   <para>
        ///     When setting this property, it attempts to convert the string to the appropriate value type
        ///     and update the cell's value. If the cell is in edit mode, it updates the edit buffer instead.
        ///   </para>
        /// </remarks>
        public string? Text
        {
            get
            {
                if (_isEditing)
                    return _editText;
                else if (_options?.StringFormat is { } format)
                    return string.Format(_options.Culture ?? CultureInfo.CurrentCulture, format, _value);
                else
                    return _value?.ToString();
            }
            set
            {
                if (_isEditing)
                {
                    _editText = value;
                }
                else
                {
                    try
                    {
                        Value = (T?)Convert.ChangeType(value, typeof(T));
                    }
                    catch
                    {
                        // TODO: Data validation errors.
                    }
                }
            }
        }

        /// <summary>
        ///   Gets or sets the strongly-typed value of the cell.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     When setting this property, if the cell is not read-only and not in edit mode, the
        ///     change is propagated to the binding.
        ///   </para>
        /// </remarks>
        public T? Value
        {
            get => _value;
            set
            {
                if (RaiseAndSetIfChanged(ref _value, value) && !IsReadOnly && !_isEditing)
                    _binding!.OnNext(value!);
            }
        }

        object? ICell.Value => Value;

        /// <summary>
        ///   Begins an edit operation on the cell.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     When edit mode is entered, the current text representation of the value is stored
        ///     in an edit buffer. Changes to the <see cref="TextCell{T}.Text" /> property during edit mode
        ///     update this buffer rather than the underlying value.
        ///   </para>
        ///   <para>
        ///     The edit must be completed with <see cref="TextCell{T}.EndEdit()" /> or canceled with
        ///     <see cref="TextCell{T}.CancelEdit()" />.
        ///   </para>
        /// </remarks>
        public void BeginEdit()
        {
            if (!_isEditing && !IsReadOnly)
            {
                _editText = Text;
                _isEditing = true;
            }
        }

        /// <summary>
        ///   Cancels the current edit operation and restores the original value.
        /// </summary>
        /// <remarks>
        ///   Discards any changes made to the <see cref="TextCell{T}.Text" /> property since
        ///   <see cref="TextCell{T}.BeginEdit()" /> was called.
        /// </remarks>
        public void CancelEdit()
        {
            if (_isEditing)
            {
                _isEditing = false;
                _editText = null;
            }
        }

        /// <summary>
        ///   Commits the current edit operation and applies the changes to the cell value.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Converts the current edit text to a value of type <typeparamref name="T" /> and assigns
        ///     it to the <see cref="TextCell{T}.Value" /> property.
        ///   </para>
        ///   <para>
        ///     If the cell has a binding, the new value is propagated to the bound source.
        ///   </para>
        /// </remarks>
        public void EndEdit()
        {
            if (_isEditing)
            {
                var text = _editText;
                _isEditing = false;
                _editText = null;
                Text = text;
            }
        }

        /// <summary>
        ///   Releases resources used by the <see cref="TextCell{T}" />.
        /// </summary>
        public void Dispose()
        {
            _subscription?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
