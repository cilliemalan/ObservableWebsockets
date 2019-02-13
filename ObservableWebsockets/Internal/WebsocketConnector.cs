﻿using System;
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

            Subject<string> incomingMessages = new Subject<string>();

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
                MemoryStream bigBuffer = null;
                var buffer = new ArraySegment<byte>(new byte[1024]);
                while (!isClosed())
                {
                    var receiveStatus = await webSocket.ReceiveAsync(buffer, cancellationToken);

                    if (receiveStatus.EndOfMessage)
                    {
                        ArraySegment<byte> bufferToUse;
                        int numBytesInBuffer;
                        if (bigBuffer != null)
                        {
                            bigBuffer.Write(buffer.Array, 0, receiveStatus.Count);
                            numBytesInBuffer = (int)bigBuffer.Length;
#if NETSTANDARD
                            bigBuffer.TryGetBuffer(out bufferToUse);
#else
                            byte[] msbuffer = bigBuffer.GetBuffer();
                            bufferToUse = new ArraySegment<byte>(msbuffer, 0, numBytesInBuffer);
#endif
                        }
                        else
                        {
                            bufferToUse = buffer;
                            numBytesInBuffer = receiveStatus.Count;
                        }

                        var str = Encoding.UTF8.GetString(bufferToUse.Array, 0, numBytesInBuffer);
                        incomingMessages.OnNext(str);
                    }
                    else
                    {
                        if (bigBuffer == null) bigBuffer = new MemoryStream();
                        bigBuffer.Write(buffer.Array, 0, receiveStatus.Count);
                    }
                }
            }

            async Task WriteLoopAsync()
            {
                while (!isClosed())
                {
                    if (messageQueue.TryDequeue(out var nextMessage))
                    {
                        if (nextMessage != null)
                        {
                            var msg = nextMessage.Item1;
                            var tp = nextMessage.Item2;
                            var end = nextMessage.Item3;
                            var arraySegment = new ArraySegment<byte>(msg);
                            await webSocket.SendAsync(arraySegment, tp, true, cancellationToken);
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
            }

            Exception caughtException = null;
            try
            {
                var readwait = ReadLoopAsync();
                var writewait = WriteLoopAsync();
                var handler = new WebSocketHandler((m, t, e) =>
                    {
                        messageQueue?.Enqueue(Tuple.Create(m, t, e));
                        messageSentAre.Trigger();
                    },
                    path,
                    incomingMessages.Subscribe);

                await Task.Yield();
                acceptHandler(handler);
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