using System;
using System.ComponentModel;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Experimental.Data;

namespace Avalonia.Controls.Models.TreeDataGrid
{
    /// <summary>
    ///   Represents a cell in a <see cref="TreeDataGrid" /> that displays its contents using data
    ///   templates.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The <see cref="TemplateCell" /> class provides template-based rendering and editing
    ///     capabilities for cells in a TreeDataGrid. It allows for customizable content presentation
    ///     beyond simple text or checkbox cells.
    ///   </para>
    ///   <para>
    ///     This class serves as the data model that backs the
    ///     <see cref="Primitives.TreeDataGridTemplateCell" /> primitive control. The
    ///     primitive control handles UI rendering and user interactions, while this model provides
    ///     access to the templates and the underlying value.
    ///   </para>
    ///   <para>
    ///     The cell can have two different visual representations: one for display mode (using
    ///     <see cref="GetCellTemplate" />) and optionally one for editing mode (using
    ///     <see cref="GetCellEditingTemplate" />).
    ///   </para>
    /// </remarks>
    public class TemplateCell : IEditableCell, IEditableObject, IDisposable
    {
        private ITemplateCellOptions? _options;
        private readonly IDisposable _subscription;

        public TemplateCell(
            object? value,
            Func<Control, IDataTemplate> getCellTemplate,
            Func<Control, IDataTemplate>? getCellEditingTemplate,
            IObservable<BindingValue<bool>> isReadOnlyObservable,
            ITemplateCellOptions? options)
        {
            GetCellTemplate = getCellTemplate;
            GetCellEditingTemplate = getCellEditingTemplate;
            Value = value;
            _options = options;

            _subscription = isReadOnlyObservable.Subscribe(x =>
            {
                if (x.HasValue)
                    IsReadOnly = x.Value;
            });
        }

        /// <summary>
        ///   Gets a value indicating whether the cell can enter edit mode.
        /// </summary>
        /// <remarks>
        ///   Returns true if an editing template has been provided through
        ///   <see cref="GetCellEditingTemplate" />; otherwise, false.
        /// </remarks>
        public bool CanEdit => !IsReadOnly;
        /// <summary>
        ///   Gets the gesture(s) that will cause the cell to enter edit mode.
        /// </summary>
        /// <remarks>
        ///   Returns the edit gestures specified in the options, or
        ///   <see cref="BeginEditGestures.Default" /> if no options were provided.
        /// </remarks>
        public BeginEditGestures EditGestures => _options?.BeginEditGestures ?? BeginEditGestures.Default;
        /// <summary>
        ///   Gets a function that retrieves the data template for displaying the cell value.
        /// </summary>
        /// <remarks>
        ///   This function is used by the
        ///   <see cref="Primitives.TreeDataGridTemplateCell" /> to obtain the
        ///   template to use for rendering the cell's content when not in edit mode.
        /// </remarks>
        public Func<Control, IDataTemplate> GetCellTemplate { get; }
        /// <summary>
        ///   Gets a function that retrieves the data template for editing the cell value, or null if
        ///   the cell is not editable.
        /// </summary>
        /// <remarks>
        ///   This function is used by the
        ///   <see cref="Primitives.TreeDataGridTemplateCell" />
        ///   to obtain the template to use for rendering the cell's content when in edit mode.
        /// </remarks>
        public Func<Control, IDataTemplate>? GetCellEditingTemplate { get; }
        public bool IsReadOnly { get; private set; }
        /// <summary>
        ///   Gets the underlying value of the cell.
        /// </summary>
        /// <remarks>
        ///   This is the object that will be used as the data context for the template.
        /// </remarks>
        public object? Value { get; }

        /// <summary>
        ///   Begins an edit operation on the cell's value.
        /// </summary>
        /// <remarks>
        ///   If the cell's <see cref="Value" /> implements <see cref="IEditableObject" />, this method
        ///   delegates the call to the value's <see cref="IEditableObject.BeginEdit()" /> method.
        /// </remarks>
        void IEditableObject.BeginEdit() => (Value as IEditableObject)?.BeginEdit();
        /// <summary>
        ///   Cancels the current edit operation.
        /// </summary>
        /// <remarks>
        ///   If the cell's <see cref="Value" /> implements <see cref="IEditableObject" />, this method
        ///   delegates the call to the value's <see cref="IEditableObject.CancelEdit()" /> method.
        /// </remarks>
        void IEditableObject.CancelEdit() => (Value as IEditableObject)?.CancelEdit();
        /// <summary>
        ///   Commits the current edit operation.
        /// </summary>
        /// <remarks>
        ///   If the cell's <see cref="Value" /> implements <see cref="IEditableObject" />, this method
        ///   delegates the call to the value's <see cref="IEditableObject.EndEdit()" /> method.
        /// </remarks>
        void IEditableObject.EndEdit() => (Value as IEditableObject)?.EndEdit();

        void IDisposable.Dispose() => _subscription.Dispose();
    }
}
