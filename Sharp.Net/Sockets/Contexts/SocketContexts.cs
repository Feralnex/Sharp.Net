using Sharp.Helpers;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Sharp.Net.Sockets.Contexts
{
    public class SocketContexts
    {
        #region Platform specific information

        private static unsafe delegate* unmanaged[Cdecl]<int, nint> NewSocketContexts { get; }
        private static unsafe delegate* unmanaged[Cdecl]<nint, void> DeleteSocketContexts { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetSocketContextsLengthOffset { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetSocketContextsEventCountOffset { get; }

        private static readonly int _lengthOffset;
        private static readonly int _eventCountOffset;

        #endregion Platform specific information

        protected static ConcurrentDictionary<nint, SocketContexts> ReverseCache { get; }

        private readonly nint _pointer;

        protected unsafe nint* Content => (nint*)*(nint*)_pointer;

        public unsafe int Length => Pointer.DangerousToInt32((byte*)_pointer, _lengthOffset);
        public unsafe int EventCount => Pointer.DangerousToInt32((byte*)_pointer, _eventCountOffset);

        public unsafe SocketContext this[int index] => Content[index];

        public unsafe SocketContexts(int length) : this(NewSocketContexts(length)) { }

        protected SocketContexts(nint pointer)
        {
            _pointer = pointer;

            ReverseCache.TryAdd(_pointer, this);
        }

        static unsafe SocketContexts()
        {
            nint newSocketContextsPointer = Library.GetExport(nameof(Net), nameof(NewSocketContexts));
            nint deleteSocketContextsPointer = Library.GetExport(nameof(Net), nameof(DeleteSocketContexts));
            nint getSocketContextsLengthOffsetPointer = Library.GetExport(nameof(Net), nameof(GetSocketContextsLengthOffset));
            nint getSocketContextsEventCountOffsetPointer = Library.GetExport(nameof(Net), nameof(GetSocketContextsEventCountOffset));

            NewSocketContexts = (delegate* unmanaged[Cdecl]<int, nint>)newSocketContextsPointer;
            DeleteSocketContexts = (delegate* unmanaged[Cdecl]<nint, void>)deleteSocketContextsPointer;
            GetSocketContextsLengthOffset = (delegate* unmanaged[Cdecl]<int>)getSocketContextsLengthOffsetPointer;
            GetSocketContextsEventCountOffset = (delegate* unmanaged[Cdecl]<int>)getSocketContextsEventCountOffsetPointer;

            _lengthOffset = GetSocketContextsLengthOffset();
            _eventCountOffset = GetSocketContextsEventCountOffset();

            ReverseCache = new ConcurrentDictionary<nint, SocketContexts>();
        }

        unsafe ~SocketContexts()
        {
            ReverseCache.TryRemove(_pointer, out _);

            DeleteSocketContexts(_pointer);
        }

        public unsafe void HandleCompletions()
        {
            for (int index = 0; index < EventCount; index++)
            {
                SocketContext context = Content[index];

                Task.Run(context.HandleCompletion);

                context.Release();
            }
        }

        public static implicit operator SocketContexts(nint pointer)
        {
            if (!ReverseCache.TryGetValue(pointer, out SocketContexts? socketContexts))
                throw new InvalidCastException();

            return socketContexts;
        }

        public static implicit operator nint(SocketContexts socketContexts)
            => socketContexts._pointer;
    }
}
