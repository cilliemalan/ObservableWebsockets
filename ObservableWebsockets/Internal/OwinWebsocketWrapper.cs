using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ObservableWebsockets.Internal
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    using WebSocketAccept = Action<
                                    IDictionary<string, object>, // WebSocket Accept parameters
                                    Func< // WebSocketFunc callback
                                        IDictionary<string, object>, // WebSocket environment
                                        Task>>;
    using WebSocketCloseAsync = Func<
                                     int, // closeStatus
                                     string, // closeDescription
                                     CancellationToken, // cancel
                                     Task>;
    using WebSocketReceiveAsync = System.Func<
                   ArraySegment<byte>, // data
                   CancellationToken, // cancel
                   Task<Tuple<int, bool, int>>>;
    //System.Tuple< // WebSocketReceiveTuple
    //    int, // messageType
    //    bool, // endOfMessage
    //    int>>>; // count
    // closeStatusDescription

    using WebSocketSendAsync = System.Func<System.ArraySegment<byte>, // data
                                        int, // message type
                                        bool, // end of message
                                        CancellationToken, // cancel
                                        Task>;
    public class OwinWebsocketWrapper : WebSocket
    {
        private WebSocketSendAsync _wsSendAsync;
        private WebSocketCloseAsync _wsCloseAsync;
        private CancellationToken _wsCallCancelled;
        private WebSocketReceiveAsync _wsRecieveAsync;

        public OwinWebsocketWrapper(IDictionary<string, object> ws)
        {
            _wsSendAsync = (WebSocketSendAsync)ws["websocket.SendAsync"];
            _wsCloseAsync = (WebSocketCloseAsync)ws["websocket.CloseAsync"];
            _wsCallCancelled = (CancellationToken)ws["websocket.CallCancelled"];
            _wsRecieveAsync = (WebSocketReceiveAsync)ws["websocket.ReceiveAsync"];
        }

        public override WebSocketCloseStatus? CloseStatus => throw new NotImplementedException();

        public override string CloseStatusDescription => throw new NotImplementedException();

        public override string SubProtocol => throw new NotImplementedException();

        public override WebSocketState State => throw new NotImplementedException();

        public override void Abort()
        {
            throw new NotImplementedException();
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            return _wsCloseAsync((int)closeStatus, statusDescription, cancellationToken);
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            return _wsCloseAsync((int)closeStatus, statusDescription, cancellationToken);
        }

        public override void Dispose()
        {
        }

        public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            var r=await _wsRecieveAsync(buffer, cancellationToken);
            return new WebSocketReceiveResult(r.Item1, (WebSocketMessageType)r.Item3, r.Item2);
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            return _wsSendAsync(buffer, (int)messageType, endOfMessage, cancellationToken);
        }
    }
}
