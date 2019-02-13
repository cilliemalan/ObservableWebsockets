using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ObservableWebsockets
{
    public static class Extensions
    {
        public static string Decode(this ArraySegment<byte> bytes) =>
            Encoding.UTF8.GetString(bytes.Array, bytes.Offset, bytes.Count);

        public static Task SendTextAsync(this WebSocket ws, string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            return ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, default);
        }

        public static Task CloseAsync(this WebSocket ws)
        {
            return ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, default);
        }

        public static async Task<string> ReceiveAsync(this WebSocket ws)
        {
            ArraySegment<byte> buff = new ArraySegment<byte>(new byte[1024]);
            var r = await ws.ReceiveAsync(buff, new CancellationTokenSource(5000).Token);
            if (r.MessageType != WebSocketMessageType.Text) throw new InvalidOperationException("Expected a string message");
            return Encoding.UTF8.GetString(buff.Array, 0, r.Count);
        }
    }
}
