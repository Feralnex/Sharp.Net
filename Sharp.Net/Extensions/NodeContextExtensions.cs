using Sharp.Collections;
using Sharp.Net.EndPoints;
using Sharp.Net.Sockets.Contexts;
using System.Threading.Tasks;

namespace Sharp.Net.Extensions
{
    public static class NodeContextExtensions
    {
        public static NodeContext Hydrate(
            this NodeContext entry,
            nint descriptor,
            ErrorCallback errorCallback,
            TaskCompletionSource<int> completionSource,
            CompletionCallback<NodeContext> completionCallback,
            TransferCallback resultCallback,
            byte[] buffer,
            int length,
            NativeList<byte> nativeBuffer,
            int flags,
            EndPoint endPoint)
        {
            entry.Hydrate(descriptor, errorCallback, completionSource, completionCallback, resultCallback, buffer, length, nativeBuffer, flags);
            entry.EndPoint = endPoint;
            entry.EndPointLength = endPoint.Size;

            return entry;
        }
    }
}
