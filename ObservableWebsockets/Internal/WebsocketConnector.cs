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

            bool hasCompleted = false;
            cancellationToken.Register(() =>
            {
                if (!hasCompleted)
                {
                    incomingMessages.OnCompleted();
                    hasCompleted = true;
                }
            });

            bool isClosed() => cts.IsCancellationRequested ||
                hasCompleted ||
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
                        incomingMessages.OnNext((properSegment, receiveStatus.MessageType, receiveStatus.EndOfMessage));
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

            Exception caughtException = null;
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
            catch (Exception ex)
            {
                caughtException = ex;
            }
            finally
            {
                messageQueue = null;
            }

            if (!hasCompleted)
            {
                if (caughtException != null &&
                    !(caughtException is WebSocketException wse && wse.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely) &&
                    !(caughtException is OperationCanceledException))
                {
                    incomingMessages.OnError(caughtException);
                }

                incomingMessages.OnCompleted();
                hasCompleted = true;
            }

            try
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
            catch { }
        }
    }
}