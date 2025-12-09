using Sharp.Net.EndPoints;
using Sharp.Net.Sockets;
using Sharp.Net.Sockets.Contexts;
using System;

namespace Sharp.Net
{
    public delegate bool ListenerCallback(out Client? client, out Exception? exception);
    public delegate bool ClientCallback(byte[] buffer, int length, int flags, out int bytesTranferred, out Exception? exception);
    public delegate bool ReceiveFromCallback(byte[] buffer, int length, int flags, out EndPoint? remoteEndPoint, out int bytesTranferred, out Exception? exception);
    public delegate bool SendToCallback(byte[] buffer, int length, int flags, EndPoint remoteEndPoint, out int bytesTranferred, out Exception? exception);
    public delegate bool ShutdownCallback(Client client);
    public delegate void ConnectCallback(Client client, EndPoint remoteEndPoint);
    public delegate void AcceptCallback(Client.Listener listener, Client client);
    public delegate void TransferCallback(Socket socket, EndPoint remoteEndPoint, byte[] buffer, int bytesTranferred);
    public delegate void ErrorCallback(Socket socket, Exception exception);
    public delegate void CompletionCallback<TSocketContext>(TSocketContext socketContext) where TSocketContext : SocketContext;

    public unsafe delegate long GetInt64(byte* source, int index);
    public unsafe delegate void SetInt64(byte* source, int index, long value);
    public unsafe delegate ulong GetUInt64(byte* source, int index);
    public unsafe delegate void SetUInt64(byte* source, int index, ulong value);
}