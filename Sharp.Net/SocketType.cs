using Sharp.Helpers;
using System;
using System.Collections.Concurrent;

namespace Sharp.Net
{
    public class SocketType
    {
        #region Platform specific information

        private static unsafe delegate* unmanaged[Cdecl]<int> GetStreamSocketType { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetDatagramSocketType { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetRawSocketType { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetReliablyDeliveredMessagesSocketType { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetSequencedPacketSocketType { get; }

        #endregion Platform specific information

        private static ConcurrentDictionary<int, SocketType> Cache { get; }

        public static SocketType Stream { get; }
        public static SocketType Datagram { get; }
        public static SocketType Raw { get; }
        public static SocketType Rdm { get; }
        public static SocketType Seqpacket { get; }

		private readonly int _value;

        private SocketType(int value)
            => _value = value;

        static unsafe SocketType()
        {
            nint getStreamSocketTypePointer = Library.GetExport(nameof(Net), nameof(GetStreamSocketType));
            nint getDatagramSocketTypePointer = Library.GetExport(nameof(Net), nameof(GetDatagramSocketType));
            nint getRawSocketTypePointer = Library.GetExport(nameof(Net), nameof(GetRawSocketType));
            nint getReliablyDeliveredMessagesSocketTypePointer = Library.GetExport(nameof(Net), nameof(GetReliablyDeliveredMessagesSocketType));
            nint getSequencedPacketSocketTypePointer = Library.GetExport(nameof(Net), nameof(GetSequencedPacketSocketType));

            GetStreamSocketType = (delegate* unmanaged[Cdecl]<int>)getStreamSocketTypePointer;
            GetDatagramSocketType = (delegate* unmanaged[Cdecl]<int>)getDatagramSocketTypePointer;
            GetRawSocketType = (delegate* unmanaged[Cdecl]<int>)getRawSocketTypePointer;
            GetReliablyDeliveredMessagesSocketType = (delegate* unmanaged[Cdecl]<int>)getReliablyDeliveredMessagesSocketTypePointer;
            GetSequencedPacketSocketType = (delegate* unmanaged[Cdecl]<int>)getSequencedPacketSocketTypePointer;

            Stream = new SocketType(GetStreamSocketType());
            Datagram = new SocketType(GetDatagramSocketType());
            Raw = new SocketType(GetRawSocketType());
            Rdm = new SocketType(GetReliablyDeliveredMessagesSocketType());
            Seqpacket = new SocketType(GetSequencedPacketSocketType());

            Cache = new ConcurrentDictionary<int, SocketType>();
            Cache.TryAdd(Stream, Stream);
            Cache.TryAdd(Datagram, Datagram);
            Cache.TryAdd(Raw, Raw);
            Cache.TryAdd(Rdm, Rdm);
            Cache.TryAdd(Seqpacket, Seqpacket);
        }

        public static implicit operator int(SocketType socketType)
            => socketType._value;

        public static implicit operator SocketType(int value)
        {
            if (!Cache.TryGetValue(value, out SocketType? socketType))
                throw new InvalidCastException();

            return socketType;
        }
    }
}
