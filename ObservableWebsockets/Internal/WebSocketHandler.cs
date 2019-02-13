using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace ObservableWebsockets.Internal
{
    internal class WebSocketHandler : IObservableWebsocket
    {
        private Action<byte[], WebSocketMessageType, bool> _sender;
        private Func<IObserver<string>, IDisposable> _subscribe;

        public WebSocketHandler(Action<byte[], WebSocketMessageType, bool> sender, string path, Func<IObserver<string>, IDisposable> subscribe)
        {
            _sender = sender;
            _subscribe = subscribe;
            Path = path;
        }

        public void Send(byte[] message, WebSocketMessageType messageType, bool endOfMessage) => _sender(message, messageType, endOfMessage);

        public IDisposable Subscribe(IObserver<string> observer) => _subscribe(observer);

        public string Path { get; }
    }
}
