using Sharp.Net.Sockets.Contexts;
using System;
using System.Threading.Tasks;

namespace Sharp.Net.Extensions
{
    public static class SocketContextExtensions
    {
        public static SocketContext Hydrate(
            this SocketContext entry,
            nint descriptor,
            ErrorCallback errorCallback)
        {
            entry.Descriptor = descriptor;
            entry.ErrorCallback = errorCallback;

            return entry;
        }

        public static SocketContext<TResultType, TSocketContext, TDelegate> Hydrate<TResultType, TSocketContext, TDelegate>(
            this SocketContext<TResultType, TSocketContext, TDelegate> entry,
            nint descriptor,
            ErrorCallback errorCallback,
            TaskCompletionSource<TResultType> completionSource,
            CompletionCallback<TSocketContext> completionCallback,
            TDelegate resultCallback)
            where TSocketContext : SocketContext<TResultType, TSocketContext, TDelegate>
            where TDelegate : Delegate
        {
            entry.Hydrate(descriptor, errorCallback);
            entry.CompletionSource = completionSource;
            entry.CompletionCallback = completionCallback;
            entry.ResultCallback = resultCallback;

            return entry;
        }
    }
}
