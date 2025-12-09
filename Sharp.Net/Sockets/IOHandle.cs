using CommunityToolkit.HighPerformance;
using Sharp.Collections;
using Sharp.Exceptions;
using Sharp.Helpers;
using Sharp.Net.Sockets.Contexts;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Sharp.Net.Sockets
{
    public class IOHandle
    {
        private const int MaxCompletionsPerCall = 4096;

        #region Platform specific information

        private static unsafe delegate* unmanaged[Cdecl]<nint*, uint, int*, bool> TryCreateHandle { get; }
        private static unsafe delegate* unmanaged[Cdecl]<nint, void> DisposeHandle { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int, nint> AllocateEntries { get; }
        private static unsafe delegate* unmanaged[Cdecl]<nint, void> FreeEntries { get; }
        private static unsafe delegate* unmanaged[Cdecl]<nint, nint, nint, int, int*, bool> TryHandleCompletions { get; }

        #endregion Platform specific information

        private static unsafe IPool<SocketContexts> SocketContextsPool { get; }

        protected static bool Running { get; }

        public static nint Handle { get; }

        static unsafe IOHandle()
        {
            nint tryCreateHandlePointer = Library.GetExport(nameof(Net), nameof(TryCreateHandle));
            nint disposeHandlePointer = Library.GetExport(nameof(Net), nameof(DisposeHandle));
            nint allocateEntriesPointer = Library.GetExport(nameof(Net), nameof(AllocateEntries));
            nint freeEntriesPointer = Library.GetExport(nameof(Net), nameof(FreeEntries));
            nint tryHandleCompletionsPointer = Library.GetExport(nameof(Net), nameof(TryHandleCompletions));

            SocketContextsPool = Pools.GetOrAdd(TrySelectSocketContextsPool, OnSocketContextsPoolMissing);

            TryCreateHandle = (delegate* unmanaged[Cdecl]<nint*, uint, int*, bool>)tryCreateHandlePointer;
            DisposeHandle = (delegate* unmanaged[Cdecl]<nint, void>)disposeHandlePointer;
            AllocateEntries = (delegate* unmanaged[Cdecl]<int, nint>)allocateEntriesPointer;
            FreeEntries = (delegate* unmanaged[Cdecl]<nint, void>)freeEntriesPointer;
            TryHandleCompletions = (delegate* unmanaged[Cdecl]<nint, nint, nint, int, int*, bool>)tryHandleCompletionsPointer;

            nint handle = nint.Zero;
            int errorCode = default;
            bool success = TryCreateHandle(&handle, 0, &errorCode);
            if (success)
            {
                Running = true;
                Handle = handle;
            }
            else
            {
                throw PlatformException.FromCode(errorCode);
            }

            int processorCount = Environment.ProcessorCount;
            for (int index = 0; index < processorCount; index++)
                Task.Factory.StartNew(StartHandlingCompletions, TaskCreationOptions.LongRunning);
        }

        private static unsafe void StartHandlingCompletions()
        {
            nint entries = AllocateEntries(MaxCompletionsPerCall);
            nint contexts = SocketContextsPool.Acquire(OnSocketContextsMissing);
            int errorCode = default;

            while (Running)
            {
                bool success = TryHandleCompletions(Handle, entries, contexts, MaxCompletionsPerCall, &errorCode);

                if (success)
                {
                    SocketContexts socketContexts = contexts;

                    Task.Factory.StartNew(HandleCompletions, socketContexts);

                    contexts = SocketContextsPool.Acquire(OnSocketContextsMissing);
                }
            }
        }

        private static void HandleCompletions(object? state)
        {
            SocketContexts contexts = Unsafe.As<SocketContexts>(state!);

            contexts.HandleCompletions();

            SocketContextsPool.Release(contexts);
        }

        private static bool TrySelectSocketContextsPool(ReadOnlySpan<IPool<SocketContexts>> span, out IPool<SocketContexts>? selectedPool)
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

        private static IPool<SocketContexts> OnSocketContextsPoolMissing()
            => new ConcurrentPool<SocketContexts>();

        private static SocketContexts OnSocketContextsMissing()
            => new SocketContexts(MaxCompletionsPerCall);
    }
}
