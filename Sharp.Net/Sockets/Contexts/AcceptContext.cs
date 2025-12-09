using Sharp.Helpers;

namespace Sharp.Net.Sockets.Contexts
{
    public class AcceptContext : ConnectContext<Client, AcceptContext, AcceptCallback>
    {
        #region Platform specific information

        private static unsafe delegate* unmanaged[Cdecl]<nint> NewAcceptContext { get; }
        private static unsafe delegate* unmanaged[Cdecl]<nint, void> DeleteAcceptContext { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetAcceptContextRemoteSocketOffset { get; }

        private static readonly int _remoteSocketOffset;

        #endregion Platform specific information

        protected unsafe override delegate* unmanaged[Cdecl]<nint, void> Destructor => DeleteAcceptContext;

        public unsafe nint RemoteSocket => Pointer.DangerousToNInt(Content, _remoteSocketOffset);

        public unsafe AcceptContext() : base(NewAcceptContext()) { }

        static unsafe AcceptContext()
        {
            nint newAcceptContextPointer = Library.GetExport(nameof(Net), nameof(NewAcceptContext));
            nint deleteAcceptContextPointer = Library.GetExport(nameof(Net), nameof(DeleteAcceptContext));
            nint getAcceptContextRemoteSocketOffsetPointer = Library.GetExport(nameof(Net), nameof(GetAcceptContextRemoteSocketOffset));

            NewAcceptContext = (delegate* unmanaged[Cdecl]<nint>)newAcceptContextPointer;
            DeleteAcceptContext = (delegate* unmanaged[Cdecl]<nint, void>)deleteAcceptContextPointer;
            GetAcceptContextRemoteSocketOffset = (delegate* unmanaged[Cdecl]<int>)getAcceptContextRemoteSocketOffsetPointer;

            _remoteSocketOffset = GetAcceptContextRemoteSocketOffset();
        }

        public override void HandleCompletion()
            => CompletionCallback!(this);
    }
}
