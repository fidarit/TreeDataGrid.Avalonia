using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;

namespace Avalonia.Experimental.Data.Core
{
    /// <summary>
    ///   Lightweight base class for observable implementations.
    /// </summary>
    /// <typeparam name="T">The observable type.</typeparam>
    /// <remarks>
    ///   <para>
    ///     This is an experimental API that is subject to change or removal without notice.
    ///   </para>
    ///   <para>
    ///     ObservableBase{T} is rather heavyweight in terms of allocations and memory
    ///     usage. This class provides a more lightweight base for some internal observable types
    ///     in the Avalonia framework.
    ///   </para>
    /// </remarks>
    public abstract class LightweightObservableBase<T> : IObservable<T>
    {
        private Exception? _error;
        private List<IObserver<T>>? _observers = [];

        /// <summary>
        ///   Gets a value indicating whether this observable has any observers.
        /// </summary>
        public bool HasObservers => _observers?.Count > 0;

        /// <inheritdoc />
        public IDisposable Subscribe(IObserver<T> observer)
        {
            _ = observer ?? throw new ArgumentNullException(nameof(observer));

            //Dispatcher.UIThread.VerifyAccess();

            var first = false;

            for (; ; )
            {
                if (Volatile.Read(ref _observers) == null)
                {
                    if (_error != null)
                    {
                        observer.OnError(_error);
                    }
                    else
                    {
                        observer.OnCompleted();
                    }

                    return Disposable.Empty;
                }

                lock (this)
                {
                    if (_observers == null)
                    {
                        continue;
                    }

                    first = _observers.Count == 0;
                    _observers.Add(observer);
                    break;
                }
            }

            if (first)
            {
                Initialize();
            }

            Subscribed(observer, first);

            return new RemoveObserver(this, observer);
        }

        void Remove(IObserver<T> observer)
        {
            if (Volatile.Read(ref _observers) != null)
            {
                lock (this)
                {
                    var observers = _observers;

                    if (observers != null)
                    {
                        observers.Remove(observer);

                        if (observers.Count == 0)
                        {
                            observers.TrimExcess();
                            Deinitialize();
                        }
                    }
                }
            }
        }

        sealed class RemoveObserver : IDisposable
        {
            LightweightObservableBase<T>? _parent;

            IObserver<T>? _observer;

            public RemoveObserver(LightweightObservableBase<T> parent, IObserver<T> observer)
            {
                _parent = parent;
                Volatile.Write(ref _observer, observer);
            }

            public void Dispose()
            {
                var observer = _observer;
                Interlocked.Exchange(ref _parent, null)?.Remove(observer!);
                _observer = null;
            }
        }

        /// <summary>
        ///   Called when the first observer subscribes.
        /// </summary>
        protected abstract void Initialize();
        /// <summary>
        ///   Called when the last observer unsubscribes.
        /// </summary>
        protected abstract void Deinitialize();

        /// <summary>
        ///   Publishes the next value to all observers.
        /// </summary>
        /// <param name="value">The value to publish.</param>
        protected void PublishNext(T value)
        {
            if (Volatile.Read(ref _observers) != null)
            {
                IObserver<T>[]? observers = null;
                int count = 0;

                // Optimize for the common case of 1/2/3 observers.
                IObserver<T>? observer0 = null;
                IObserver<T>? observer1 = null;
                IObserver<T>? observer2 = null;
                lock (this)
                {
                    if (_observers == null)
                    {
                        return;
                    }

                    count = _observers.Count;
                    switch (count)
                    {
                        case 3:
                            observer0 = _observers[0];
                            observer1 = _observers[1];
                            observer2 = _observers[2];
                            break;
                        case 2:
                            observer0 = _observers[0];
                            observer1 = _observers[1];
                            break;
                        case 1:
                            observer0 = _observers[0];
                            break;
                        case 0:
                            return;
                        default:
                            {
                                observers = ArrayPool<IObserver<T>>.Shared.Rent(count);
                                _observers.CopyTo(observers);
                                break;
                            }
                    }
                }

                if (observer0 != null)
                {
                    observer0.OnNext(value);
                    observer1?.OnNext(value);
                    observer2?.OnNext(value);
                }
                else if (observers != null)
                {
                    for (int i = 0; i < count; i++)
                    {
                        observers[i].OnNext(value);
                        // Avoid memory leak by clearing the reference.
                        observers[i] = null!;
                    }

                    ArrayPool<IObserver<T>>.Shared.Return(observers);
                }
            }
        }

        /// <summary>
        ///   Publishes completion notification to all observers.
        /// </summary>
        protected void PublishCompleted()
        {
            if (Volatile.Read(ref _observers) != null)
            {
                IObserver<T>[] observers;

                lock (this)
                {
                    if (_observers == null)
                    {
                        return;
                    }
                    observers = _observers.ToArray();
                    Volatile.Write(ref _observers, null);
                }

                foreach (var observer in observers)
                {
                    observer.OnCompleted();
                }

                Deinitialize();
            }
        }

        /// <summary>
        ///   Publishes an error notification to all observers.
        /// </summary>
        /// <param name="error">The exception that occurred.</param>
        protected void PublishError(Exception error)
        {
            if (Volatile.Read(ref _observers) != null)
            {

                IObserver<T>[] observers;

                lock (this)
                {
                    if (_observers == null)
                    {
                        return;
                    }

                    _error = error;
                    observers = _observers.ToArray();
                    Volatile.Write(ref _observers, null);
                }

                foreach (var observer in observers)
                {
                    observer.OnError(error);
                }

                Deinitialize();
            }
        }

        /// <summary>
        ///   Called after an observer has subscribed.
        /// </summary>
        /// <param name="observer">The observer that subscribed.</param>
        /// <param name="first">True if this is the first observer; otherwise false.</param>
        protected virtual void Subscribed(IObserver<T> observer, bool first)
        {
        }
    }
}
