using Sharp.Helpers;
using System;

namespace Sharp.Net.EndPoints
{
    public abstract class IPEndPoint : EndPoint
    {
        #region Platform specific information

        protected abstract int PortOffset { get; }
        protected abstract int AddressOffset { get; }
        protected abstract int AddressSize { get; }

        #endregion

        public unsafe ushort Port
        {
            get => Pointer.DangerousToUInt16(Content, PortOffset, bigEndian: true);
            set => Pointer.DangerousInsert(Content, PortOffset, value, bigEndian: true);
        }
        public abstract byte[] Address { get; set; }

        protected IPEndPoint(ushort addressFamily) : base(addressFamily) { }

        protected IPEndPoint(ushort addressFamily, byte[] address) : base(addressFamily)
        {
            Address = address;
        }

        protected IPEndPoint(ushort addressFamily, ushort port, byte[] address) : base(addressFamily)
        {
            Port = port;
            Address = address;
        }
    }
}