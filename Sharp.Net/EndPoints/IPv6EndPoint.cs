using Sharp.Collections.Extensions;
using Sharp.Exceptions;
using Sharp.Extensions;
using Sharp.Helpers;
using Sharp.Net.Exceptions;
using Sharp.Net.Extensions;
using System;

namespace Sharp.Net.EndPoints
{
    public class IPv6EndPoint : IPEndPoint
    {
        #region Platform specific information

        private static unsafe delegate* unmanaged[Cdecl]<int> GetIPv6EndPointMinimumSize { get; }
        private static unsafe delegate* unmanaged[Cdecl]<sbyte*, byte*, int*, int*, bool> TryParseIPv6Address { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetIPv6EndPointAddressFamilyOffset { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetIPv6EndPointPortOffset { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetIPv6EndPointFlowInformationOffset { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetIPv6EndPointAddressOffset { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetIPv6EndPointScopeIdOffset { get; }

        private static readonly int _minimumSize;
        private static readonly int _addressFamilyOffset;
        private static readonly int _portOffset;
        private static readonly int _flowInformationOffset;
        private static readonly int _addressOffset;
        private static readonly int _scopeIdOffset;
        private static readonly int _bufferAccessPosition;

        protected override int AddressFamilyOffset => _addressFamilyOffset;
        protected override int PortOffset => _portOffset;
        protected static int FlowInformationOffset => _flowInformationOffset;
        protected override int AddressOffset => _addressOffset;
        protected static int ScopeIdOffset => _scopeIdOffset;
        protected override int AddressSize => sizeof(decimal);

        public override int MinimumSize => _minimumSize;
        public override int ContentAccessPosition => _bufferAccessPosition;

        #endregion Platform specific information

        private static readonly byte[] _any;
        private static readonly byte[] _loopback;

        public static ReadOnlySpan<byte> Any => _any;
        public static ReadOnlySpan<byte> Loopback => _loopback;

        public override unsafe byte[] Address
        {
            get
            {
                decimal address = Pointer.DangerousToDecimal(Content, AddressOffset);

                return address.ToBytes();
            }
            set
            {
                decimal address = value.ToDecimal(0);

                Pointer.DangerousInsert(Content, AddressOffset, address);
            }
        }
        public unsafe uint FlowInformation
        {
            get => Pointer.DangerousToUInt32(Content, FlowInformationOffset, bigEndian: true);
            set => Pointer.DangerousInsert(Content, FlowInformationOffset, value, bigEndian: true);
        }
        public unsafe uint ScopeId
        {
            get => Pointer.DangerousToUInt32(Content, ScopeIdOffset, bigEndian: true);
            set => Pointer.DangerousInsert(Content, ScopeIdOffset, value, bigEndian: true);
        }

        public IPv6EndPoint() : base(Net.AddressFamily.IPv6) { }

        public IPv6EndPoint(ushort port) : base(Net.AddressFamily.IPv6)
        {
            Port = port;
        }

        public unsafe IPv6EndPoint(decimal address) : base(Net.AddressFamily.IPv6)
        {
            Pointer.DangerousInsert(Content, AddressOffset, address);
        }

        public IPv6EndPoint(ushort port, decimal address) : this(address)
        {
            Port = port;
        }

        public IPv6EndPoint(byte[] address) : this(0, address) { }

        public unsafe IPv6EndPoint(ReadOnlySpan<byte> address) : base(Net.AddressFamily.IPv6)
        {
            decimal addressAsValue = address.ToDecimal(0);

            Pointer.DangerousInsert(Content, AddressOffset, addressAsValue);
        }

        public IPv6EndPoint(ushort port, byte[] address) : base(Net.AddressFamily.IPv6, port, address) { }

        private unsafe IPv6EndPoint(ushort port, byte* addressPointer) : this(port)
        {
            decimal address = Pointer.DangerousToDecimal(addressPointer, 0);

            Pointer.DangerousInsert(Content, AddressOffset, address);
        }

        static unsafe IPv6EndPoint()
        {
            nint getIPv6EndPointMinimumSizePointer = Library.GetExport(nameof(Net), nameof(GetIPv6EndPointMinimumSize));
            nint tryParseIPv6AddressPointer = Library.GetExport(nameof(Net), nameof(TryParseIPv6Address));
            nint getIPv6EndPointAddressFamilyOffsetPointer = Library.GetExport(nameof(Net), nameof(GetIPv6EndPointAddressFamilyOffset));
            nint getIPv6EndPointPortOffsetPointer = Library.GetExport(nameof(Net), nameof(GetIPv6EndPointPortOffset));
            nint getIPv6EndPointFlowInformationOffsetPointer = Library.GetExport(nameof(Net), nameof(GetIPv6EndPointFlowInformationOffset));
            nint getIPv6EndPointAddressOffsetPointer = Library.GetExport(nameof(Net), nameof(GetIPv6EndPointAddressOffset));
            nint getIPv6EndPointScopeIdOffsetPointer = Library.GetExport(nameof(Net), nameof(GetIPv6EndPointScopeIdOffset));

            GetIPv6EndPointMinimumSize = (delegate* unmanaged[Cdecl]<int>)getIPv6EndPointMinimumSizePointer;
            TryParseIPv6Address = (delegate* unmanaged[Cdecl]<sbyte*, byte*, int*, int*, bool>)tryParseIPv6AddressPointer;
            GetIPv6EndPointAddressFamilyOffset = (delegate* unmanaged[Cdecl]<int>)getIPv6EndPointAddressFamilyOffsetPointer;
            GetIPv6EndPointPortOffset = (delegate* unmanaged[Cdecl]<int>)getIPv6EndPointPortOffsetPointer;
            GetIPv6EndPointFlowInformationOffset = (delegate* unmanaged[Cdecl]<int>)getIPv6EndPointFlowInformationOffsetPointer;
            GetIPv6EndPointAddressOffset = (delegate* unmanaged[Cdecl]<int>)getIPv6EndPointAddressOffsetPointer;
            GetIPv6EndPointScopeIdOffset = (delegate* unmanaged[Cdecl]<int>)getIPv6EndPointScopeIdOffsetPointer;

            _minimumSize = GetIPv6EndPointMinimumSize();
            _addressFamilyOffset = GetIPv6EndPointAddressFamilyOffset();
            _portOffset = GetIPv6EndPointPortOffset();
            _flowInformationOffset = GetIPv6EndPointFlowInformationOffset();
            _addressOffset = GetIPv6EndPointAddressOffset();
            _scopeIdOffset = GetIPv6EndPointScopeIdOffset();

            _bufferAccessPosition = _scopeIdOffset + sizeof(ushort);
            _any = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
            _loopback = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01];
        }

        public static unsafe bool TryCreate(string address, ushort port, out IPv6EndPoint? endPoint, out Exception? exception)
        {
            if (string.IsNullOrEmpty(address))
            {
                endPoint = default;
                exception = new ArgumentNullException(nameof(address));

                return false;
            }

            sbyte* nativeAddress = stackalloc sbyte[address.Length + 1];
            byte* parsedAddress = stackalloc byte[sizeof(decimal)];
            ReadOnlySpan<char> characters = address.AsSpan();
            characters.CopyTo(nativeAddress);

            int resultCode = default;
            int errorCode = default;
            bool parsed = TryParseIPv6Address(nativeAddress, parsedAddress, &resultCode, &errorCode);
            bool wrongAddressFormat = resultCode == 0;

            if (parsed)
            {
                endPoint = new IPv6EndPoint(port, parsedAddress);
                exception = default;
            }
            else
            {
                endPoint = default;

                if (wrongAddressFormat)
                    exception = new IPAddressFormatException(address);
                else
                    exception = PlatformException.FromCode(errorCode);
            }

            return parsed;
        }
    }
}