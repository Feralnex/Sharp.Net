using Sharp.Helpers;
using System;
using System.Collections.Concurrent;

namespace Sharp.Net
{
    public class SocketError
    {
        #region Platform specific information

        private static unsafe delegate* unmanaged[Cdecl]<int> GetNotConnectedSocketError { get; }

        #endregion Platform specific information

        private static ConcurrentDictionary<int, SocketError> Cache { get; }

        public static SocketError NotConnected { get; }

		private readonly int _value;

        private SocketError(int value)
            => _value = value;

        static unsafe SocketError()
        {
            nint getNotConnectedSocketErrorPointer = Library.GetExport(nameof(Net), nameof(GetNotConnectedSocketError));

            GetNotConnectedSocketError = (delegate* unmanaged[Cdecl]<int>)getNotConnectedSocketErrorPointer;

            NotConnected = new SocketError(GetNotConnectedSocketError());

            Cache = new ConcurrentDictionary<int, SocketError>();
            Cache.TryAdd(NotConnected, NotConnected);
        }

        public override string ToString()
            => _value.ToString();

        public static implicit operator int(SocketError pollEventCode)
            => pollEventCode._value;

        public static implicit operator SocketError(int value)
        {
            if (!Cache.TryGetValue(value, out SocketError? socketError))
                throw new InvalidCastException();

            return socketError;
        }
    }
}
