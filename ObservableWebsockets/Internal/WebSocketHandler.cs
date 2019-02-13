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
        private Func<IObserver<(byte[] message, WebSocketMessageType messageType, bool endOfMessage)>, IDisposable> _subscribe;

        public string Path { get; }
        
        public WebSocketHandler(Action<byte[], WebSocketMessageType, bool> sender,
            string path,
            Func<IObserver<(byte[] message, WebSocketMessageType messageType, bool endOfMessage)>, IDisposable> subscribe)
        {
            _sender = sender;
            _subscribe = subscribe;
            Path = path;
        }

        public void Send(byte[] message, WebSocketMessageType messageType, bool endOfMessage) => _sender(message, messageType, endOfMessage);

#if HAS_VALUETUPLE
        public IDisposable Subscribe(IObserver<(byte[] message, WebSocketMessageType messageType, bool endOfMessage)> observer) =>
            _subscribe(observer);
#else
        public IDisposable Subscribe(IObserver<Tuple<byte[], WebSocketMessageType, bool>> observer) =>
            _subscribe(new MultiObserver(observer));

        private class MultiObserver : IObserver<(byte[] message, WebSocketMessageType messageType, bool endOfMessage)>
        {
            private IObserver<Tuple<byte[], WebSocketMessageType, bool>> _observer;

            public MultiObserver(IObserver<Tuple<byte[], WebSocketMessageType, bool>> observer)
            {
                _observer = observer;
            }

            public void OnCompleted() => _observer.OnCompleted();

            public void OnError(Exception error) => _observer.OnError(error);

            public void OnNext((byte[] message, WebSocketMessageType messageType, bool endOfMessage) value) =>
                _observer.OnNext(Tuple.Create(value.message, value.messageType, value.endOfMessage));
        }
#endif
    }
}
