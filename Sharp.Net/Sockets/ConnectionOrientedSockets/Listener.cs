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
    public partial class Client : Socket
    {
        public class Listener : Socket
        {
            #region Platform specific information

            private static unsafe delegate* unmanaged[Cdecl]<nint, int, int*, bool> NativeTryListen { get; }
            private static unsafe delegate* unmanaged[Cdecl]<nint, nint, nint, int*, int*, bool> NativeTryAccept { get; }
            private static unsafe delegate* unmanaged[Cdecl]<nint, nint, nint, void> NativeBeginAccept { get; }

            #endregion Platform specific information

            protected static IPool<AcceptContext> AcceptPool { get; }

            protected Listener(nint descriptor, Configuration configuration) : base(descriptor, configuration) { }

            protected Listener(nint descriptor, Configuration configuration, EndPoint localEndPoint) : base(descriptor, configuration, localEndPoint) { }

            static unsafe Listener()
            {
                nint tryListenPointer = Library.GetExport(nameof(Net), nameof(TryListen));
                nint tryAcceptPointer = Library.GetExport(nameof(Net), nameof(TryAccept));
                nint beginAcceptPointer = Library.GetExport(nameof(Net), nameof(BeginAccept));

                NativeTryListen = (delegate* unmanaged[Cdecl]<nint, int, int*, bool>)tryListenPointer;
                NativeTryAccept = (delegate* unmanaged[Cdecl]<nint, nint, nint, int*, int*, bool>)tryAcceptPointer;
                NativeBeginAccept = (delegate* unmanaged[Cdecl]<nint, nint, nint, void>)beginAcceptPointer;

                AcceptPool = Pools.GetOrAdd(TrySelectThreadSafePool, OnPoolMissing<AcceptContext>);
            }

            public static unsafe bool TryCreate(Configuration configuration, out Listener? listener, out Exception? exception)
            {
                bool success = TryCreate(configuration, out nint descriptor, out exception);

                listener = success
                    ? new Listener(descriptor, configuration)
                    : default;

                return success;
            }

            public unsafe bool TryListen(int pendingQueue, out Exception? exception)
            {
                if (!Bound && !TryBind(out exception))
                    return false;

                int errorCode = default;
                bool success = NativeTryListen(Descriptor, pendingQueue, &errorCode);

                if (success)
                    exception = default;
                else
                    exception = PlatformException.FromCode(errorCode);

                return success;
            }

            public unsafe bool TryAccept(out Client? client, out Exception? exception)
            {
                nint clientDescriptor = (nint)NativeMemory.AllocZeroed(DescriptorSize);
                EndPoint remoteEndPoint = Configuration.AllocateEndPoint();
                int remoteEndPointSize = remoteEndPoint.Size;
                int errorCode = default;
                bool success = NativeTryAccept(Descriptor, clientDescriptor, remoteEndPoint, &remoteEndPointSize, &errorCode);

                if (success)
                {
                    client = new Client(clientDescriptor, Configuration, LocalEndPoint.Target!, remoteEndPoint);
                    exception = default;
                }
                else
                {
                    client = default;
                    exception = PlatformException.FromCode(errorCode);

                    NativeMemory.Free((void*)clientDescriptor);
                }

                return success;
            }

            public unsafe Task<Client> BeginAccept(AcceptCallback acceptCallback, ErrorCallback errorCallback)
            {
                TaskCompletionSource<Client> completionSource = new TaskCompletionSource<Client>();
                AcceptContext context = AcceptPool.Acquire(OnPoolEmpty<AcceptContext>);
                EndPoint endPoint = Configuration.AllocateEndPoint();

                context!.Hydrate(Descriptor, errorCallback, completionSource, OnBeginAcceptCompletion, acceptCallback, endPoint);

                NativeBeginAccept(IOHandle.Handle, Configuration, context);

                return completionSource.Task;
            }

            private void OnBeginAcceptCompletion(AcceptContext context)
            {
                Client? client = default;

                if (context.CompletedSuccessfully)
                {
                    client = new Client(context.RemoteSocket, Configuration, LocalEndPoint.Target!, context.EndPoint);

                    context.ResultCallback?.Invoke(this, client);
                }
                else
                {
                    PlatformException exception = PlatformException.FromCode(context.ErrorCode);

                    context.ErrorCallback?.Invoke(this, exception);
                }

                context.CompletionSource!.SetResult(client!);
            }
        }
    }
}
