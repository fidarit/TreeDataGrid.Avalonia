using System;
using Avalonia.Data;
using Avalonia.Experimental.Data;
using Avalonia.Experimental.Data.Core;

namespace Avalonia.Controls.Models.TreeDataGrid
{
    public class CheckBoxCell : NotifyingBase, IEditableCell, IDisposable
    {
        private readonly IObserver<BindingValue<bool?>>? _binding;
        private readonly IDisposable? _subscription;
        private bool? _value;

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

        public bool CanEdit => false;
        public BeginEditGestures EditGestures => BeginEditGestures.None;
        public bool SingleTapEdit => false;
        public bool IsReadOnly { get; private set; }
        public bool IsThreeState { get; }

        public bool? Value
        {
            get => _value;
            set
            {
                if (RaiseAndSetIfChanged(ref _value, value) && !IsReadOnly)
                    _binding!.OnNext(value);
            }
        }

        object? ICell.Value => Value;

        public void Dispose()
        {
            _subscription?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
