using CommunityToolkit.HighPerformance;
using Sharp.Collections;
using Sharp.Helpers;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Sharp.Net.Sockets.Contexts
{
    public abstract class SocketContext
    {
        #region Platform specific information

        private static unsafe delegate* unmanaged[Cdecl]<nint> NewSocketContext { get; }
        private static unsafe delegate* unmanaged[Cdecl]<nint, void> DeleteSocketContext { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetSocketContextDescriptorOffset { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetSocketContextCompletedSynchronouslyOffset { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetSocketContextCompletedSuccessfullyOffset { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetSocketContextErrorCodeOffset { get; }

        private static readonly int _descriptorOffset;
        private static readonly int _completedSynchronouslyOffset;
        private static readonly int _completedSuccessfullyOffset;
        private static readonly int _errorCodeOffset;

        #endregion Platform specific information

        protected static ConcurrentDictionary<nint, SocketContext> ReverseCache { get; }

        private readonly nint _pointer;

        protected unsafe byte* Content => (byte*)_pointer;
        protected unsafe virtual delegate* unmanaged[Cdecl]<nint, void> Destructor => DeleteSocketContext;

        public ErrorCallback? ErrorCallback { get; set; }
        public unsafe nint Descriptor
        {
            get => Pointer.DangerousToNInt(Content, _descriptorOffset);
            set => Pointer.DangerousInsert(Content, _descriptorOffset, value);
        }
        public unsafe bool CompletedSynchronously => Pointer.DangerousToBool(Content, _completedSynchronouslyOffset);
        public unsafe bool CompletedSuccessfully => Pointer.DangerousToBool(Content, _completedSuccessfullyOffset);
        public unsafe int ErrorCode => Pointer.DangerousToInt32(Content, _errorCodeOffset);

        protected unsafe SocketContext() : this(NewSocketContext()) { }

        protected SocketContext(nint pointer)
        {
            _pointer = pointer;

            ReverseCache.TryAdd(_pointer, this);
        }

        static unsafe SocketContext()
        {
            nint newSocketContextPointer = Library.GetExport(nameof(Net), nameof(NewSocketContext));
            nint deleteSocketContextPointer = Library.GetExport(nameof(Net), nameof(DeleteSocketContext));
            nint getSocketContextDescriptorOffsetPointer = Library.GetExport(nameof(Net), nameof(GetSocketContextDescriptorOffset));
            nint getSocketContextCompletedSynchronouslyOffsetPointer = Library.GetExport(nameof(Net), nameof(GetSocketContextCompletedSynchronouslyOffset));
            nint getSocketContextCompletedSuccessfullyOffsetPointer = Library.GetExport(nameof(Net), nameof(GetSocketContextCompletedSuccessfullyOffset));
            nint getSocketContextErrorCodeOffsetPointer = Library.GetExport(nameof(Net), nameof(GetSocketContextErrorCodeOffset));

            NewSocketContext = (delegate* unmanaged[Cdecl]<nint>)newSocketContextPointer;
            DeleteSocketContext = (delegate* unmanaged[Cdecl]<nint, void>)deleteSocketContextPointer;
            GetSocketContextDescriptorOffset = (delegate* unmanaged[Cdecl]<int>)getSocketContextDescriptorOffsetPointer;
            GetSocketContextCompletedSynchronouslyOffset = (delegate* unmanaged[Cdecl]<int>)getSocketContextCompletedSynchronouslyOffsetPointer;
            GetSocketContextCompletedSuccessfullyOffset = (delegate* unmanaged[Cdecl]<int>)getSocketContextCompletedSuccessfullyOffsetPointer;
            GetSocketContextErrorCodeOffset = (delegate* unmanaged[Cdecl]<int>)getSocketContextErrorCodeOffsetPointer;

            _descriptorOffset = GetSocketContextDescriptorOffset();
            _completedSynchronouslyOffset = GetSocketContextCompletedSynchronouslyOffset();
            _completedSuccessfullyOffset = GetSocketContextCompletedSuccessfullyOffset();
            _errorCodeOffset = GetSocketContextErrorCodeOffset();

            ReverseCache = new ConcurrentDictionary<nint, SocketContext>();
        }

        unsafe ~SocketContext()
        {
            ReverseCache.TryRemove(_pointer, out _);

            Destructor(_pointer);
        }

        public abstract void HandleCompletion();

        public abstract void Release();

        public static implicit operator SocketContext(nint pointer)
        {
            if (!ReverseCache.TryGetValue(pointer, out SocketContext? socketContext))
                throw new InvalidCastException();

            return socketContext;
        }

        public static implicit operator nint(SocketContext socketContext)
            => socketContext._pointer;
    }

    public abstract class SocketContext<TReturnType, TSocketContext, TDelegate> : SocketContext
        where TSocketContext : SocketContext
        where TDelegate : Delegate
    {
        protected static IPool Pool { get; private set; }

        public TaskCompletionSource<TReturnType>? CompletionSource { get; set; }
        public CompletionCallback<TSocketContext>? CompletionCallback { get; set; }
        public TDelegate? ResultCallback { get; set; }

        protected SocketContext() : base() { }

        protected SocketContext(nint pointer) : base(pointer) { }

        static SocketContext()
        {
            Pool = Pools.GetOrAdd(TrySelectThreadSafePool, OnPoolMissing);
        }

        public override void Release()
        {
            Pool.Release(this);
        }

        protected static bool TrySelectThreadSafePool(ReadOnlySpan<IPool<TSocketContext>> span, out IPool<TSocketContext>? selectedPool)
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

        protected static IPool<TSocketContext> OnPoolMissing()
            => new ConcurrentPool<TSocketContext>();
    }
}
