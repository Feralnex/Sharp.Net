using Sharp.Net.EndPoints;

namespace Sharp.Net.Configurations
{
    public abstract partial class Configuration
    {
        private class IPv6Configuration : Configuration
        {
            public IPv6Configuration(Key key) : base(key) { }

            public static Configuration Create(Key key)
                => new IPv6Configuration(key);

            public override IPv6EndPoint AllocateEndPoint()
                => new IPv6EndPoint();
        }

        public static Configuration IPv6(SocketType socketType)
        {
            Key key = new Key(AddressFamily.IPv6, socketType, Protocol.IP);

            return Cache.GetOrAdd(key, IPv6Configuration.Create);
        }

        public static Configuration IPv6(SocketType socketType, Protocol protocol)
        {
            Key key = new Key(AddressFamily.IPv6, socketType, protocol);

            return Cache.GetOrAdd(key, IPv6Configuration.Create);
        }
    }
}
