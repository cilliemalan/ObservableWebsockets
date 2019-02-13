using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ObservableWebsockets.Internal
{
    internal class AsyncAutoResetEvent
    {
        private TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();

        public void Trigger()
        {
            var newTcs = new TaskCompletionSource<int>();
            for (; ; )
            {
                var tcs = this.tcs;
                if (Interlocked.CompareExchange(ref this.tcs, newTcs, tcs) == tcs)
                {
                    tcs.SetResult(0);
                    break;
                }
            }
        }

        public Task WaitAsync(CancellationToken cancellationToken = default)
        {
            var tcs = Volatile.Read(ref this.tcs);
            if (tcs == null) return Task.FromResult(0);

            if (cancellationToken.CanBeCanceled)
            {
                TaskCompletionSource<int> waitTcs = new TaskCompletionSource<int>();
                cancellationToken.Register(() => waitTcs.TrySetCanceled());
                tcs.Task.ContinueWith(t => waitTcs.TrySetResult(0));
                return waitTcs.Task;
            }
            else
            {
                return tcs.Task;
            }
        }
    }
}
