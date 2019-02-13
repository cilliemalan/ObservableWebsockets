using ObservableWebsockets;
using ObservableWebsockets.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if OWIN
namespace Owin
{
    public static class ObservableWebsocketExtensions
    {
        /// <summary>
        /// Bind observable websockets to a pipeline.
        /// </summary>
        /// <param name="app">The <see cref="IAppBuilder"/>.</param>
        /// <param name="onAccept">A method that will be invoked when a websocket connection is established.</param>
        public static IAppBuilder UseObservableWebsockets(this IAppBuilder app, Action<IObservableWebsocket> onAccept)
        {
            var mw = new WebsocketMiddleware(onAccept);
            return app.Use(mw);
        }
    }
}
#else
namespace Microsoft.AspNetCore.Builder
{
    public static class ObservableWebsocketExtensions
    {
        /// <summary>
        /// Bind observable websockets to a pipeline.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="onAccept">A method that will be invoked when a websocket connection is established.</param>
        public static IApplicationBuilder UseObservableWebsockets(this IApplicationBuilder app, Action<IObservableWebsocket> onAccept)
        {
            return app.Use(async (context, next) =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                    await WebsocketConnector.HandleWebsocketAsync(context.Request.Path, webSocket, onAccept);
                }
                else
                {
                    await next();
                }
            });
        }
    }
}
#endif