using System;
using Avalonia.Experimental.Data.Core;
using Avalonia.Reactive;

namespace Avalonia.Experimental.Data
{
    internal static class ObservableEx
    {
        public static IObservable<T> SingleValue<T>(T value)
        {
            return new SingleValueImpl<T>(value);
        }

        private sealed class SingleValueImpl<T> : IObservable<T>
        {
            private readonly T _value;

            public SingleValueImpl(T value)
            {
                _value = value;
            }
            public IDisposable Subscribe(IObserver<T> observer)
            {
                observer.OnNext(_value);
                return Disposable.Empty;
            }
        }


        public static IDisposable Subscribe<T>(this IObservable<T> observable, Action<T> onNext)
        {
            return observable.Subscribe(new AnonymousObserver<T>(onNext));
        }
    }
}
