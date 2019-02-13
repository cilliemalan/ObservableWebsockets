using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ObservableWebsockets.Internal
{
    /// <summary>
    /// A lightweight subject based on Rx Subject (https://github.com/dotnet/reactive/blob/master/Rx.NET/Source/src/System.Reactive/Subjects/Subject.cs)
    /// </summary>
    /// <typeparam name="T">thing thing</typeparam>
    class Subject<T> : IObservable<T>, IObserver<T>
    {
        private Subscription[] _observers = new Subscription[0];

        public void OnCompleted()
        {
            var observers = Volatile.Read(ref _observers);
            foreach (var observer in observers)
            {
                observer.Observer?.OnCompleted();
            }
        }

        public void OnError(Exception error)
        {
            if (error == null) throw new ArgumentNullException(nameof(error));

            var observers = Volatile.Read(ref _observers);
            foreach (var observer in observers)
            {
                observer.Observer?.OnError(error);
            }
        }

        public void OnNext(T value)
        {
            var observers = Volatile.Read(ref _observers);
            foreach (var observer in observers)
            {
                observer.Observer?.OnNext(value);
            }
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));

            Subscription disposer = null;
            for (; ; )
            {
                var observers = Volatile.Read(ref _observers);

                if (disposer == null)
                {
                    disposer = new Subscription(this, observer);
                }

                var newObservers = new Subscription[observers.Length + 1];
                newObservers[observers.Length] = disposer;
                Array.Copy(observers, 0, newObservers, 0, observers.Length);
                if (Interlocked.CompareExchange(ref _observers, newObservers, observers) == observers)
                {
                    return disposer;
                }
            }
        }

        private void Unsubscribe(Subscription observer)
        {
            for (; ; )
            {
                var observers = Volatile.Read(ref _observers);
                var len = observers.Length;

                var ix = Array.IndexOf(observers, observer);

                if (ix < 0) break;

                var newObservers = default(Subscription[]);
                if (len == 1)
                {
                    newObservers = new Subscription[0];
                }
                else if (len == 2 && ix == 0)
                {
                    newObservers = new Subscription[] { observers[1] };
                }
                else if (len == 2 && ix == 1)
                {
                    newObservers = new Subscription[] { observers[0] };
                }
                else
                {
                    newObservers = new Subscription[len - 1];
                    Array.Copy(observers, 0, newObservers, 0, ix);
                    Array.Copy(observers, ix + 1, newObservers, ix, len - ix - 1);
                }

                if (Interlocked.CompareExchange(ref _observers, newObservers, observers) == observers)
                {
                    break;
                }
            }
        }

        private class Subscription : IDisposable
        {
            private Subject<T> _subject;
            private IObserver<T> _observer;

            public Subscription(Subject<T> subject, IObserver<T> observer)
            {
                _subject = subject;
                _observer = observer;
            }

            public void Dispose()
            {
                var observer = Interlocked.Exchange(ref _observer, null);
                if (observer != null)
                {
                    _subject.Unsubscribe(this);
                    _subject = null;
                }
            }

            public IObserver<T> Observer => Volatile.Read(ref _observer);
        }
    }
}
