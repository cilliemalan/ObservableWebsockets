using System;
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
    public interface IObservableWebsocket : IObservable<string>
    {
        /// <summary>
        /// The path on which the websocket connection was established.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Send a message out of the websocket. Sent messages are queued on the websocket thread and are not guaranteed to be delivered
        /// as the websocket may be disconnected before a message is dequeued.
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="messageType">The type of message to send</param>
        /// <param name="endOfMessage">True if this is the last chunk in a message</param>
        void Send(byte[] message, WebSocketMessageType messageType, bool endOfMessage);
    }
}
