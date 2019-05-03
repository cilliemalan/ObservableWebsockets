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
        /// <param name="configure">An action called to configure the observable websockets.</param>
        public static IAppBuilder UseObservableWebsockets(this IAppBuilder app, Action<ObservableWebsocketOptions> configure)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var options = new ObservableWebsocketOptions();
            configure(options);
            return UseObservableWebsockets(app, options);
        }

        /// <summary>
        /// Bind observable websockets to a pipeline.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="options">The observable websocket options</param>
        public static IAppBuilder UseObservableWebsockets(this IAppBuilder app, ObservableWebsocketOptions options)
        {
            var mw = new WebsocketMiddleware(options);
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
        /// <param name="configure">An action called to configure the observable websockets.</param>
        public static IApplicationBuilder UseObservableWebsockets(this IApplicationBuilder app, Action<ObservableWebsocketOptions> configure)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var options = new ObservableWebsocketOptions();
            configure(options);
            return UseObservableWebsockets(app, options);
        }

        /// <summary>
        /// Bind observable websockets to a pipeline.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="options">The observable websocket options</param>
        public static IApplicationBuilder UseObservableWebsockets(this IApplicationBuilder app, ObservableWebsocketOptions options)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (options.Acceptor == null)
            {
                throw new InvalidOperationException("The ObservableWebsocketOptions must have Acceptor set. For example, use app.UseObservableWebsockets(c => c.OnAccept(ws => { ... }))");
            }

            return app.Use(async (context, next) =>
            {
                if (context.WebSockets.IsWebSocketRequest && (options.ConnectionEvaluator?.Invoke(context) ?? true))
                {
                    var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                    await WebsocketConnector.HandleWebsocketAsync(new NetStandardRequestContext(context), webSocket, options);
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