using Sharp.Helpers;
using System;
using System.Runtime.InteropServices;

namespace Sharp.Net.Sockets.Contexts
{
    public class ClientContext : ClientContext<int, ClientContext, TransferCallback>
    {
        public unsafe ClientContext() : base() { }

        protected ClientContext(nint pointer) : base(pointer) { }

        public override void HandleCompletion()
            => CompletionCallback!(this);
    }

    public abstract class ClientContext<TResultType, TSocketContext, TDelegate> : SocketContext<TResultType, TSocketContext, TDelegate>
        where TSocketContext : ClientContext<TResultType, TSocketContext, TDelegate>
        where TDelegate : Delegate
    {
        #region Platform specific information

        private static unsafe delegate* unmanaged[Cdecl]<nint> NewClientContext { get; }
        private static unsafe delegate* unmanaged[Cdecl]<nint, void> DeleteClientContext { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetClientContextLengthOffset { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetClientContextLengthSize { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetClientContextBufferOffset { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetClientContextFlagsOffset { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetClientContextBytesTransferredOffset { get; }

        private static readonly int _lengthOffset;
        private static readonly int _lengthSize;
        private static readonly int _nativeBufferOffset;
        private static readonly int _flagsOffset;
        private static readonly int _bytesTransferredOffset;
        private static readonly GetUInt64 _getLength;
        private static readonly SetUInt64 _setLength;
        private static readonly GetInt64 _getFlagsOrBytesTransferred;
        private static readonly SetInt64 _setFlagsOrBytesTransferred;

        #endregion Platform specific information

        protected unsafe override delegate* unmanaged[Cdecl]<nint, void> Destructor => DeleteClientContext;

        public byte[]? Buffer { get; set; }
        public unsafe ulong Length
        {
            get => _getLength(Content, _lengthOffset);
            set => _setLength(Content, _lengthOffset, value);
        }
        public unsafe nint NativeBuffer
        {
            get => Pointer.DangerousToNInt(Content, _nativeBufferOffset);
            set => Pointer.DangerousInsert(Content, _nativeBufferOffset, value);
        }
        public unsafe long Flags
        {
            get => _getFlagsOrBytesTransferred(Content, _flagsOffset);
            set => _setFlagsOrBytesTransferred(Content, _flagsOffset, value);
        }
        public unsafe long BytesTransferred => _getFlagsOrBytesTransferred(Content, _bytesTransferredOffset);

        public unsafe ClientContext() : base(NewClientContext()) { }

        protected ClientContext(nint pointer) : base(pointer) { }

        static unsafe ClientContext()
        {
            nint newClientContextPointer = Library.GetExport(nameof(Net), nameof(NewClientContext));
            nint deleteClientContextPointer = Library.GetExport(nameof(Net), nameof(DeleteClientContext));
            nint getClientContextLengthOffsetPointer = Library.GetExport(nameof(Net), nameof(GetClientContextLengthOffset));
            nint getClientContextLengthSizePointer = Library.GetExport(nameof(Net), nameof(GetClientContextLengthSize));
            nint getClientContextBufferOffsetPointer = Library.GetExport(nameof(Net), nameof(GetClientContextBufferOffset));
            nint getClientContextFlagsOffsetPointer = Library.GetExport(nameof(Net), nameof(GetClientContextFlagsOffset));
            nint getClientContextBytesTransferredOffsetPointer = Library.GetExport(nameof(Net), nameof(GetClientContextBytesTransferredOffset));

            NewClientContext = (delegate* unmanaged[Cdecl]<nint>)newClientContextPointer;
            DeleteClientContext = (delegate* unmanaged[Cdecl]<nint, void>)deleteClientContextPointer;
            GetClientContextLengthOffset = (delegate* unmanaged[Cdecl]<int>)getClientContextLengthOffsetPointer;
            GetClientContextLengthSize = (delegate* unmanaged[Cdecl]<int>)getClientContextLengthSizePointer;
            GetClientContextBufferOffset = (delegate* unmanaged[Cdecl]<int>)getClientContextBufferOffsetPointer;
            GetClientContextFlagsOffset = (delegate* unmanaged[Cdecl]<int>)getClientContextFlagsOffsetPointer;
            GetClientContextBytesTransferredOffset = (delegate* unmanaged[Cdecl]<int>)getClientContextBytesTransferredOffsetPointer;

            _lengthOffset = GetClientContextLengthOffset();
            _lengthSize = GetClientContextLengthSize();
            _nativeBufferOffset = GetClientContextBufferOffset();
            _flagsOffset = GetClientContextFlagsOffset();
            _bytesTransferredOffset = GetClientContextBytesTransferredOffset();

            if (_lengthSize == sizeof(uint))
            {
                _getLength = GetLength;
                _setLength = SetLength;
            }
            else
            {
                _getLength = Pointer.DangerousToUInt64;
                _setLength = Pointer.DangerousInsert;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _getFlagsOrBytesTransferred = GetFlagsOrBytesTransferredFromUInt32;
                _setFlagsOrBytesTransferred = SetFlagsOrBytesTransferredAsUInt32;
            }
            else
            {
                _getFlagsOrBytesTransferred = GetFlagsOrBytesTransferredFromInt32;
                _setFlagsOrBytesTransferred = SetFlagsOrBytesTransferredAsInt32;
            }
        }

        private static unsafe ulong GetLength(byte* source, int index)
            => Pointer.DangerousToUInt32(source, index);

        private static unsafe void SetLength(byte* source, int index, ulong value)
            => Pointer.DangerousInsert(source, index, (uint)value);

        private static unsafe long GetFlagsOrBytesTransferredFromInt32(byte* source, int index)
            => Pointer.DangerousToInt32(source, index);

        private static unsafe void SetFlagsOrBytesTransferredAsInt32(byte* source, int index, long value)
            => Pointer.DangerousInsert(source, index, (int)value);

        private static unsafe long GetFlagsOrBytesTransferredFromUInt32(byte* source, int index)
            => Pointer.DangerousToUInt32(source, index);

        private static unsafe void SetFlagsOrBytesTransferredAsUInt32(byte* source, int index, long value)
            => Pointer.DangerousInsert(source, index, (uint)value);
    }
}
