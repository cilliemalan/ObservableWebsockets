using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ObservableWebsockets.Tests.Common
{
    public class Trigger
    {
        private TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();
        private bool _trigerred;
        private string _message;

        protected Trigger(string message)
        {
            _message = message;
        }

        public void Mark() => Mark(null);

        protected void Mark(object o) => _tcs.SetResult(o);

        public TaskAwaiter GetAwaiter()
        {
            CancellationTokenSource cts = new CancellationTokenSource(5000);
            cts.Token.Register(() => _tcs.TrySetCanceled(cts.Token));
            return ((Task)_tcs.Task).GetAwaiter();
        }

        protected TaskAwaiter<T> GetAwaiterInternal<T>()
        {
            CancellationTokenSource cts = new CancellationTokenSource(5000);
            cts.Token.Register(() => _tcs.TrySetCanceled(cts.Token));

            async Task<T> CastTask() => (T)await _tcs.Task;
            return CastTask().GetAwaiter();
        }

        

        public static Trigger Create(string message = "The trigger wait timed out.")
        {
            return new Trigger(message);
        }

        public static Trigger<T> Create<T>(string message = "The trigger wait timed out.")
        {
            return new Trigger<T>(message);
        }
    }

    public class Trigger<T> : Trigger
    {
        private T _value;

        internal Trigger(string message)
            : base(message)
        {
        }

        public void Mark(T value) => base.Mark(value);
        public new TaskAwaiter<T> GetAwaiter() => GetAwaiterInternal<T>();
    }
}
