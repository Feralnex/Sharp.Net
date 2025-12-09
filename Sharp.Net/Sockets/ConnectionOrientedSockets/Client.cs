using Sharp.Collections;
using Sharp.Exceptions;
using Sharp.Helpers;
using Sharp.Net.Configurations;
using Sharp.Net.EndPoints;
using Sharp.Net.Exceptions;
using Sharp.Net.Extensions;
using Sharp.Net.Sockets.Contexts;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Sharp.Net.Sockets
{
    public partial class Client : Socket
    {
        #region Platform specific information

        private static unsafe delegate* unmanaged[Cdecl]<nint, nint, int, int*, bool> NativeTryConnect { get; }
        private static unsafe delegate* unmanaged[Cdecl]<nint, nint, nint, void> NativeBeginConnect { get; }
        private static unsafe delegate* unmanaged[Cdecl]<nint, int*, bool> NativeTryShutdown { get; }
        private static unsafe delegate* unmanaged[Cdecl]<nint, nint, nint, void> NativeBeginShutdown { get; }
        private static unsafe delegate* unmanaged[Cdecl]<nint, byte*, uint, int, long*, int*, bool> NativeTryReceive { get; }
        private static unsafe delegate* unmanaged[Cdecl]<nint, nint, void> NativeBeginReceive { get; }
        private static unsafe delegate* unmanaged[Cdecl]<nint, byte*, uint, int, long*, int*, bool> NativeTrySend { get; }
        private static unsafe delegate* unmanaged[Cdecl]<nint, nint, void> NativeBeginSend { get; }

        #endregion Platform specific information

        protected static IPool<ConnectContext> ConnectPool { get; }
        protected static IPool<ShutdownContext> ShutdownPool { get; }
        protected static IPool<ClientContext> TransferPool { get; }

        public bool Connected => RemoteEndPoint.HasSome;

        protected Reference<EndPoint> RemoteEndPoint { get; }

        protected Client(nint descriptor, Configuration configuration) : base(descriptor, configuration)
        {
            RemoteEndPoint = new Reference<EndPoint>();
        }

        protected Client(nint descriptor, Configuration configuration, EndPoint localEndPoint) : base(descriptor, configuration, localEndPoint)
        {
            RemoteEndPoint = new Reference<EndPoint>();
        }

        protected Client(nint descriptor, Configuration configuration, EndPoint localEndPoint, EndPoint remoteEndPoint) : this(descriptor, configuration, localEndPoint)
        {
            RemoteEndPoint.Set(remoteEndPoint);
        }

        static unsafe Client()
        {
            nint tryConnectPointer = Library.GetExport(nameof(Net), nameof(TryConnect));
            nint beginConnectPointer = Library.GetExport(nameof(Net), nameof(BeginConnect));
            nint tryShutdownPointer = Library.GetExport(nameof(Net), nameof(TryShutdown));
            nint beginShutdownPointer = Library.GetExport(nameof(Net), nameof(BeginShutdown));
            nint tryReceivePointer = Library.GetExport(nameof(Net), nameof(TryReceive));
            nint beginReceivePointer = Library.GetExport(nameof(Net), nameof(BeginReceive));
            nint trySendPointer = Library.GetExport(nameof(Net), nameof(TrySend));
            nint beginSendPointer = Library.GetExport(nameof(Net), nameof(BeginSend));

            NativeTryConnect = (delegate* unmanaged[Cdecl]<nint, nint, int, int*, bool>)tryConnectPointer;
            NativeBeginConnect = (delegate* unmanaged[Cdecl]<nint, nint, nint, void>)beginConnectPointer;
            NativeTryShutdown = (delegate* unmanaged[Cdecl]<nint, int*, bool>)tryShutdownPointer;
            NativeBeginShutdown = (delegate* unmanaged[Cdecl]<nint, nint, nint, void>)beginShutdownPointer;
            NativeTryReceive = (delegate* unmanaged[Cdecl]<nint, byte*, uint, int, long*, int*, bool>)tryReceivePointer;
            NativeBeginReceive = (delegate* unmanaged[Cdecl]<nint, nint, void>)beginReceivePointer;
            NativeTrySend = (delegate* unmanaged[Cdecl]<nint, byte*, uint, int, long*, int*, bool>)trySendPointer;
            NativeBeginSend = (delegate* unmanaged[Cdecl]<nint, nint, void>)beginSendPointer;

            ConnectPool = Pools.GetOrAdd(TrySelectThreadSafePool, OnPoolMissing<ConnectContext>);
            ShutdownPool = Pools.GetOrAdd(TrySelectThreadSafePool, OnPoolMissing<ShutdownContext>);
            TransferPool = Pools.GetOrAdd(TrySelectThreadSafePool, OnPoolMissing<ClientContext>);
        }

        public bool TryGetRemoteEndPoint(out EndPoint? endPoint)
            => RemoteEndPoint.TryGet(out endPoint);

        public static unsafe bool TryCreate(Configuration configuration, out Client? client, out Exception? exception)
        {
            bool success = TryCreate(configuration, out nint descriptor, out exception);

            client = success
                ? new Client(descriptor, configuration)
                : default;

            return success;
        }

        public unsafe bool TryConnect(EndPoint endPoint, out Exception? exception)
        {
            if (endPoint is null)
            {
                exception = new ArgumentNullException(nameof(endPoint));

                return false;
            }

            if (!Bound && !TryBind(out exception))
                return false;

            int errorCode = default;
            bool success = NativeTryConnect(Descriptor, endPoint, endPoint.Size, &errorCode);

            if (success)
            {
                RemoteEndPoint.Set(endPoint);

                exception = default;
            }
            else
            {
                exception = PlatformException.FromCode(errorCode);
            }

            return success;
        }

        public unsafe Task<bool> BeginConnect(EndPoint endPoint, ConnectCallback connectedCallback, ErrorCallback errorCallback)
        {
            if (!Bound && !TryBind(out Exception? exception))
            {
                errorCallback?.Invoke(this, exception!);

                return Task.FromResult(false);
            }

            TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();
            ConnectContext context = ConnectPool.Acquire(OnPoolEmpty<ConnectContext>);

            context!.Hydrate(Descriptor, errorCallback, completionSource, OnBeginConnectCompletion, connectedCallback, endPoint);

            NativeBeginConnect(IOHandle.Handle, Configuration, context);

            return completionSource.Task;
        }

        public unsafe bool TryShutdown(out Exception? exception)
        {
            if (RemoteEndPoint.IfNone(OnNoneRemoteEndPoint, out exception))
                return false;

            int errorCode = default;
            bool success = NativeTryShutdown(Descriptor, &errorCode);

            if (success)
            {
                RemoteEndPoint.Clear();

                exception = default;
            }
            else
            {
                exception = PlatformException.FromCode(errorCode);
            }

            return success;
        }

        public unsafe Task<bool> BeginShutdown(ShutdownCallback shutdownCallback, ErrorCallback errorCallback)
        {
            if (RemoteEndPoint.IfNone(OnNoneRemoteEndPoint, errorCallback))
                return Task.FromResult(false);

            TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();
            ShutdownContext context = ShutdownPool.Acquire(OnPoolEmpty<ShutdownContext>);

            context!.Hydrate(Descriptor, errorCallback, completionSource, OnBeginShutdownCompletion, shutdownCallback);

            NativeBeginShutdown(IOHandle.Handle, Configuration, context);

            return completionSource.Task;
        }

        public unsafe bool TryReceive(ref byte buffer, int flags, out Exception? exception)
        {
            if (RemoteEndPoint.IfNone(OnNoneRemoteEndPoint, out exception))
                return false;

            byte data = default;
            long bytesCount = default;
            int errorCode = default;
            bool success = NativeTryReceive(Descriptor, &data, 1, flags, &bytesCount, &errorCode);

            if (success)
            {
                buffer = data;
                exception = default;
            }
            else
            {
                exception = PlatformException.FromCode(errorCode);
            }

            return success;
        }

        public unsafe bool TryReceive(byte[] buffer, int length, int flags, out int bytesReceived, out Exception? exception)
        {
            bytesReceived = default;

            if (RemoteEndPoint.IfNone(OnNoneRemoteEndPoint, out exception))
                return false;

            if (buffer is null)
            {
                exception = new ArgumentNullException(nameof(buffer));

                return false;
            }

            nint nativeBuffer = AcquireNativeBuffer(length);
            long bytesCount = default;
            int errorCode = default;
            bool success = NativeTryReceive(Descriptor, (byte*)nativeBuffer, (uint)length, flags, &bytesCount, &errorCode);

            if (success)
            {
                Marshal.Copy(nativeBuffer, buffer, 0, length);
                bytesReceived = (int)bytesCount;
                exception = default;
            }
            else
            {
                exception = PlatformException.FromCode(errorCode);
            }

            ReleaseNativeBuffer(nativeBuffer);

            return success;
        }

        public unsafe Task<int> BeginReceive(byte[] buffer, int length, int flags, TransferCallback transferCallback, ErrorCallback errorCallback)
        {
            if (RemoteEndPoint.IfNone(OnNoneRemoteEndPoint, errorCallback))
                return Task.FromResult(-1);

            TaskCompletionSource<int> completionSource = new TaskCompletionSource<int>();
            ClientContext context = TransferPool.Acquire(OnPoolEmpty<ClientContext>);
            NativeList<byte> nativeBuffer = AcquireNativeBuffer(length);

            context!.Hydrate(Descriptor, errorCallback, completionSource, OnBeginTransferCompletion, transferCallback, buffer, length, nativeBuffer, flags);

            NativeBeginReceive(IOHandle.Handle, context);

            return completionSource.Task;
        }

        public unsafe bool TrySend(byte buffer, int flags, out Exception? exception)
        {
            if (RemoteEndPoint.IfNone(OnNoneRemoteEndPoint, out exception))
                return false;

            long bytesCount = default;
            int errorCode = default;
            bool success = NativeTrySend(Descriptor, &buffer, 1, flags, &bytesCount, &errorCode);

            if (success)
                exception = default;
            else
                exception = PlatformException.FromCode(errorCode);

            return success;
        }

        public unsafe bool TrySend(byte[] buffer, int length, int flags, out int bytesSent, out Exception? exception)
        {
            bytesSent = default;

            if (RemoteEndPoint.IfNone(OnNoneRemoteEndPoint, out exception))
                return false;

            if (buffer is null)
            {
                exception = new ArgumentNullException(nameof(buffer));

                return false;
            }

            nint nativeBuffer = AcquireNativeBuffer(length);

            Marshal.Copy(buffer, 0, nativeBuffer, length);

            long bytesCount = default;
            int errorCode = default;
            bool success = NativeTrySend(Descriptor, (byte*)nativeBuffer, (uint)length, flags, &bytesCount, &errorCode);

            if (success)
            {
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

        public unsafe Task BeginSend(byte[] buffer, int length, int flags, TransferCallback transferCallback, ErrorCallback errorCallback)
        {
            if (RemoteEndPoint.IfNone(OnNoneRemoteEndPoint, errorCallback))
                return Task.FromResult(-1);

            TaskCompletionSource<int> completionSource = new TaskCompletionSource<int>();
            ClientContext context = TransferPool.Acquire(OnPoolEmpty<ClientContext>);
            NativeList<byte> nativeBuffer = AcquireNativeBuffer(length);

            context!.Hydrate(Descriptor, errorCallback, completionSource, OnBeginTransferCompletion, transferCallback, buffer, length, nativeBuffer, flags);

            NativeBeginSend(IOHandle.Handle, context);

            if (context.CompletedSynchronously)
            {
                bool completedSuccessfully = context.CompletedSuccessfully;
                int bytesTransferred = (int)context.BytesTransferred;
                int errorCode = context.ErrorCode;

                context.Release();

                OnBeginTransferCompletion(completedSuccessfully,
                    errorCallback,
                    completionSource,
                    transferCallback,
                    buffer,
                    nativeBuffer,
                    bytesTransferred,
                    errorCode);
            }

            return completionSource.Task;
        }

        private void OnBeginConnectCompletion(ConnectContext context)
        {
            if (context.CompletedSuccessfully)
            {
                RemoteEndPoint.Set(context.EndPoint);

                context.ResultCallback?.Invoke(this, context.EndPoint);
            }
            else
            {
                PlatformException exception = PlatformException.FromCode(context.ErrorCode);

                context.ErrorCallback?.Invoke(this, exception);
            }

            context.CompletionSource!.SetResult(context.CompletedSuccessfully);
        }

        private void OnBeginShutdownCompletion(ShutdownContext context)
        {
            if (context.CompletedSuccessfully)
            {
                context.ResultCallback?.Invoke(this);
            }
            else
            {
                PlatformException exception = PlatformException.FromCode(context.ErrorCode);

                context.ErrorCallback?.Invoke(this, exception);
            }

            context.CompletionSource!.SetResult(context.CompletedSuccessfully);
        }

        private void OnBeginTransferCompletion(ClientContext context)
            => OnBeginTransferCompletion(context.CompletedSuccessfully,
                context.ErrorCallback,
                context.CompletionSource!,
                context.ResultCallback,
                context.Buffer!,
                context.NativeBuffer,
                (int)context.BytesTransferred,
                context.ErrorCode);

        private void OnBeginTransferCompletion(bool completedSuccessfully,
            ErrorCallback? errorCallback,
            TaskCompletionSource<int> completionSource,
            TransferCallback? transferCallback,
            byte[] buffer,
            NativeList<byte> nativeBuffer,
            int bytesTransferred,
            int errorCode)
        {
            if (completedSuccessfully)
            {
                Marshal.Copy(nativeBuffer, buffer, 0, bytesTransferred);

                ReleaseNativeBuffer(nativeBuffer);

                transferCallback?.Invoke(this, RemoteEndPoint.Target!, buffer, bytesTransferred);
                completionSource.SetResult(bytesTransferred);
            }
            else
            {
                PlatformException exception = PlatformException.FromCode(errorCode);

                errorCallback?.Invoke(this, exception);
                completionSource.SetResult(-1);
            }
        }

        private NotConnectedException OnNoneRemoteEndPoint()
            => new NotConnectedException(SocketError.NotConnected);

        private void OnNoneRemoteEndPoint(ErrorCallback errorCallback)
        {
            NotConnectedException notConnectedException = OnNoneRemoteEndPoint();

            errorCallback?.Invoke(this, notConnectedException);
        }
    }
}
