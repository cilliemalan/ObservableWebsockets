﻿#if OWIN
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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

    internal class WebsocketMiddleware
    {
        private AppFunc _next;
        private readonly Action<IObservableWebsocket> _onAccept;

        public WebsocketMiddleware(Action<IObservableWebsocket> onAccept)
        {
            _onAccept = onAccept;
        }

        public void Initialize(AppFunc next)
        {
            _next = next;
        }

        public async Task Invoke(IDictionary<string, object> env)
        {
            var accept = env.TryGetValue("websocket.Accept", out var wsa) ? (WebSocketAccept)wsa : null;
            if (accept != null)
            {
                env.TryGetValue("owin.RequestPath", out var path);
                var wsProtocolOpt = env.TryGetValue("owin.RequestHeaders", out var headers) ?
                    ((IDictionary<string, string[]>)headers).TryGetValue("Sec-WebSocket-Protocol", out var secWsProtocol) ?
                        secWsProtocol?[0]?.Split(',')?.FirstOrDefault()?.Trim() : null : null;
                Dictionary<string, object> acceptOptions = new Dictionary<string, object>();
                if (wsProtocolOpt != null)
                {
                    acceptOptions["websocket.SubProtocol"] = wsProtocolOpt;
                }

                accept(acceptOptions, (ws) => AcceptSocketAsync(path as string ?? "/", ws));
                env["owin.ResponseStatusCode"] = (int)HttpStatusCode.SwitchingProtocols;
            }
            else
            {
                await _next(env);
            }
        }

        private async Task AcceptSocketAsync(string path, IDictionary<string, object> ws)
        {
            if (ws.TryGetValue("System.Net.WebSockets.WebSocketContext", out var _wsctx))
            {
                WebSocketContext wsctx = (WebSocketContext)_wsctx;
                await WebsocketConnector.HandleWebsocketAsync(path, wsctx.WebSocket, _onAccept);
            }
            else
            {
                var wrapper = new OwinWebsocketWrapper(ws);
                await WebsocketConnector.HandleWebsocketAsync(path, wrapper, _onAccept);
            }
        }
    }
}
#endif