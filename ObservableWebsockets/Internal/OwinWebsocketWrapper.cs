using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ObservableWebsockets.Internal
{
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
        private WebSocketState _stateGuess = WebSocketState.Open;
        private WebSocketCloseStatus? _closeStatus;
        private string _closeStatusDescription;

        public OwinWebsocketWrapper(IDictionary<string, object> ws)
        {
            _wsSendAsync = (WebSocketSendAsync)ws["websocket.SendAsync"];
            _wsCloseAsync = (WebSocketCloseAsync)ws["websocket.CloseAsync"];
            _wsCallCancelled = (CancellationToken)ws["websocket.CallCancelled"];
            _wsRecieveAsync = (WebSocketReceiveAsync)ws["websocket.ReceiveAsync"];
            ws.TryGetValue("websocket.SubProtocol", out var sp);
            SubProtocol = sp as string;
        }

        public override WebSocketCloseStatus? CloseStatus => _closeStatus;

        public override string CloseStatusDescription => _closeStatusDescription;

        public override string SubProtocol { get; }

        public override WebSocketState State => _stateGuess;

        public override void Abort() => throw new NotImplementedException();

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public override async Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            try
            {
                await _wsCloseAsync(MapCloseStatus(closeStatus), statusDescription, cancellationToken);
                _closeStatus = closeStatus;
                _closeStatusDescription = statusDescription;
            }
            finally
            {
                _stateGuess = WebSocketState.Closed;
            }
        }

        public override void Dispose()
        {
        }

        public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            try
            {
                var r = await _wsRecieveAsync(buffer, cancellationToken);
                WebSocketMessageType wsmt = MapWebSocketMessageType(r.Item1);
                if (wsmt == WebSocketMessageType.Close)
                {
                    _stateGuess = WebSocketState.CloseReceived;
                }

                return new WebSocketReceiveResult(r.Item3, wsmt, r.Item2);
            }
            catch
            {
                _stateGuess = WebSocketState.Aborted;
                throw;
            }
        }

        public override async Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            try
            {
                await _wsSendAsync(buffer, MapWebSocketMessageType(messageType), endOfMessage, cancellationToken);
            }
            catch
            {
                _stateGuess = WebSocketState.Aborted;
                throw;
            }
        }

        private static int MapWebSocketMessageType(WebSocketMessageType t)
        {
            switch (t)
            {
                case WebSocketMessageType.Text: return 1;
                case WebSocketMessageType.Binary: return 2;
                case WebSocketMessageType.Close: return 3;
                default: return default;
            }
        }

        private static WebSocketMessageType MapWebSocketMessageType(int r)
        {
            switch (r)
            {
                case 1: return WebSocketMessageType.Text;
                case 2: return WebSocketMessageType.Binary;
                case 3: return WebSocketMessageType.Close;
                default: return default;
            }
        }

        private static int MapCloseStatus(WebSocketCloseStatus closeStatus) => (int)closeStatus;
    }
}
