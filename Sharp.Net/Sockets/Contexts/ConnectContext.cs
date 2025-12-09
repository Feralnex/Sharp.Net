using Sharp.Helpers;
using System;
using System.Runtime.InteropServices;

namespace Sharp.Net.Sockets.Contexts
{
    public class ConnectContext : ConnectContext<bool, ConnectContext, ConnectCallback>
    {
        public unsafe ConnectContext() : base() { }

        public override void HandleCompletion()
            => CompletionCallback!(this);
    }

    public abstract class ConnectContext<TResultType, TSocketContext, TDelegate> : SocketContext<TResultType, TSocketContext, TDelegate>
         where TSocketContext : ConnectContext<TResultType, TSocketContext, TDelegate>
         where TDelegate : Delegate
    {
        #region Platform specific information

        private static unsafe delegate* unmanaged[Cdecl]<nint> NewConnectContext { get; }
        private static unsafe delegate* unmanaged[Cdecl]<nint, void> DeleteConnectContext { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetConnectContextEndPointOffset { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetConnectContextEndPointLengthOffset { get; }

        private static readonly int _endPointOffset;
        private static readonly int _endPointLengthOffset;
        private static readonly GetInt64 _getEndPointLength;
        private static readonly SetInt64 _setEndPointLength;

        #endregion Platform specific information

        protected unsafe override delegate* unmanaged[Cdecl]<nint, void> Destructor => DeleteConnectContext;

        public unsafe nint EndPoint
        {
            get => Pointer.DangerousToNInt(Content, _endPointOffset);
            set => Pointer.DangerousInsert(Content, _endPointOffset, value);
        }
        public unsafe long EndPointLength
        {
            get => _getEndPointLength(Content, _endPointLengthOffset);
            set => _setEndPointLength(Content, _endPointLengthOffset, value);
        }

        public unsafe ConnectContext() : base(NewConnectContext()) { }

        protected ConnectContext(nint pointer) : base(pointer) { }

        static unsafe ConnectContext()
        {
            nint newConnectContextPointer = Library.GetExport(nameof(Net), nameof(NewConnectContext));
            nint deleteConnectContextPointer = Library.GetExport(nameof(Net), nameof(DeleteConnectContext));
            nint getConnectContextEndPointOffsetPointer = Library.GetExport(nameof(Net), nameof(GetConnectContextEndPointOffset));
            nint getConnectContextEndPointLengthOffsetPointer = Library.GetExport(nameof(Net), nameof(GetConnectContextEndPointLengthOffset));

            NewConnectContext = (delegate* unmanaged[Cdecl]<nint>)newConnectContextPointer;
            DeleteConnectContext = (delegate* unmanaged[Cdecl]<nint, void>)deleteConnectContextPointer;
            GetConnectContextEndPointOffset = (delegate* unmanaged[Cdecl]<int>)getConnectContextEndPointOffsetPointer;
            GetConnectContextEndPointLengthOffset = (delegate* unmanaged[Cdecl]<int>)getConnectContextEndPointLengthOffsetPointer;

            _endPointOffset = GetConnectContextEndPointOffset();
            _endPointLengthOffset = GetConnectContextEndPointLengthOffset();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _getEndPointLength = GetEndPointLengthFromInt32;
                _setEndPointLength = SetEndPointLengthAsInt32;
            }
            else
            {
                _getEndPointLength = GetEndPointLengthFromUInt32;
                _setEndPointLength = GetEndPointLengthFromUInt32;
            }
        }

        private static unsafe long GetEndPointLengthFromInt32(byte* source, int index)
            => Pointer.DangerousToInt32(source, index);

        private static unsafe void SetEndPointLengthAsInt32(byte* source, int index, long value)
            => Pointer.DangerousInsert(source, index, (int)value);

        private static unsafe long GetEndPointLengthFromUInt32(byte* source, int index)
            => Pointer.DangerousToUInt32(source, index);

        private static unsafe void GetEndPointLengthFromUInt32(byte* source, int index, long value)
            => Pointer.DangerousInsert(source, index, (uint)value);
    }
}
