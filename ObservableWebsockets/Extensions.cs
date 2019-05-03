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
        public static string Decode(this ArraySegment<byte> bytes) => Decode(bytes, Encoding.UTF8);

        public static string Decode(this ArraySegment<byte> bytes, Encoding encoding) =>
            encoding.GetString(bytes.Array, bytes.Offset, bytes.Count);

        public static void SendText(this IObservableWebsocket ws, string text) =>
            SendText(ws, text, Encoding.UTF8);

        public static void SendText(this IObservableWebsocket ws, string text, Encoding encoding) =>
            ws.Send(encoding.GetBytes(text), WebSocketMessageType.Text, true);

        public static void SendBinary(this IObservableWebsocket ws, byte[] data) =>
            ws.Send(data, WebSocketMessageType.Binary, true);
    }
}
