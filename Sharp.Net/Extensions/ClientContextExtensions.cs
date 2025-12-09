using Sharp.Collections;
using Sharp.Net.Sockets.Contexts;
using System;
using System.Threading.Tasks;

namespace Sharp.Net.Extensions
{
    public static class ClientContextExtensions
    {
        public static ClientContext<TResultType, TSocketContext, TDelegate> Hydrate<TResultType, TSocketContext, TDelegate>(
            this ClientContext<TResultType, TSocketContext, TDelegate> entry,
            nint descriptor,
            ErrorCallback errorCallback,
            TaskCompletionSource<TResultType> completionSource,
            CompletionCallback<TSocketContext> completionCallback,
            TDelegate resultCallback,
            byte[] buffer,
            int length,
            NativeList<byte> nativeBuffer,
            int flags)
            where TSocketContext : ClientContext<TResultType, TSocketContext, TDelegate>
            where TDelegate : Delegate
        {
            entry.Hydrate(descriptor, errorCallback, completionSource, completionCallback, resultCallback);
            entry.Buffer = buffer;
            entry.Length = (ulong)length;
            entry.NativeBuffer = nativeBuffer;
            entry.Flags = flags;

            return entry;
        }
    }
}
