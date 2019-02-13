#if NETSTANDARD
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.WebSockets;
#elif OWIN
using Owin;
using Microsoft.Owin;
using Microsoft.Owin.Host;
using Microsoft.Owin.Hosting;
#endif
using ObservableWebsockets.Tests.Common;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ObservableWebsockets.Tests.NetCore
{
    public class BasicTests
    {
        private static int r = 0;
        
#if NETSTANDARD
        private static string GetHost() => $"localhost:{22000 + Interlocked.Increment(ref r)}";
        private static IDisposable BuildAppAsync(string host, Action<IObservableWebsocket> onAccept) =>
            WebHost.StartWith($"http://{host}", app => app.UseWebSockets().UseObservableWebsockets(onAccept));
#elif OWIN
        private static string GetHost() => $"localhost:{32000 + Interlocked.Increment(ref r)}";
        private static IDisposable BuildAppAsync(string host, Action<IObservableWebsocket> onAccept) =>
            WebApp.Start($"http://{host}", app => app.UseObservableWebsockets(onAccept));
#endif

        [Fact]
        public async Task BasicMessageTest()
        {
            var host = GetHost();
            var connectionReceived = Trigger.Create<string>("Never connected");
            var messageReceived = Trigger.Create<string>("Message never received");
            var closeMessageReceived = Trigger.Create("Close message never received");
            var completedReceived = Trigger.Create("Complete never received");
            var numMessagesReceived = Trigger.Create<int>("never got number of messages");

            using (BuildAppAsync(host, ws =>
            {
                connectionReceived.Mark(ws.Path);
                ws.Take(1).Subscribe(m => messageReceived.Mark(m.message.Decode()));
                ws.Where(x => x.messageType == WebSocketMessageType.Close).Subscribe(m => closeMessageReceived.Mark());
                ws.Subscribe(_ => { }, completedReceived.Mark);
                ws.Count().Subscribe(numMessagesReceived.Mark);
            }))
            {
                var ws = new ClientWebSocket();
                await ws.ConnectAsync(new Uri($"ws://{host}/thepath"), default);

                await ws.SendTextAsync("Hello World");
                Assert.Equal("/thepath", await connectionReceived);
                Assert.Equal("Hello World", await messageReceived);

                await ws.CloseAsync();
                await completedReceived;
                Assert.Equal(2, await numMessagesReceived);
            }
        }

        [Fact]
        public async Task OpenCloseTest()
        {
            var host = GetHost();
            var connectionReceived = Trigger.Create<string>("Never connected");
            var closeMessageReceived = Trigger.Create("Close message never received");
            var completedReceived = Trigger.Create("Complete never received");
            var numMessagesReceived = Trigger.Create<int>("never got number of messages");

            using (BuildAppAsync(host, ws =>
            {
                connectionReceived.Mark(ws.Path);
                ws.Where(x => x.messageType == WebSocketMessageType.Close).Subscribe(m => closeMessageReceived.Mark());
                ws.Subscribe(_ => { }, completedReceived.Mark);
                ws.Count().Subscribe(numMessagesReceived.Mark);
            }))
            {
                var ws = new ClientWebSocket();
                await ws.ConnectAsync(new Uri($"ws://{host}/thepath"), default);

                await ws.CloseAsync();
                await completedReceived;
                Assert.Equal(1, await numMessagesReceived);
            }
        }

        [Fact]
        public async Task ServerMessageTest()
        {
            var host = GetHost();
            var connectionReceived = Trigger.Create<string>("Never connected");
            var closeMessageReceived = Trigger.Create("Close message never received");
            var completedReceived = Trigger.Create("Complete never received");
            var numMessagesReceived = Trigger.Create<int>("never got number of messages");

            using (BuildAppAsync(host, ws =>
            {
                connectionReceived.Mark(ws.Path);
                ws.Send(Encoding.UTF8.GetBytes("From Server"), WebSocketMessageType.Text, true);
                ws.Where(x => x.messageType == WebSocketMessageType.Close).Subscribe(m => closeMessageReceived.Mark());
                ws.Subscribe(_ => { }, completedReceived.Mark);
                ws.Count().Subscribe(numMessagesReceived.Mark);
            }))
            {
                var ws = new ClientWebSocket();
                await ws.ConnectAsync(new Uri($"ws://{host}/thepath"), default);

                var msg = await ws.ReceiveAsync();
                Assert.Equal("From Server", msg);

                await ws.CloseAsync();
                await completedReceived;
                Assert.Equal(1, await numMessagesReceived);
            }
        }

        [Fact]
        public async Task ClientFailureTest()
        {
            var host = GetHost();
            var connectionReceived = Trigger.Create<string>("Never connected");
            var closeMessageReceived = Trigger.Create("Close message never received");
            var completedReceived = Trigger.Create("Complete never received");
            var numMessagesReceived = Trigger.Create<int>("never got number of messages");

            using (BuildAppAsync(host, ws =>
            {
                connectionReceived.Mark(ws.Path);
                ws.Send(Encoding.UTF8.GetBytes("From Server"), WebSocketMessageType.Text, true);
                ws.Where(x => x.messageType == WebSocketMessageType.Close).Subscribe(m => closeMessageReceived.Mark());
                ws.Subscribe(_ => { }, completedReceived.Mark);
                ws.Count().Subscribe(numMessagesReceived.Mark);
            }))
            {
                try
                {
                    using (var ws = new ClientWebSocket())
                    {
                        await ws.ConnectAsync(new Uri($"ws://{host}/thepath"), default);

                        var msg = await ws.ReceiveAsync();
                        Assert.Equal("From Server", msg);

                        await ws.CloseAsync(WebSocketCloseStatus.Empty, "this will cause InvalidOperationException", default);
                    }
                }
                catch { }

                await completedReceived;
                Assert.Equal(0, await numMessagesReceived);
            }
        }
    }
}
