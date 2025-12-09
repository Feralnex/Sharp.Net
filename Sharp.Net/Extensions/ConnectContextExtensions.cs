using Sharp.Net.EndPoints;
using Sharp.Net.Sockets.Contexts;
using System;
using System.Threading.Tasks;

namespace Sharp.Net.Extensions
{
    public static class ConnectContextExtensions
    {
        public static ConnectContext<TResultType, TSocketContext, TDelegate> Hydrate<TResultType, TSocketContext, TDelegate>(
            this ConnectContext<TResultType, TSocketContext, TDelegate> entry,
            nint descriptor,
            ErrorCallback errorCallback,
            TaskCompletionSource<TResultType> completionSource,
            CompletionCallback<TSocketContext> completionCallback,
            TDelegate resultCallback,
            EndPoint endPoint)
            where TSocketContext : ConnectContext<TResultType, TSocketContext, TDelegate>
            where TDelegate : Delegate
        {
            entry.Hydrate(descriptor, errorCallback, completionSource, completionCallback, resultCallback);
            entry.EndPoint = endPoint;
            entry.EndPointLength = endPoint.Size;

            return entry;
        }
    }
}
