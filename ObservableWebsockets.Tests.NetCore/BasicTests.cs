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
            WebHost.StartWith($"http://{host}", app => app.UseWebSockets().UseObservableWebsockets(c=> c.OnAccept(onAccept)));
        private static IDisposable BuildAppAsync(string host, Action<ObservableWebsocketOptions> configure, Action<IObservableWebsocket> onAccept) =>
            WebHost.StartWith($"http://{host}", app => app.UseWebSockets().UseObservableWebsockets(c => { configure(c); c.OnAccept(onAccept); }));
#elif OWIN
        private static string GetHost() => $"localhost:{32000 + Interlocked.Increment(ref r)}";
        private static IDisposable BuildAppAsync(string host, Action<IObservableWebsocket> onAccept) =>
            WebApp.Start($"http://{host}", app => app.UseObservableWebsockets(c => c.OnAccept(onAccept)));
        private static IDisposable BuildAppAsync(string host, Action<ObservableWebsocketOptions> configure, Action<IObservableWebsocket> onAccept) =>
            WebApp.Start($"http://{host}", app => app.UseObservableWebsockets(c => { configure(c); c.OnAccept(onAccept); }));
#endif

        [Fact(DisplayName = "Messages sent from the client are observed on the server")]
        public async Task BasicMessageTest()
        {
            var host = GetHost();
            var connectionReceived = Trigger.Create<string>("Never connected");
            var messageReceived = Trigger.Create<string>("Message never received");

            using (BuildAppAsync(host, ws =>
            {
                connectionReceived.Mark(ws.Path);
                ws.Take(1).Subscribe(m => messageReceived.Mark(m.message.Decode()));
            }))
            {
                var ws = new ClientWebSocket();
                await ws.ConnectAsync(new Uri($"ws://{host}/thepath"), default);

                await ws.SendTextAsync("Hello World");
                Assert.Equal("/thepath", await connectionReceived);
                Assert.Equal("Hello World", await messageReceived);

                await ws.CloseAsync();
            }
        }

        [Fact(DisplayName = "The close message is observed on the server")]
        public async Task CloseMessageTest()
        {
            var host = GetHost();
            var connectionReceived = Trigger.Create<string>("Never connected");
            var messageReceived = Trigger.Create<string>("Message never received");
            var closeMessageReceived = Trigger.Create("Close message never received");
            var numMessagesReceived = Trigger.Create<int>("never got number of messages");

            using (BuildAppAsync(host, ws =>
            {
                connectionReceived.Mark(ws.Path);
                ws.Take(1).Subscribe(m => messageReceived.Mark(m.message.Decode()));
                ws.Where(x => x.messageType == WebSocketMessageType.Close).Subscribe(m => closeMessageReceived.Mark());
                ws.Count().Subscribe(numMessagesReceived.Mark);
            }))
            {
                var ws = new ClientWebSocket();
                await ws.ConnectAsync(new Uri($"ws://{host}/thepath"), default);

                await ws.SendTextAsync("Hello World");
                Assert.Equal("/thepath", await connectionReceived);
                Assert.Equal("Hello World", await messageReceived);

                await ws.CloseAsync();
                Assert.Equal(2, await numMessagesReceived);
            }
        }

        [Fact(DisplayName = "OnComplete is observed on the server after client close")]
        public async Task OnCompleteTest()
        {
            var host = GetHost();
            var connectionReceived = Trigger.Create<string>("Never connected");
            var completedReceived = Trigger.Create("Complete never received");

            using (BuildAppAsync(host, ws =>
            {
                connectionReceived.Mark(ws.Path);
                ws.Subscribe(_ => { }, completedReceived.Mark);
            }))
            {
                var ws = new ClientWebSocket();
                await ws.ConnectAsync(new Uri($"ws://{host}/thepath"), default);

                await ws.SendTextAsync("Hello World");
                Assert.Equal("/thepath", await connectionReceived);

                await ws.CloseAsync();
                await completedReceived;
            }
        }

        [Fact(DisplayName = "Two messages are received - message and close.")]
        public async Task NumMessageTest()
        {
            var host = GetHost();
            var connectionReceived = Trigger.Create<string>("Never connected");
            var completedReceived = Trigger.Create("Complete never received");
            var numMessagesReceived = Trigger.Create<int>("never got number of messages");

            using (BuildAppAsync(host, ws =>
            {
                connectionReceived.Mark(ws.Path);
                ws.Subscribe(_ => { }, completedReceived.Mark);
                ws.Count().Subscribe(numMessagesReceived.Mark);
            }))
            {
                var ws = new ClientWebSocket();
                await ws.ConnectAsync(new Uri($"ws://{host}/thepath"), default);

                await ws.SendTextAsync("Hello World");
                Assert.Equal("/thepath", await connectionReceived);

                await ws.CloseAsync();
                await completedReceived;
                Assert.Equal(2, await numMessagesReceived);
            }
        }

        [Fact(DisplayName = "When no messages are sent, the server still receives the close message")]
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

        [Fact(DisplayName = "The server can send a message to the client immediately")]
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

        [Fact(DisplayName = "The server connection completes normaly on client error")]
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

                        await ws.CloseAsync(WebSocketCloseStatus.Empty, "this will cause an exception", default);
                    }
                }
                catch { }

                await completedReceived;
                Assert.Equal(0, await numMessagesReceived);
            }
        }

        [Fact(DisplayName = "The connection closes normally on observer error")]
        public async Task ServerFailureCloseTest()
        {
            var host = GetHost();
            var connectionReceived = Trigger.Create<string>("Never connected");

            using (BuildAppAsync(host, ws =>
            {
                connectionReceived.Mark(ws.Path);
                ws.Send(Encoding.UTF8.GetBytes("From Server"), WebSocketMessageType.Text, true);
                ws.Subscribe(z => throw new Exception());
            }))
            {
                using (var ws = new ClientWebSocket())
                {
                    await ws.ConnectAsync(new Uri($"ws://{host}/thepath"), default);

                    var msg = await ws.ReceiveAsync();
                    Assert.Equal("From Server", msg);

                    await ws.SendTextAsync("testing 123");
                    await ws.ReceiveCloseAsync();
                }
            }
        }

        [Fact(DisplayName = "The server produces observable error on observer error")]
        public async Task ObservableErrorTest()
        {
            var host = GetHost();
            var connectionReceived = Trigger.Create<string>("Never connected");
            var errorReceived = Trigger.Create<Exception>("never got an exception");

            using (BuildAppAsync(host, ws =>
            {
                connectionReceived.Mark(ws.Path);
                ws.Send(Encoding.UTF8.GetBytes("From Server"), WebSocketMessageType.Text, true);
                ws.Subscribe(z => throw new Exception("server error"), errorReceived.Mark, () => { });
            }))
            {
                using (var ws = new ClientWebSocket())
                {
                    await ws.ConnectAsync(new Uri($"ws://{host}/thepath"), default);

                    var msg = await ws.ReceiveAsync();
                    Assert.Equal("From Server", msg);

                    await ws.SendTextAsync("testing 123");
                    await ws.ReceiveCloseAsync();
                }

                Assert.Equal("server error", (await errorReceived).Message);
            }
        }

        [Fact(DisplayName = "Messages sent from the client are observed on the server when a path filter is set")]
        public async Task BasicFilterTest()
        {
            var host = GetHost();
            var connectionReceived = Trigger.Create<string>("Never connected");
            var messageReceived = Trigger.Create<string>("Message never received");

            using (BuildAppAsync(host, c => c.FilterPath("/thepath"), ws =>
            {
                connectionReceived.Mark(ws.Path);
                ws.Take(1).Subscribe(m => messageReceived.Mark(m.message.Decode()));
            }))
            {
                var ws = new ClientWebSocket();
                await ws.ConnectAsync(new Uri($"ws://{host}/thepath"), default);

                await ws.SendTextAsync("Hello World");
                Assert.Equal("/thepath", await connectionReceived);
                Assert.Equal("Hello World", await messageReceived);

                await ws.CloseAsync();
            }
        }

        [Fact(DisplayName = "Connections fail when path filter is set and connecting incorrectly")]
        public async Task FilterErrorTest()
        {
            var host = GetHost();

            using (BuildAppAsync(host, c => c.FilterPath("/thecorrectpath"), ws =>
            {
            }))
            {
                var ws = new ClientWebSocket();
                await Assert.ThrowsAnyAsync<WebSocketException>(() => ws.ConnectAsync(new Uri($"ws://{host}/theincorrectpath"), default));
            }
        }
    }
}
