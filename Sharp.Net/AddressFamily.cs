using Sharp.Helpers;
using System;
using System.Collections.Concurrent;

namespace Sharp.Net
{
    public class AddressFamily
    {
        #region Platform specific information

        private static unsafe delegate* unmanaged[Cdecl]<ushort> GetUnspecifiedAddressFamily { get; }
        private static unsafe delegate* unmanaged[Cdecl]<ushort> GetIPv4AddressFamily { get; }
        private static unsafe delegate* unmanaged[Cdecl]<ushort> GetIPv6AddressFamily { get; }

        #endregion Platform specific information

        private static ConcurrentDictionary<ushort, AddressFamily> Cache { get; }

        public static AddressFamily Unspecified { get; }
        public static AddressFamily IPv4 { get; }
        public static AddressFamily IPv6 { get; }

        private readonly ushort _value;

        private AddressFamily(ushort value)
            => _value = value;

        static unsafe AddressFamily()
        {
            nint getUnspecifiedAddressFamilyPointer = Library.GetExport(nameof(Net), nameof(GetUnspecifiedAddressFamily));
            nint getIPv4AddressFamilyPointer = Library.GetExport(nameof(Net), nameof(GetIPv4AddressFamily));
            nint getIPv6AddressFamilyPointer = Library.GetExport(nameof(Net), nameof(GetIPv4AddressFamily));

            GetUnspecifiedAddressFamily = (delegate* unmanaged[Cdecl]<ushort>)getUnspecifiedAddressFamilyPointer;
            GetIPv4AddressFamily = (delegate* unmanaged[Cdecl]<ushort>)getIPv4AddressFamilyPointer;
            GetIPv6AddressFamily = (delegate* unmanaged[Cdecl]<ushort>)getIPv6AddressFamilyPointer;

            Unspecified = new AddressFamily(GetUnspecifiedAddressFamily());
            IPv4 = new AddressFamily(GetIPv4AddressFamily());
            IPv6 = new AddressFamily(GetIPv6AddressFamily());

            Cache = new ConcurrentDictionary<ushort, AddressFamily>();
            Cache.TryAdd(Unspecified, Unspecified);
            Cache.TryAdd(IPv4, IPv4);
            Cache.TryAdd(IPv6, IPv6);
        }

        public static implicit operator ushort(AddressFamily addressFamily)
            => addressFamily._value;

        public static implicit operator AddressFamily(ushort value)
        {
            if (!Cache.TryGetValue(value, out AddressFamily? addressFamily))
                throw new InvalidCastException();

            return addressFamily;
        }
    }
}
