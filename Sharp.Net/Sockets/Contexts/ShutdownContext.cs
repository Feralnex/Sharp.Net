namespace Sharp.Net.Sockets.Contexts
{
    public class ShutdownContext : SocketContext<bool, ShutdownContext, ShutdownCallback>
    {
        public ShutdownContext() : base() { }

        public override void HandleCompletion()
            => CompletionCallback!(this);
    }
}
