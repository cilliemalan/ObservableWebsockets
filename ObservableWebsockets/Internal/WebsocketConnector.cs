using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ObservableWebsockets.Internal
{
    internal class WebsocketConnector
    {
        internal static async Task HandleWebsocketAsync(string path, WebSocket webSocket, Action<IObservableWebsocket> acceptHandler)
        {
            var messageQueue = new System.Collections.Concurrent.ConcurrentQueue<Tuple<byte[], WebSocketMessageType, bool>>();
            var messageSentAre = new AsyncAutoResetEvent();

            CancellationTokenSource cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            var incomingMessages = new Subject<(ArraySegment<byte> message, WebSocketMessageType messageType, bool endOfMessage)>();


            // run to complete the observable sequence. Called on error, cancellation, or close.
            int hasCompleted = 0;
            void Complete(Exception ex = null)
            {
                if (Interlocked.CompareExchange(ref hasCompleted, 1, 0) == 0)
                {
                    if (ex != null)
                    {
                        try
                        {
                            incomingMessages.OnError(ex);
                        }
                        catch { }
                    }
                    else
                    {
                        try
                        {
                            incomingMessages.OnCompleted();
                        }
                        catch (Exception e)
                        {
                            try
                            {
                                incomingMessages.OnError(AggregateExceptions(ex, e));
                            }
                            catch { }
                        }
                    }
                }
            }

            cancellationToken.Register(() => Complete());

            bool isClosed() => cts.IsCancellationRequested ||
                Volatile.Read(ref hasCompleted) == 1 ||
                webSocket.State != WebSocketState.Open;

            async Task ReadLoopAsync()
            {
                while (!isClosed())
                {
                    try
                    {
                        var buffer = new ArraySegment<byte>(new byte[1024]);
                        var receiveStatus = await webSocket.ReceiveAsync(buffer, cancellationToken);
                        var properSegment = new ArraySegment<byte>(buffer.Array, 0, receiveStatus.Count);
                        try
                        {
                            incomingMessages.OnNext((properSegment, receiveStatus.MessageType, receiveStatus.EndOfMessage));
                        }
                        catch (Exception ex)
                        {
                            Complete(ex);
                            cts.Cancel();
                            throw;
                        }
                    }
                    catch (WebSocketException)
                    {
                        if (!isClosed()) throw;
                    }
                }

                // signal that we are closed
                cts.Cancel();
            }

            async Task WriteLoopAsync()
            {
                while (!isClosed())
                {
                    if (messageQueue.TryDequeue(out var nextMessage))
                    {
                        if (nextMessage != null)
                        {
                            try
                            {
                                var msg = nextMessage.Item1;
                                var tp = nextMessage.Item2;
                                var end = nextMessage.Item3;
                                var arraySegment = new ArraySegment<byte>(msg);
                                await webSocket.SendAsync(arraySegment, tp, true, cancellationToken);
                            }
                            catch (WebSocketException)
                            {
                                if (!isClosed()) throw;
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            await messageSentAre.WaitAsync(cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                        }
                    }
                }

                // signal that we are closed just in case it hasn't been triggered already
                cts.Cancel();
            }

            try
            {
                var handler = new WebSocketHandler((m, t, e) =>
                    {
                        messageQueue?.Enqueue(Tuple.Create(m, t, e));
                        messageSentAre.Trigger();
                    },
                    path,
                    incomingMessages.Subscribe);

                await Task.Yield();
                acceptHandler(handler);
                await Task.Yield();
                var readwait = ReadLoopAsync();
                var writewait = WriteLoopAsync();
                await Task.WhenAll(readwait, writewait);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Complete(ex);
            }
            finally
            {
                Complete();
            }

            try
            {
                if (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, default);
                }
            }
            catch { }
        }

        private static Exception AggregateExceptions(Exception ex, Exception e)
        {
            if (ex == null)
            {
                ex = e;
            }
            else if (ex is AggregateException ae)
            {
                if (e is AggregateException nae)
                {
                    ex = new AggregateException(ae.InnerExceptions.Concat(nae.InnerExceptions));
                }
                else
                {
                    ex = new AggregateException(ae.InnerExceptions.Concat(new[] { e }));
                }
            }
            else
            {
                if (e is AggregateException nae)
                {
                    ex = new AggregateException(new[] { ex }.Concat(nae.InnerExceptions));
                }
                else
                {
                    ex = new AggregateException(new[] { ex, e });
                }
            }

            return ex;
        }
    }
}