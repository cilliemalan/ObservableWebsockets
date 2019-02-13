using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObservableWebsockets
{
    public class ObservableWebsocketOptions
    {
        /// <summary>
        /// Called when a websocket client tries to connect. if it returns
        /// true, the connection is established. If it returns false,
        /// the request will continue in the pipeline.
        /// </summary>
#if OWIN
        public Func<IDictionary<string, object>, bool> ConnectionEvaluator { get; set; }

        private static bool MatchPath(IDictionary<string, object> ctx, string path) =>
            (string)ctx["owin.RequestPath"] == path;
#else
        public Func<Microsoft.AspNetCore.Http.HttpContext, bool> ConnectionEvaluator { get; set; }
        
        private static bool MatchPath(Microsoft.AspNetCore.Http.HttpContext ctx, string path) =>
            ctx.Request.Path.Value == path;
#endif

        /// <summary>
        /// Called when a websocket client connects successfully. Exposes
        /// the observable websocket interface.
        /// </summary>
        public Action<IObservableWebsocket> Acceptor { get; set; }

#if OWIN
        /// <summary>
        /// Forces using the OWIN Websocket interface. By default
        /// it will try to use <see cref="System.Net.WebSockets.WebSocket"/>
        /// if it is exposed by OWIN.
        /// </summary>
        public bool ForceOwinWebsockets { get; set; }
#endif

        /// <summary>
        /// Sets the <see cref="Acceptor"/>
        /// </summary>
        /// <param name="onAccept">
        /// Called when a websocket client connects successfully. Exposes
        /// the observable websocket interface.
        /// </param>
        public ObservableWebsocketOptions OnAccept(Action<IObservableWebsocket> onAccept)
        {
            Acceptor = onAccept;
            return this;
        }

        /// <summary>
        /// Causes the observable websocket to only accept websocket requests
        /// on the given path. This method will chain previously set filters
        /// </summary>
        /// <param name="path">The path to accept websocket connections on.</param>
        public ObservableWebsocketOptions FilterPath(string path)
        {
            if (ConnectionEvaluator != null)
            {
                ConnectionEvaluator = ctx => ConnectionEvaluator(ctx) && MatchPath(ctx, path);
            }
            else
            {
                ConnectionEvaluator = ctx => MatchPath(ctx, path);
            }

            return this;
        }
    }
}
