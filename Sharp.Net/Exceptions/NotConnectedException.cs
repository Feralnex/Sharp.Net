using Sharp.Exceptions;
using Sharp.Net.Localization;

namespace Sharp.Net.Exceptions
{
    public class NotConnectedException : PlatformException
    {
        public NotConnectedException(SocketError socketError) : base(socketError, string.Format(ExceptionMessages.NotConnected, socketError)) { }
    }
}
