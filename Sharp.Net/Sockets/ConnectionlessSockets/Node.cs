using Sharp.Collections;
using Sharp.Exceptions;
using Sharp.Helpers;
using Sharp.Net.Configurations;
using Sharp.Net.EndPoints;
using Sharp.Net.Extensions;
using Sharp.Net.Sockets.Contexts;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Sharp.Net.Sockets
{
    public class Node : Socket
    {
        #region Platform specific information

        private static unsafe delegate* unmanaged[Cdecl]<nint, byte*, uint, int, nint, int, long*, int*, bool> NativeTryReceiveFrom { get; }
        private static unsafe delegate* unmanaged[Cdecl]<nint, nint, void> NativeBeginReceiveFrom { get; }
        private static unsafe delegate* unmanaged[Cdecl]<nint, byte*, uint, int, nint, int, long*, int*, bool> NativeTrySendTo { get; }
        private static unsafe delegate* unmanaged[Cdecl]<nint, nint, void> NativeBeginSendTo { get; }

        #endregion Platform specific information

        protected static IPool<NodeContext> TransferPool { get; }

        protected Node(nint descriptor, Configuration configuration) : base(descriptor, configuration) { }

        static unsafe Node()
        {
            nint tryReceiveFromPointer = Library.GetExport(nameof(Net), nameof(TryReceiveFrom));
            nint beginReceiveFromPointer = Library.GetExport(nameof(Net), nameof(BeginReceiveFrom));
            nint trySendToPointer = Library.GetExport(nameof(Net), nameof(TrySendTo));
            nint beginSendToPointer = Library.GetExport(nameof(Net), nameof(BeginSendTo));

            NativeTryReceiveFrom = (delegate* unmanaged[Cdecl]<nint, byte*, uint, int, nint, int, long*, int*, bool>)tryReceiveFromPointer;
            NativeBeginReceiveFrom = (delegate* unmanaged[Cdecl]<nint, nint, void>)beginReceiveFromPointer;
            NativeTrySendTo = (delegate* unmanaged[Cdecl]<nint, byte*, uint, int, nint, int, long*, int*, bool>)trySendToPointer;
            NativeBeginSendTo = (delegate* unmanaged[Cdecl]<nint, nint, void>)beginSendToPointer;

            TransferPool = Pools.GetOrAdd(TrySelectThreadSafePool, OnPoolMissing<NodeContext>);
        }

        public static unsafe bool TryCreate(Configuration configuration, out Node? node, out Exception? exception)
        {
            bool success = TryCreate(configuration, out nint descriptor, out exception);

            node = success
                ? new Node(descriptor, configuration)
                : default;

            return success;
        }

        public unsafe bool TryReceiveFrom(ref byte buffer, int flags, out EndPoint? remoteEndPoint, out Exception? exception)
        {
            remoteEndPoint = Configuration.AllocateEndPoint();

            byte data = default;
            long bytesReceived = default;
            int endPointSize = remoteEndPoint.Size;
            int errorCode = default;
            bool success = NativeTryReceiveFrom(Descriptor, &data, 1, flags, remoteEndPoint, endPointSize, &bytesReceived, &errorCode);

            if (success)
            {
                buffer = data;
                exception = default;
            }
            else
            {
                remoteEndPoint = default;
                exception = PlatformException.FromCode(errorCode);
            }

            return success;
        }

        public unsafe bool TryReceiveFrom(byte[] buffer, int length, int flags, out EndPoint? remoteEndPoint, out int bytesReceived, out Exception? exception)
        {
            bytesReceived = default;

            if (buffer is null)
            {
                remoteEndPoint = default;
                exception = new ArgumentNullException(nameof(buffer));

                return false;
            }

            remoteEndPoint = Configuration.AllocateEndPoint();

            nint nativeBuffer = AcquireNativeBuffer(length);
            int endPointSize = remoteEndPoint.Size;
            long bytesCount = default;
            int errorCode = default;
            bool success = NativeTryReceiveFrom(Descriptor, (byte*)nativeBuffer, (uint)length, flags, remoteEndPoint, endPointSize, &bytesCount, &errorCode);

            if (success)
            {
                Marshal.Copy(nativeBuffer, buffer, 0, length);
                bytesReceived = (int)bytesCount;
                exception = default;
            }
            else
            {
                remoteEndPoint = default;
                exception = PlatformException.FromCode(errorCode);
            }

            ReleaseNativeBuffer(nativeBuffer);

            return success;
        }

        public unsafe Task<int> BeginReceiveFrom(byte[] buffer, int length, int flags, TransferCallback transferCallback, ErrorCallback errorCallback)
        {
            if (!Bound && !TryBind(out Exception? exception))
            {
                errorCallback?.Invoke(this, exception!);

                return Task.FromResult(-1);
            }

            TaskCompletionSource<int> completionSource = new TaskCompletionSource<int>();
            NodeContext context = TransferPool.Acquire(OnPoolEmpty<NodeContext>);
            NativeList<byte> nativeBuffer = AcquireNativeBuffer(length);
            EndPoint endPoint = Configuration.AllocateEndPoint();

            context!.Hydrate(Descriptor, errorCallback, completionSource, OnBeginTransferCompletion, transferCallback, buffer, length, nativeBuffer, flags, endPoint);

            NativeBeginReceiveFrom(IOHandle.Handle, context);

            return completionSource.Task;
        }

        public unsafe bool TrySendTo(byte buffer, int flags, EndPoint remoteEndPoint, out Exception? exception)
        {
            if (remoteEndPoint is null)
            {
                exception = new ArgumentNullException(nameof(remoteEndPoint));

                return false;
            }

            if (!Bound && !TryBind(out exception))
                return false;

            long bytesCount = default;
            int errorCode = default;
            bool success = NativeTrySendTo(Descriptor, &buffer, 1, flags, remoteEndPoint, remoteEndPoint.Size, &bytesCount, &errorCode);

            if (success)
                exception = default;
            else
                exception = PlatformException.FromCode(errorCode);

            return success;
        }

        public unsafe bool TrySendTo(byte[] buffer, int length, int flags, EndPoint remoteEndPoint, out int bytesSent, out Exception? exception)
        {
            bytesSent = default;

            if (buffer is null)
            {
                exception = new ArgumentNullException(nameof(buffer));

                return false;
            }
            if (remoteEndPoint is null)
            {
                exception = new ArgumentNullException(nameof(remoteEndPoint));

                return false;
            }

            if (!Bound && !TryBind(out exception))
                return false;

            nint nativeBuffer = AcquireNativeBuffer(length);
            long bytesCount = default;
            int errorCode = default;
            bool success = NativeTrySendTo(Descriptor, (byte*)nativeBuffer, (uint)length, flags, remoteEndPoint, remoteEndPoint.Size, &bytesCount, &errorCode);

            if (success)
            {
                Marshal.Copy(nativeBuffer, buffer, 0, length);
                bytesSent = (int)bytesCount;
                exception = default;
            }
            else
            {
                exception = PlatformException.FromCode(errorCode);
            }

            ReleaseNativeBuffer(nativeBuffer);

            return success;
        }

        public unsafe Task<int> BeginSendTo(byte[] buffer, int length, int flags, EndPoint remoteEndPoint, TransferCallback transferCallback, ErrorCallback errorCallback)
        {
            if (!Bound && !TryBind(out Exception? exception))
            {
                errorCallback?.Invoke(this, exception!);

                return Task.FromResult(-1);
            }

            TaskCompletionSource<int> completionSource = new TaskCompletionSource<int>();
            NodeContext context = TransferPool.Acquire(OnPoolEmpty<NodeContext>);
            NativeList<byte> nativeBuffer = AcquireNativeBuffer(length);

            context!.Hydrate(Descriptor, errorCallback, completionSource, OnBeginTransferCompletion, transferCallback, buffer, length, nativeBuffer, flags, remoteEndPoint);

            NativeBeginSendTo(IOHandle.Handle, context);

            return completionSource.Task;
        }

        private void OnBeginTransferCompletion(NodeContext context)
        {
            if (context.CompletedSuccessfully)
            {
                Marshal.Copy(context.NativeBuffer, context.Buffer!, 0, (int)context.BytesTransferred);

                ReleaseNativeBuffer(context.NativeBuffer);

                context.ResultCallback?.Invoke(this, context.EndPoint, context.Buffer!, (int)context.BytesTransferred);
                context.CompletionSource!.SetResult((int)context.BytesTransferred);
            }
            else
            {
                PlatformException exception = PlatformException.FromCode(context.ErrorCode);

                context.ErrorCallback?.Invoke(this, exception);
                context.CompletionSource!.SetResult(-1);
            }
        }
    }
}
