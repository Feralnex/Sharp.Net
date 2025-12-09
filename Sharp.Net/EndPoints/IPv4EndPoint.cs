using Sharp.Collections;
using Sharp.Collections.Extensions;
using Sharp.Exceptions;
using Sharp.Extensions;
using Sharp.Helpers;
using Sharp.Net.Exceptions;
using Sharp.Net.Extensions;
using System;

namespace Sharp.Net.EndPoints
{
    public class IPv4EndPoint : IPEndPoint
    {
        #region Platform specific information

        private static unsafe delegate* unmanaged[Cdecl]<int> GetIPv4EndPointMinimumSize { get; }
        private static unsafe delegate* unmanaged[Cdecl]<sbyte*, byte*, int*, int*, bool> TryParseIPv4Address { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetIPv4EndPointAddressFamilyOffset { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetIPv4EndPointPortOffset { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetIPv4EndPointAddressOffset { get; }

        private static readonly int _minimumSize;
        private static readonly int _addressFamilyOffset;
        private static readonly int _portOffset;
        private static readonly int _addressOffset;
        private static readonly int _bufferAccessPosition;

        protected override int AddressFamilyOffset => _addressFamilyOffset;
        protected override int PortOffset => _portOffset;
        protected override int AddressOffset => _addressOffset;
        protected override int AddressSize => sizeof(uint);

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
                uint address = Pointer.DangerousToUInt32(Content, AddressOffset);

                return address.ToBytes();
            }
            set
            {
                uint address = value.ToUInt32(0);

                Pointer.DangerousInsert(Content, AddressOffset, address);
            }
        }

        public IPv4EndPoint() : base(Net.AddressFamily.IPv4) { }

        public IPv4EndPoint(ushort port) : base(Net.AddressFamily.IPv4)
        {
            Port = port;
        }

        public unsafe IPv4EndPoint(uint address) : base(Net.AddressFamily.IPv4)
        {
            Pointer.DangerousInsert(Content, AddressOffset, address);
        }

        public IPv4EndPoint(ushort port, uint address) : this(address)
        {
            Port = port;
        }

        public IPv4EndPoint(byte[] address) : base(Net.AddressFamily.IPv4, address) { }

        public unsafe IPv4EndPoint(ReadOnlySpan<byte> address) : base(Net.AddressFamily.IPv4)
        {
            uint addressAsValue = address.ToUInt32(0);

            Pointer.DangerousInsert(Content, AddressOffset, addressAsValue);
        }

        public IPv4EndPoint(ushort port, byte[] address) : base(Net.AddressFamily.IPv4, port, address) { }

        private unsafe IPv4EndPoint(ushort port, byte* addressPointer) : this(port)
        {
            uint address = Pointer.DangerousToUInt32(addressPointer, 0);

            Pointer.DangerousInsert(Content, AddressOffset, address);
        }

        static unsafe IPv4EndPoint()
        {
            nint getIPv4EndPointMinimumSizePointer = Library.GetExport(nameof(Net), nameof(GetIPv4EndPointMinimumSize));
            nint tryParseIPv4AddressPointer = Library.GetExport(nameof(Net), nameof(TryParseIPv4Address));
            nint getIPv4EndPointAddressFamilyOffsetPointer = Library.GetExport(nameof(Net), nameof(GetIPv4EndPointAddressFamilyOffset));
            nint getIPv4EndPointPortOffsetPointer = Library.GetExport(nameof(Net), nameof(GetIPv4EndPointPortOffset));
            nint getIPv4EndPointAddressOffsetPointer = Library.GetExport(nameof(Net), nameof(GetIPv4EndPointAddressOffset));

            GetIPv4EndPointMinimumSize = (delegate* unmanaged[Cdecl]<int>)getIPv4EndPointMinimumSizePointer;
            TryParseIPv4Address = (delegate* unmanaged[Cdecl]<sbyte*, byte*, int*, int*, bool>)tryParseIPv4AddressPointer;
            GetIPv4EndPointAddressFamilyOffset = (delegate* unmanaged[Cdecl]<int>)getIPv4EndPointAddressFamilyOffsetPointer;
            GetIPv4EndPointPortOffset = (delegate* unmanaged[Cdecl]<int>)getIPv4EndPointPortOffsetPointer;
            GetIPv4EndPointAddressOffset = (delegate* unmanaged[Cdecl]<int>)getIPv4EndPointAddressOffsetPointer;

            _minimumSize = GetIPv4EndPointMinimumSize();
            _addressFamilyOffset = GetIPv4EndPointAddressFamilyOffset();
            _portOffset = GetIPv4EndPointPortOffset();
            _addressOffset = GetIPv4EndPointAddressOffset();
            _bufferAccessPosition = _addressOffset + sizeof(uint);

            _any = 0x00000000.ToBytes(bigEndian: true);
            _loopback = 0x7f000001.ToBytes(bigEndian: true);
        }

        public static unsafe bool TryCreate(string address, ushort port, out IPv4EndPoint? endPoint, out Exception? exception)
        {
            if (string.IsNullOrEmpty(address))
            {
                endPoint = default;
                exception = new ArgumentNullException(nameof(address));

                return false;
            }

            sbyte* nativeAddress = stackalloc sbyte[address.Length + 1];
            byte* parsedAddress = stackalloc byte[sizeof(uint)];
            ReadOnlySpan<char> characters = address.AsSpan();
            characters.CopyTo(nativeAddress);

            int resultCode = default;
            int errorCode = default;
            bool parsed = TryParseIPv4Address(nativeAddress, parsedAddress, &resultCode, &errorCode);
            bool wrongAddressFormat = resultCode == 0;

            if (parsed)
            {
                endPoint = new IPv4EndPoint(port, parsedAddress);
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