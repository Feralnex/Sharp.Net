using Sharp.Helpers;
using System;
using System.Collections.Concurrent;

namespace Sharp.Net
{
    public class Protocol
    {
        #region Platform specific information

        private static unsafe delegate* unmanaged[Cdecl]<ushort> GetIPProtocol { get; }
        private static unsafe delegate* unmanaged[Cdecl]<ushort> GetIcmpProtocol { get; }
        private static unsafe delegate* unmanaged[Cdecl]<ushort> GetIgmpProtocol { get; }
        private static unsafe delegate* unmanaged[Cdecl]<ushort> GetIPv4Protocol { get; }
        private static unsafe delegate* unmanaged[Cdecl]<ushort> GetTcpProtocol { get; }
        private static unsafe delegate* unmanaged[Cdecl]<ushort> GetPupProtocol { get; }
        private static unsafe delegate* unmanaged[Cdecl]<ushort> GetUdpProtocol { get; }
        private static unsafe delegate* unmanaged[Cdecl]<ushort> GetIdpProtocol { get; }
        private static unsafe delegate* unmanaged[Cdecl]<ushort> GetIPv6Protocol { get; }
        private static unsafe delegate* unmanaged[Cdecl]<ushort> GetIPv6RoutingHeaderProtocol { get; }
        private static unsafe delegate* unmanaged[Cdecl]<ushort> GetIPv6FragmentHeaderProtocol { get; }
        private static unsafe delegate* unmanaged[Cdecl]<ushort> GetIPSecEncapsulatingSecurityPayloadProtocol { get; }
        private static unsafe delegate* unmanaged[Cdecl]<ushort> GetIPSecAuthenticationHeaderProtocol { get; }
        private static unsafe delegate* unmanaged[Cdecl]<ushort> GetIcmpV6Protocol { get; }
        private static unsafe delegate* unmanaged[Cdecl]<ushort> GetIPv6NoNextHeaderProtocol { get; }
        private static unsafe delegate* unmanaged[Cdecl]<ushort> GetIPv6DestinationOptionsProtocol { get; }
        private static unsafe delegate* unmanaged[Cdecl]<ushort> GetRawProtocol { get; }

        #endregion Platform specific information

        private static ConcurrentDictionary<int, Protocol> Cache { get; }

        public static Protocol IP { get; }
        public static Protocol Icmp { get; }
        public static Protocol Igmp { get; }
        public static Protocol IPv4 { get; }
        public static Protocol Tcp { get; }
        public static Protocol Pup { get; }
        public static Protocol Udp { get; }
        public static Protocol Idp { get; }
        public static Protocol IPv6 { get; }
        public static Protocol StreamIPv6RoutingHeader { get; }
        public static Protocol IPv6FragmentHeader { get; }
        public static Protocol IPSecEncapsulatingSecurityPayload { get; }
        public static Protocol IPSecAuthenticationHeader { get; }
        public static Protocol IcmpV6 { get; }
        public static Protocol IPv6NoNextHeader { get; }
        public static Protocol IPv6DestinationOptions { get; }
        public static Protocol Raw { get; }

		private readonly int _value;

        private Protocol(int value)
            => _value = value;

        static unsafe Protocol()
        {
            nint getIPProtocolPointer = Library.GetExport(nameof(Net), nameof(GetIPProtocol));
            nint getIcmpProtocolPointer = Library.GetExport(nameof(Net), nameof(GetIcmpProtocol));
            nint getIgmpProtocolPointer = Library.GetExport(nameof(Net), nameof(GetIgmpProtocol));
            nint getIPv4ProtocolPointer = Library.GetExport(nameof(Net), nameof(GetIPv4Protocol));
            nint getTcpProtocolPointer = Library.GetExport(nameof(Net), nameof(GetTcpProtocol));
            nint getPupProtocolPointer = Library.GetExport(nameof(Net), nameof(GetPupProtocol));
            nint getUdpProtocolPointer = Library.GetExport(nameof(Net), nameof(GetUdpProtocol));
            nint getIdpProtocolPointer = Library.GetExport(nameof(Net), nameof(GetIdpProtocol));
            nint getIPv6ProtocolPointer = Library.GetExport(nameof(Net), nameof(GetIPv6Protocol));
            nint getIPv6RoutingHeaderProtocolPointer = Library.GetExport(nameof(Net), nameof(GetIPv6RoutingHeaderProtocol));
            nint getIPv6FragmentHeaderProtocolPointer = Library.GetExport(nameof(Net), nameof(GetIPv6FragmentHeaderProtocol));
            nint getIPSecEncapsulatingSecurityPayloadProtocolPointer = Library.GetExport(nameof(Net), nameof(GetIPSecEncapsulatingSecurityPayloadProtocol));
            nint getIPSecAuthenticationHeaderProtocolPointer = Library.GetExport(nameof(Net), nameof(GetIPSecAuthenticationHeaderProtocol));
            nint getIcmpV6ProtocolPointer = Library.GetExport(nameof(Net), nameof(GetIcmpV6Protocol));
            nint getIPv6NoNextHeaderProtocolPointer = Library.GetExport(nameof(Net), nameof(GetIPv6NoNextHeaderProtocol));
            nint getIPv6DestinationOptionsProtocolPointer = Library.GetExport(nameof(Net), nameof(GetIPv6DestinationOptionsProtocol));
            nint getRawProtocolPointer = Library.GetExport(nameof(Net), nameof(GetRawProtocol));

            GetIPProtocol = (delegate* unmanaged[Cdecl]<ushort>)getIPProtocolPointer;
            GetIcmpProtocol = (delegate* unmanaged[Cdecl]<ushort>)getIcmpProtocolPointer;
            GetIgmpProtocol = (delegate* unmanaged[Cdecl]<ushort>)getIgmpProtocolPointer;
            GetIPv4Protocol = (delegate* unmanaged[Cdecl]<ushort>)getIPv4ProtocolPointer;
            GetTcpProtocol = (delegate* unmanaged[Cdecl]<ushort>)getTcpProtocolPointer;
            GetPupProtocol = (delegate* unmanaged[Cdecl]<ushort>)getPupProtocolPointer;
            GetUdpProtocol = (delegate* unmanaged[Cdecl]<ushort>)getUdpProtocolPointer;
            GetIdpProtocol = (delegate* unmanaged[Cdecl]<ushort>)getIdpProtocolPointer;
            GetIPv6Protocol = (delegate* unmanaged[Cdecl]<ushort>)getIPv6ProtocolPointer;
            GetIPv6RoutingHeaderProtocol = (delegate* unmanaged[Cdecl]<ushort>)getIPv6RoutingHeaderProtocolPointer;
            GetIPv6FragmentHeaderProtocol = (delegate* unmanaged[Cdecl]<ushort>)getIPv6FragmentHeaderProtocolPointer;
            GetIPSecEncapsulatingSecurityPayloadProtocol = (delegate* unmanaged[Cdecl]<ushort>)getIPSecEncapsulatingSecurityPayloadProtocolPointer;
            GetIPSecAuthenticationHeaderProtocol = (delegate* unmanaged[Cdecl]<ushort>)getIPSecAuthenticationHeaderProtocolPointer;
            GetIcmpV6Protocol = (delegate* unmanaged[Cdecl]<ushort>)getIcmpV6ProtocolPointer;
            GetIPv6NoNextHeaderProtocol = (delegate* unmanaged[Cdecl]<ushort>)getIPv6NoNextHeaderProtocolPointer;
            GetIPv6DestinationOptionsProtocol = (delegate* unmanaged[Cdecl]<ushort>)getIPv6DestinationOptionsProtocolPointer;
            GetRawProtocol = (delegate* unmanaged[Cdecl]<ushort>)getRawProtocolPointer;

            IP = new Protocol(GetIPProtocol());
            Icmp = new Protocol(GetIcmpProtocol());
            Igmp = new Protocol(GetIgmpProtocol());
            IPv4 = new Protocol(GetIPv4Protocol());
            Tcp = new Protocol(GetTcpProtocol());
            Pup = new Protocol(GetPupProtocol());
            Udp = new Protocol(GetUdpProtocol());
            Idp = new Protocol(GetIdpProtocol());
            IPv6 = new Protocol(GetIPv6Protocol());
            StreamIPv6RoutingHeader = new Protocol(GetIPv6RoutingHeaderProtocol());
            IPv6FragmentHeader = new Protocol(GetIPv6FragmentHeaderProtocol());
            IPSecEncapsulatingSecurityPayload = new Protocol(GetIPSecEncapsulatingSecurityPayloadProtocol());
            IPSecAuthenticationHeader = new Protocol(GetIPSecAuthenticationHeaderProtocol());
            IcmpV6 = new Protocol(GetIcmpV6Protocol());
            IPv6NoNextHeader = new Protocol(GetIPv6NoNextHeaderProtocol());
            IPv6DestinationOptions = new Protocol(GetIPv6DestinationOptionsProtocol());
            Raw = new Protocol(GetRawProtocol());

            Cache = new ConcurrentDictionary<int, Protocol>();
            Cache.TryAdd(IP, IP);
            Cache.TryAdd(Icmp, Icmp);
            Cache.TryAdd(Igmp, Igmp);
            Cache.TryAdd(IPv4, IPv4);
            Cache.TryAdd(Tcp, Tcp);
            Cache.TryAdd(Pup, Pup);
            Cache.TryAdd(Udp, Udp);
            Cache.TryAdd(Idp, Idp);
            Cache.TryAdd(IPv6, IPv6);
            Cache.TryAdd(StreamIPv6RoutingHeader, StreamIPv6RoutingHeader);
            Cache.TryAdd(IPv6FragmentHeader, IPv6FragmentHeader);
            Cache.TryAdd(IPSecEncapsulatingSecurityPayload, IPSecEncapsulatingSecurityPayload);
            Cache.TryAdd(IPSecAuthenticationHeader, IPSecAuthenticationHeader);
            Cache.TryAdd(IcmpV6, IcmpV6);
            Cache.TryAdd(IPv6NoNextHeader, IPv6NoNextHeader);
            Cache.TryAdd(IPv6DestinationOptions, IPv6DestinationOptions);
            Cache.TryAdd(Raw, Raw);
        }

        public static implicit operator int(Protocol protocol)
            => protocol._value;

        public static implicit operator Protocol(int value)
        {
            if (!Cache.TryGetValue(value, out Protocol? protocol))
                throw new InvalidCastException();

            return protocol;
        }
    }
}
