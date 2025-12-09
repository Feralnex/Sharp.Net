using Sharp.Net.EndPoints;

namespace Sharp.Net.Configurations
{
    public abstract partial class Configuration
    {
        private class IPv4Configuration : Configuration
        {
            public IPv4Configuration(Key key) : base(key) { }

            public static Configuration Create(Key key)
                => new IPv4Configuration(key);

            public override IPv4EndPoint AllocateEndPoint()
                => new IPv4EndPoint();
        }

        public static Configuration IPv4(SocketType socketType)
        {
            Key key = new Key(AddressFamily.IPv4, socketType, Protocol.IP);

            return Cache.GetOrAdd(key, IPv4Configuration.Create);
        }

        public static Configuration IPv4(SocketType socketType, Protocol protocol)
        {
            Key key = new Key(AddressFamily.IPv4, socketType, protocol);

            return Cache.GetOrAdd(key, IPv4Configuration.Create);
        }
    }
}
