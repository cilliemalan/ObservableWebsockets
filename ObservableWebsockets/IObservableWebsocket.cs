﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;

namespace ObservableWebsockets
{
    /// <summary>
    /// An observable interface to a websocket connection. Subscribers will receive incoming messages.
    /// </summary>
#if HAS_VALUETUPLE
    public interface IObservableWebsocket : IObservable<(ArraySegment<byte> message,WebSocketMessageType messageType,bool endOfMessage)>
#else
    public interface IObservableWebsocket : IObservable<Tuple<ArraySegment<byte>, WebSocketMessageType, bool>>
#endif
    {
        /// <summary>
        /// Send a message out of the websocket. Sent messages are queued on the websocket thread and are not guaranteed to be delivered
        /// as the websocket may be disconnected before a message is dequeued.
        /// </summary>
        /// <param name="message">The message to send. This is not an <see cref="ArraySegment[byte]"/> because the message is not sent immediately.</param>
        /// <param name="messageType">The type of message to send</param>
        /// <param name="endOfMessage">True if this is the last chunk in a message</param>
        void Send(byte[] message, WebSocketMessageType messageType, bool endOfMessage);

        string LocalIpAddress { get; }
        int LocalPort { get; }
        string RemoteIpAddress { get; }
        int RemotePort { get; }
        string Host { get; }
        string Path { get; }
        string PathBase { get; }
        string QueryString { get; }
        string Scheme { get; }
        string Method { get; }
        bool IsSecure { get; }
        string Url { get; }
        string Protocol { get; }
        IDictionary<string, string> Headers { get; }

#if NETSTANDARD
         Microsoft.AspNetCore.Http.IHeaderDictionary RawHeaders { get; }
#else
        IDictionary<string, string[]> RawHeaders { get; }
#endif
    }
}
