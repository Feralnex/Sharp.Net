using CommunityToolkit.HighPerformance;
using Sharp.Collections;
using Sharp.Exceptions;
using Sharp.Extensions;
using Sharp.Helpers;
using Sharp.Net.Configurations;
using Sharp.Net.EndPoints;
using Sharp.Net.Sockets.Contexts;
using System;
using System.Runtime.InteropServices;

namespace Sharp.Net.Sockets
{
    public abstract class Socket
    {
        #region Platform specific information

        private static unsafe delegate* unmanaged[Cdecl]<nuint> GetDescriptorSize { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int*, bool> TryWSAStartup { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int*, bool> TryWSACleanup { get; }
        protected static unsafe delegate* unmanaged[Cdecl]<nint, nint, int*, bool> TryCreateSocket { get; }
        protected static unsafe delegate* unmanaged[Cdecl]<nint, nint, int*, bool> TryPrepareSocketForAsync { get; }
        protected static unsafe delegate* unmanaged[Cdecl]<nint, nint, int, int*, bool> TryBindSocket { get; }
        protected static unsafe delegate* unmanaged[Cdecl]<nint, int*, bool> TryCloseSocket { get; }

        #endregion Platform specific information

        private static IKeyedPool<int, NativeList<byte>> BufferPool { get; }

        public static bool Initialized { get; private set; }
        public static nuint DescriptorSize { get; private set; }

        public nint Descriptor { get; private set; }
        public Configuration Configuration { get; private set; }
        public bool Bound => LocalEndPoint.HasSome;

        protected Reference<EndPoint> LocalEndPoint;

        protected Socket(nint descriptor, Configuration configuration)
        {
            LocalEndPoint = new Reference<EndPoint>();

            Descriptor = descriptor;
            Configuration = configuration;
        }

        protected Socket(nint descriptor, Configuration configuration, EndPoint localEndPoint) : this(descriptor, configuration)
        {
            LocalEndPoint.Set(localEndPoint);
        }

        static unsafe Socket()
        {
            nint getDescriptorSizePointer = Library.GetExport(nameof(Net), nameof(GetDescriptorSize));
            nint tryWSAStartupPointer = Library.GetExport(nameof(Net), nameof(TryWSAStartup));
            nint tryWSACleanupPointer = Library.GetExport(nameof(Net), nameof(TryWSACleanup));
            nint tryCreateSocketPointer = Library.GetExport(nameof(Net), nameof(TryCreateSocket));
            nint tryPrepareSocketForAsyncPointer = Library.GetExport(nameof(Net), nameof(TryPrepareSocketForAsync));
            nint tryBindSocketPointer = Library.GetExport(nameof(Net), nameof(TryBindSocket));
            nint tryCloseSocketPointer = Library.GetExport(nameof(Net), nameof(TryCloseSocket));

            BufferPool = KeyedPools.GetOrAdd(OnBufferPoolMissing);

            GetDescriptorSize = (delegate* unmanaged[Cdecl]<nuint>)getDescriptorSizePointer;
            TryWSAStartup = (delegate* unmanaged[Cdecl]<int*, bool>)tryWSAStartupPointer;
            TryWSACleanup = (delegate* unmanaged[Cdecl]<int*, bool>)tryWSACleanupPointer;
            TryCreateSocket = (delegate* unmanaged[Cdecl]<nint, nint, int*, bool>)tryCreateSocketPointer;
            TryPrepareSocketForAsync = (delegate* unmanaged[Cdecl]<nint, nint, int*, bool>)tryPrepareSocketForAsyncPointer;
            TryBindSocket = (delegate* unmanaged[Cdecl]<nint, nint, int, int*, bool>)tryBindSocketPointer;
            TryCloseSocket = (delegate* unmanaged[Cdecl]<nint, int*, bool>)tryCloseSocketPointer;

            int errorCode = default;

            Initialized = TryWSAStartup(&errorCode);
            DescriptorSize = GetDescriptorSize();

            if (!Initialized)
                throw PlatformException.FromCode(errorCode);
            else
                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }

        unsafe ~Socket()
        {
            NativeMemory.Free((void*)Descriptor);
        }

        public bool TryGetLocalEndPoint(out EndPoint? endPoint)
            => LocalEndPoint.TryGet(out endPoint);

        public bool TryBind(out Exception? exception)
        {
            EndPoint localEndPoint = Configuration.AllocateEndPoint();
            bool bound = TryBind(localEndPoint, out exception);

            return bound;
        }

        public unsafe bool TryBind(EndPoint endPoint, out Exception? exception)
        {
            if (endPoint is null)
            {
                exception = new ArgumentNullException(nameof(endPoint));

                return false;
            }

            int errorCode = default;
            bool success = TryBindSocket(Descriptor, endPoint, endPoint.Size, &errorCode);

            if (success)
            {
                LocalEndPoint.Set(endPoint);

                exception = default;
            }
            else
            {
                exception = PlatformException.FromCode(errorCode);
            }

            return success;
        }

        public unsafe bool TryClose(out Exception? exception)
        {
            int errorCode = default;
            bool success = TryCloseSocket(Descriptor, &errorCode);

            if (success)
            {
                OnClosed();

                exception = default;
            }
            else
            {
                exception = PlatformException.FromCode(errorCode);
            }

            return success;
        }

        protected virtual void OnClosed()
            => LocalEndPoint.Clear();

        protected static unsafe bool TryCreate(Configuration configuration, out nint descriptor, out Exception? exception)
        {
            descriptor = (nint)NativeMemory.AllocZeroed(DescriptorSize);
            exception = default;

            int errorCode = default;
            bool success = TryCreateSocket(configuration, descriptor, &errorCode);

            if (success)
            {
                success = TryPrepareSocketForAsync(descriptor, IOHandle.Handle, &errorCode);

                if (!success)
                {
                    PlatformException prepareSocketAsyncException = PlatformException.FromCode(errorCode);

                    if (!TryCloseSocket(descriptor, &errorCode))
                    {
                        PlatformException closeException = PlatformException.FromCode(errorCode);

                        exception = new PlatformException(prepareSocketAsyncException.Code | closeException.Code, string.Join(Environment.NewLine, prepareSocketAsyncException.Message, closeException.Message));
                    }
                }
            }
            else
            {
                exception = PlatformException.FromCode(errorCode);
            }

            if (!success)
                NativeMemory.Free((void*)descriptor);

            return success;
        }

        protected static bool TrySelectThreadSafePool<TSocketContext>(ReadOnlySpan<IPool<TSocketContext>> span, out IPool<TSocketContext>? selectedPool)
            where TSocketContext : SocketContext
        {
            selectedPool = default!;

            for (int index = 0; index < span.Length; index++)
            {
                selectedPool = span.DangerousGetReferenceAt(index);

                if (selectedPool.IsThreadSafe)
                    return true;
            }

            return false;
        }

        protected static IPool<TSocketContext> OnPoolMissing<TSocketContext>()
             where TSocketContext : SocketContext
            => new ConcurrentPool<TSocketContext>();

        protected static TSocketContext OnPoolEmpty<TSocketContext>()
             where TSocketContext : SocketContext, new()
            => new TSocketContext();

        protected static NativeList<byte> AcquireNativeBuffer(int length)
        {
            int bufferSize = length.GetBucketValue();
            NativeList<byte> nativeBuffer = BufferPool.Acquire(bufferSize, OnNativeListMissing);

            return nativeBuffer!;
        }

        protected static void ReleaseNativeBuffer(NativeList<byte> nativeBuffer)
        {
            BufferPool.Release((int)nativeBuffer.Size, nativeBuffer);
        }

        protected static ConcurrentKeyedPool<int, NativeList<byte>> OnBufferPoolMissing()
            => new ConcurrentKeyedPool<int, NativeList<byte>>();

        protected static NativeList<byte> OnNativeListMissing(int bufferSize)
            => new NativeList<byte>((nuint)bufferSize);

        private static unsafe void OnProcessExit(object? sender, EventArgs e)
        {
            int errorCode = default;
            bool cleanedUp = TryWSACleanup(&errorCode);

            if (!cleanedUp)
                throw PlatformException.FromCode(errorCode);
        }
    }
}
