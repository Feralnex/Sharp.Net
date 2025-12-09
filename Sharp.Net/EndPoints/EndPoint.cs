using Sharp.Helpers;
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace Sharp.Net.EndPoints
{
    public partial class EndPoint
    {
        #region Platform specific information

        private static unsafe delegate* unmanaged[Cdecl]<int> GetEndPointMinimumSize { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetEndPointAddressFamilyOffset { get; }

        private static readonly int _minimumSize;
        private static readonly int _addressFamilyOffset;
        private static readonly int _contentAccessPosition;

        protected virtual int AddressFamilyOffset => _addressFamilyOffset;

        public virtual int MinimumSize => _minimumSize;
        public virtual int ContentAccessPosition => _contentAccessPosition;

        #endregion Platform specific information

        protected static ConcurrentDictionary<nint, EndPoint> ReverseCache { get; }

        private readonly nint _pointer;

        protected unsafe byte* Content => (byte*)_pointer;

        public int Size => MinimumSize;
        public unsafe ushort AddressFamily
        {
            get => Pointer.DangerousToUInt16(Content, AddressFamilyOffset);
            private set => Pointer.DangerousInsert(Content, AddressFamilyOffset, value);
        }

        public unsafe byte this[int index]
        {
            get => Content[ContentAccessPosition + index];
            set => Content[ContentAccessPosition + index] = value;
        }

        public EndPoint() : this(Net.AddressFamily.Unspecified) { }

        public unsafe EndPoint(ushort addressFamily)
        {
            _pointer = (nint)NativeMemory.AllocZeroed((nuint)MinimumSize);

            ReverseCache.TryAdd(_pointer, this);

            AddressFamily = addressFamily;
        }

        static unsafe EndPoint()
        {
            nint getEndPointMinimumSizePointer = Library.GetExport(nameof(Net), nameof(GetEndPointMinimumSize));
            nint getEndPointAddressFamilyOffsetPointer = Library.GetExport(nameof(Net), nameof(GetEndPointAddressFamilyOffset));

            GetEndPointMinimumSize = (delegate* unmanaged[Cdecl]<int>)getEndPointMinimumSizePointer;
            GetEndPointAddressFamilyOffset = (delegate* unmanaged[Cdecl]<int>)getEndPointAddressFamilyOffsetPointer;

            _minimumSize = GetEndPointMinimumSize();
            _addressFamilyOffset = GetEndPointAddressFamilyOffset();
            _contentAccessPosition = _addressFamilyOffset + sizeof(ushort);

            ReverseCache = new ConcurrentDictionary<nint, EndPoint>();
        }

        unsafe ~EndPoint()
        {
            ReverseCache.TryRemove(_pointer, out _);

            NativeMemory.Free(Content);
        }

        public static implicit operator EndPoint(nint pointer)
        {
            if (!ReverseCache.TryGetValue(pointer, out EndPoint? endPoint))
                throw new InvalidCastException();

            return endPoint;
        }

        public static implicit operator nint(EndPoint endPoint)
            => endPoint._pointer;
    }
}