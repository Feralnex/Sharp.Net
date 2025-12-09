using Sharp.Helpers;
using Sharp.Net.EndPoints;
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace Sharp.Net.Configurations
{
    public abstract partial class Configuration
    {
        #region Platform specific information

        private static unsafe delegate* unmanaged[Cdecl]<ushort, int, int, nint> NewConfiguration { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetConfigurationAddressFamilyOffset { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetConfigurationSocketTypeOffset { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetConfigurationProtocolOffset { get; }

        private static readonly int _addressFamilyOffset;
        private static readonly int _socketTypeOffset;
        private static readonly int _protocolOffset;

        #endregion Platform specific information

        protected readonly record struct Key(ushort AddressFamily, int SocketType, int Protocol);

        protected static ConcurrentDictionary<Key, Configuration> Cache { get; }
        protected static ConcurrentDictionary<nint, Configuration> ReverseCache { get; }

        private readonly nint _pointer;
        private unsafe byte* Content => (byte*)_pointer;
        public unsafe AddressFamily AddressFamily => Pointer.DangerousToUInt16(Content, _addressFamilyOffset);
        public unsafe SocketType SocketType => Pointer.DangerousToInt32(Content, _socketTypeOffset);
        public unsafe Protocol Protocol => Pointer.DangerousToInt32(Content, _protocolOffset);

        protected unsafe Configuration(Key key)
        {
            _pointer = NewConfiguration(key.AddressFamily, key.SocketType, key.Protocol);

            Cache.TryAdd(key, this);
            ReverseCache.TryAdd(_pointer, this);
        }

        static unsafe Configuration()
        {
            nint newConfigurationPointer = Library.GetExport(nameof(Net), nameof(NewConfiguration));
            nint getConfigurationAddressFamilyOffsetPointer = Library.GetExport(nameof(Net), nameof(GetConfigurationAddressFamilyOffset));
            nint getConfigurationSocketTypeOffsetPointer = Library.GetExport(nameof(Net), nameof(GetConfigurationSocketTypeOffset));
            nint getConfigurationProtocolOffsetPointer = Library.GetExport(nameof(Net), nameof(GetConfigurationProtocolOffset));

            NewConfiguration = (delegate* unmanaged[Cdecl]<ushort, int, int, nint>)newConfigurationPointer;
            GetConfigurationAddressFamilyOffset = (delegate* unmanaged[Cdecl]<int>)getConfigurationAddressFamilyOffsetPointer;
            GetConfigurationSocketTypeOffset = (delegate* unmanaged[Cdecl]<int>)getConfigurationSocketTypeOffsetPointer;
            GetConfigurationProtocolOffset = (delegate* unmanaged[Cdecl]<int>)getConfigurationProtocolOffsetPointer;

            _addressFamilyOffset = GetConfigurationAddressFamilyOffset();
            _socketTypeOffset = GetConfigurationSocketTypeOffset();
            _protocolOffset = GetConfigurationProtocolOffset();

            Cache = new ConcurrentDictionary<Key, Configuration>();
            ReverseCache = new ConcurrentDictionary<nint, Configuration>();
        }

        unsafe ~Configuration()
        {
            ReverseCache.TryRemove(_pointer, out _);

            NativeMemory.Free(Content);
        }

        public static implicit operator Configuration(nint pointer)
        {
            if (!ReverseCache.TryGetValue(pointer, out Configuration? configuration))
                throw new InvalidCastException();

            return configuration;
        }

        public static implicit operator nint(Configuration configuration)
            => configuration._pointer;

        public abstract EndPoint AllocateEndPoint();
    }
}
