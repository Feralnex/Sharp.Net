# About
Sharp.Net is a low-level networking library built in C# that provides direct access to platform-specific socket and endpoint configurations. It leverages unsafe code, native memory, and function pointers to deliver high-performance networking primitives while maintaining a managed interface.

## Dependencies
Sharp.Net builds upon several foundational libraries to provide its functionality:

- [Sharp](https://github.com/Feralnex/Sharp)
- [Sharp.Collections](https://github.com/Feralnex/Sharp.Collections)
- [Sharp.Exceptions](https://github.com/Feralnex/Sharp.Exceptions)
- [Plus.Net](https://github.com/Feralnex/Plus.Net)

## Configuration
The `Configuration` class abstracts platform-specific socket details. It uses unmanaged function pointers to allocate configurations and retrieve offsets for address family, socket type, and protocol.

```csharp
public static Configuration IPv4(SocketType socketType)
{
    Key key = new Key(AddressFamily.IPv4, socketType, Protocol.IP);
    return Cache.GetOrAdd(key, IPv4Configuration.Create);
}
```

```csharp
public static Configuration IPv6(SocketType socketType, Protocol protocol)
{
    Key key = new Key(AddressFamily.IPv6, socketType, protocol);
    return Cache.GetOrAdd(key, IPv6Configuration.Create);
}
```

## EndPoint
The `EndPoint` class represents a network endpoint. It provides direct access to memory buffers and supports implicit conversions between native pointers and managed objects.

```csharp
public unsafe ushort AddressFamily
{
    get => Pointer.DangerousToUInt16(Content, AddressFamilyOffset);
    private set => Pointer.DangerousInsert(Content, AddressFamilyOffset, value);
}
```

## IPv4EndPoint
Specialized endpoint for IPv4 addresses. Includes parsing utilities and predefined constants for `Any` and `Loopback`.

```csharp
public static unsafe bool TryCreate(string address, ushort port, out IPv4EndPoint? endPoint, out Exception? exception)
{
    // Parses string into IPv4EndPoint
}
```

## IPv6EndPoint
Specialized endpoint for IPv6 addresses. Supports flow information and scope identifiers.

```csharp
public unsafe uint ScopeId
{
    get => Pointer.DangerousToUInt32(Content, ScopeIdOffset, bigEndian: true);
    set => Pointer.DangerousInsert(Content, ScopeIdOffset, value, bigEndian: true);
}
```

## Socket
The `Socket` class is the foundation of Sharp.Net. It manages native socket descriptors, initialization, binding, connecting, sending, and receiving. It integrates with platform-specific APIs like **WSAStartup** and **WSACleanup** to ensure proper lifecycle management.

Key properties:
- **Descriptor** – native pointer to the socket.
- **Configuration** – associated `Configuration` object (IPv4/IPv6, socket type, protocol).
- **Bound** – indicates whether the socket has been bound to an endpoint.
- **LocalEndPoint** – reference to the bound endpoint.

### Creating a Client
Clients are created using `Client.TryCreate`, which returns a boolean indicating success, along with the client instance or an exception.

```csharp
Configuration configuration = Configuration.IPv4(SocketType.Stream);
bool clientCreated = Client.TryCreate(configuration, out Client? client, out Exception? exception);

if (clientCreated)
{
    Console.WriteLine("Client created successfully.");
    client.TryClose(out Exception? tryCloseException);
}
```

### Binding a Client
You can bind a client either without specifying an endpoint (auto-allocation) or with a specific endpoint.

```csharp
// Without endpoint
bool bound = client.TryBind(out Exception? bindException);

// With endpoint
IPv4EndPoint.TryCreate("127.0.0.1", 3666, out IPv4EndPoint? endPoint, out _);
bool boundToEndpoint = client.TryBind(endPoint!, out Exception? bindException);
```

### Connecting to a Listener
Clients can connect to a listener using `TryConnect`. The unit tests demonstrate both successful and failed connections.

```csharp
// Listener endpoint
IPv4EndPoint listenerEndPoint = new IPv4EndPoint(3666);

// Attempt connection
bool connected = client.TryConnect(listenerEndPoint, out Exception? connectException);

if (connected)
    Console.WriteLine("Connected to listener.");
else
    Console.WriteLine($"Connection failed: {connectException?.Message}");
```

### Asynchronous Connect
Sharp.Net supports asynchronous connections via `BeginConnect`, with success and error callbacks.

```csharp
await client.BeginConnect(
    listenerEndPoint,
    (socket, remoteEndPoint) => Console.WriteLine("Connected asynchronously."),
    (socket, exception) => Console.WriteLine($"Connection failed: {exception.Message}")
);
```

### Sending and Receiving Data
Clients can send and receive both single bytes and arrays of bytes.

```csharp
// Sending a single byte
byte dataToSend = sizeof(uint);
bool sent = client.TrySend(dataToSend, 0, out Exception? sendException);

// Receiving a single byte
byte dataReceived = default;
bool received = client.TryReceive(ref dataReceived, 0, out Exception? receiveException);

// Sending multiple bytes
byte[] buffer = [sizeof(ushort), sizeof(uint), sizeof(ulong)];
bool sentBytes = client.TrySend(buffer, buffer.Length, 0, out int bytesSent, out Exception? sendException);

// Receiving multiple bytes
byte[] receiveBuffer = new byte[3];
bool receivedBytes = client.TryReceive(receiveBuffer, receiveBuffer.Length, 0, out int bytesReceived, out Exception? receiveException);
```

### Asynchronous Send and Receive
Asynchronous operations are supported via `BeginSend` and `BeginReceive`.

```csharp
await client.BeginSend(
    buffer, buffer.Length, 0,
    (socket, endPoint, buf, transferred) => Console.WriteLine($"Sent {transferred} bytes."),
    (socket, exception) => Console.WriteLine($"Send failed: {exception.Message}")
);

await client.BeginReceive(
    receiveBuffer, receiveBuffer.Length, 0,
    (socket, endPoint, buf, transferred) => Console.WriteLine($"Received {transferred} bytes."),
    (socket, exception) => Console.WriteLine($"Receive failed: {exception.Message}")
);
```

## Error Handling
Custom exceptions provide clear error reporting:

- **IPAddressFormatException** – thrown when an IP address string is invalid.
- **NotConnectedException** – thrown when a socket operation is attempted without a valid connection.