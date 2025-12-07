using System;
using System.ComponentModel;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Experimental.Data;

namespace Avalonia.Controls.Models.TreeDataGrid
{
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

        public bool CanEdit => !IsReadOnly;
        public BeginEditGestures EditGestures => _options?.BeginEditGestures ?? BeginEditGestures.Default;
        public Func<Control, IDataTemplate> GetCellTemplate { get; }
        public Func<Control, IDataTemplate>? GetCellEditingTemplate { get; }
        public bool IsReadOnly { get; private set; }
        public object? Value { get; }

        void IEditableObject.BeginEdit() => (Value as IEditableObject)?.BeginEdit();
        void IEditableObject.CancelEdit() => (Value as IEditableObject)?.CancelEdit();
        void IEditableObject.EndEdit() => (Value as IEditableObject)?.EndEdit();

        void IDisposable.Dispose() => _subscription.Dispose();
    }
}
