using Sharp.Helpers;
using System.Runtime.InteropServices;

namespace Sharp.Net.Sockets.Contexts
{
    public class NodeContext : ClientContext<int, NodeContext, TransferCallback>
    {
        #region Platform specific information

        private static unsafe delegate* unmanaged[Cdecl]<nint> NewNodeContext { get; }
        private static unsafe delegate* unmanaged[Cdecl]<nint, void> DeleteNodeContext { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetNodeContextEndPointOffset { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetNodeContextEndPointLengthOffset { get; }

        private static readonly int _endPointOffset;
        private static readonly int _endPointLengthOffset;
        private static readonly GetInt64 _getEndPointLength;

        #endregion Platform specific information

        protected unsafe override delegate* unmanaged[Cdecl]<nint, void> Destructor => DeleteNodeContext;

        public unsafe nint EndPoint
        {
            get => Pointer.DangerousToNInt(Content, _endPointOffset);
            set => Pointer.DangerousInsert(Content, _endPointOffset, value);
        }
        public unsafe long EndPointLength
        {
            get => _getEndPointLength(Content, _endPointLengthOffset);
            set => Pointer.DangerousInsert(Content, _endPointLengthOffset, value);
        }

        public unsafe NodeContext() : base(NewNodeContext()) { }

        static unsafe NodeContext()
        {
            nint newNodeContextPointer = Library.GetExport(nameof(Net), nameof(NewNodeContext));
            nint deleteNodeContextPointer = Library.GetExport(nameof(Net), nameof(DeleteNodeContext));
            nint getNodeContextEndPointOffsetPointer = Library.GetExport(nameof(Net), nameof(GetNodeContextEndPointOffset));
            nint getNodeContextEndPointLengthOffsetPointer = Library.GetExport(nameof(Net), nameof(GetNodeContextEndPointLengthOffset));

            NewNodeContext = (delegate* unmanaged[Cdecl]<nint>)newNodeContextPointer;
            DeleteNodeContext = (delegate* unmanaged[Cdecl]<nint, void>)deleteNodeContextPointer;
            GetNodeContextEndPointOffset = (delegate* unmanaged[Cdecl]<int>)getNodeContextEndPointOffsetPointer;
            GetNodeContextEndPointLengthOffset = (delegate* unmanaged[Cdecl]<int>)getNodeContextEndPointLengthOffsetPointer;

            _endPointOffset = GetNodeContextEndPointOffset();
            _endPointLengthOffset = GetNodeContextEndPointLengthOffset();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                _getEndPointLength = GetEndPointLengthFromInt32;
            else
                _getEndPointLength = GetEndPointLengthFromUInt32;
        }

        public override void HandleCompletion()
            => CompletionCallback!(this);

        private static unsafe long GetEndPointLengthFromInt32(byte* source, int index)
            => Pointer.DangerousToInt32(source, index);

        private static unsafe long GetEndPointLengthFromUInt32(byte* source, int index)
            => Pointer.DangerousToUInt32(source, index);
    }
}
