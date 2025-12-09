using Sharp.Net.Configurations;
using Sharp.Net.EndPoints;
using Sharp.Net.Sockets;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sharp.Net.Tests
{
    public class ClientTests
    {
        [Fact]
        public void TryCreate_WhenUsedWithSupportedConfigurationOfSocketType_ShouldReturnTrueAndAssignClient()
        {
            // Arrange
            Configuration configuration = Configuration.IPv4(SocketType.Stream);

            // Act
            bool clientCreated = Client.TryCreate(configuration, out Client? client, out Exception? tryCreateException);
            bool clientClosed = client!.TryClose(out Exception? tryCloseException);

            // Assert
            Assert.True(clientCreated);
            Assert.True(clientClosed);
            Assert.NotNull(client);
            Assert.Null(tryCreateException);
            Assert.Null(tryCloseException);
        }

        [Fact]
        public void TryCreate_WhenUsedWithSupportedConfigurationOfSocketTypeAndProtocol_ShouldReturnTrueAndAssignClient()
        {
            // Arrange
            Configuration configuration = Configuration.IPv4(SocketType.Stream, Protocol.Tcp);

            // Act
            bool clientCreated = Client.TryCreate(configuration, out Client? client, out Exception? tryCreateException);
            bool clientClosed = client!.TryClose(out Exception? tryCloseException);

            // Assert
            Assert.True(clientCreated);
            Assert.True(clientClosed);
            Assert.NotNull(client);
            Assert.Null(tryCreateException);
            Assert.Null(tryCloseException);
        }

        [Fact]
        public void TryCreate_WhenUsedWithNotSupportedConfigurationOfSocketTypeAndProtocol_ShouldReturnFalseAndAssignAnException()
        {
            // Arrange
            Configuration configuration = Configuration.IPv4(SocketType.Stream, Protocol.Udp);

            // Act
            bool clientCreated = Client.TryCreate(configuration, out Client? client, out Exception? tryCreateException);

            // Assert
            Assert.False(clientCreated);
            Assert.Null(client);
            Assert.NotNull(tryCreateException);
        }

        [Fact]
        public void TryBind_WhenUsedWithoutEndPoint_ShouldReturnTrue()
        {
            // Arrange
            Configuration configuration = Configuration.IPv4(SocketType.Stream);
            bool clientCreated = Client.TryCreate(configuration, out Client? client, out Exception? tryCreateException);

            // Act
            bool clientBound = client!.TryBind(out Exception? tryBindException);
            bool clientClosed = client.TryClose(out Exception? tryCloseException);

            // Assert
            Assert.True(clientCreated);
            Assert.True(clientBound);
            Assert.True(clientClosed);
            Assert.NotNull(client);
            Assert.Null(tryCreateException);
            Assert.Null(tryBindException);
            Assert.Null(tryCloseException);
        }

        [Fact]
        public void TryBind_WhenUsedWithEndPoint_ShouldReturnTrue()
        {
            // Arrange
            Configuration configuration = Configuration.IPv4(SocketType.Stream);
            bool endPointCreated = IPv4EndPoint.TryCreate("127.0.0.1", 3666, out IPv4EndPoint? endPoint, out Exception? tryCreateEndPointException);
            bool clientCreated = Client.TryCreate(configuration, out Client? client, out Exception? tryCreateClientException);

            // Act
            bool clientBound = client!.TryBind(endPoint!, out Exception? tryBindException);
            bool clientClosed = client.TryClose(out Exception? tryCloseException);

            // Assert
            Assert.True(endPointCreated);
            Assert.True(clientCreated);
            Assert.True(clientBound);
            Assert.True(clientClosed);
            Assert.NotNull(endPoint);
            Assert.NotNull(client);
            Assert.Null(tryCreateEndPointException);
            Assert.Null(tryCreateClientException);
            Assert.Null(tryBindException);
            Assert.Null(tryCloseException);
        }

        [Fact]
        public async Task TryConnect_WhenSuccessfullyConnectedToListener_ShouldReturnTrue()
        {
            // Arrange
            Configuration configuration = Configuration.IPv4(SocketType.Stream);
            IPv4EndPoint listenerLocalEndPoint = new IPv4EndPoint(3666);
            bool localClientCreated = Client.TryCreate(configuration, out Client? localClient, out Exception? tryCreateException);
            SemaphoreSlim semaphore = new SemaphoreSlim(0);
            void onAccepted(Client.Listener listener, Client remoteClient)
            {
                // Arrange & Act
                bool localClientClosed = localClient!.TryClose(out Exception? tryCloseLocalClientException);
                bool remoteClientClosed = remoteClient!.TryClose(out Exception? tryCloseRemoteClientException);
                bool listenerClosed = listener.TryClose(out Exception? tryCloseListenerException);

                // Assert
                Assert.True(localClientClosed);
                Assert.True(remoteClientClosed);
                Assert.True(listenerClosed);
                Assert.NotNull(localClient);
                Assert.NotNull(remoteClient);
                Assert.NotNull(listener);
                Assert.Null(tryCloseLocalClientException);
                Assert.Null(tryCloseRemoteClientException);
                Assert.Null(tryCloseListenerException);
            }

            // Act
            // Fire
            Task task = Task.Run(() => Listen(configuration, listenerLocalEndPoint, semaphore, onAccepted));

            semaphore.Wait();

            bool listenerEndPointCreated = IPv4EndPoint.TryCreate("127.0.0.1", listenerLocalEndPoint.Port, out IPv4EndPoint? listenerEndPoint, out Exception? tryCreateEndPointException);
            bool localClientConnected = localClient!.TryConnect(listenerEndPoint!, out Exception? tryConnectException);

            await task;

            // Assert
            Assert.True(listenerEndPointCreated);
            Assert.True(localClientCreated);
            Assert.True(localClientConnected);
            Assert.NotNull(localClient);
            Assert.NotNull(listenerEndPoint);
            Assert.Null(tryCreateException);
            Assert.Null(tryCreateEndPointException);
            Assert.Null(tryConnectException);
        }

        [Fact]
        public void TryConnect_WhenFailedToConnectToTheListener_ShouldReturnFalseAndAssignAnException()
        {
            // Arrange
            Configuration configuration = Configuration.IPv4(SocketType.Stream);
            IPv4EndPoint listenerLocalEndPoint = new IPv4EndPoint(3666);
            bool clientCreated = Client.TryCreate(configuration, out Client? client, out Exception? tryCreateException);

            // Act
            bool listenerEndPointCreated = IPv4EndPoint.TryCreate("127.0.0.1", listenerLocalEndPoint.Port, out IPv4EndPoint? listenerEndPoint, out Exception? tryCreateEndPointException);
            bool clientConnected = client!.TryConnect(listenerEndPoint!, out Exception? tryConnectException);
            bool clientClosed = client!.TryClose(out Exception? tryCloseClientException);

            // Assert
            Assert.True(listenerEndPointCreated);
            Assert.True(clientCreated);
            Assert.False(clientConnected);
            Assert.True(clientClosed);
            Assert.NotNull(client);
            Assert.NotNull(listenerEndPoint);
            Assert.Null(tryCreateException);
            Assert.Null(tryCreateEndPointException);
            Assert.NotNull(tryConnectException);
            Assert.Null(tryCloseClientException);
        }

        [Fact]
        public async Task BeginConnect_WhenSuccessfullyConnectedToListener_ShouldReturnTrue()
        {
            // Arrange
            Configuration configuration = Configuration.IPv4(SocketType.Stream);
            IPv4EndPoint listenerLocalEndPoint = new IPv4EndPoint(3666);
            Client? localClient = default;
            bool localClientConnected = default;
            EndPoint endPoint = default!;
            Exception tryConnectException = default!;
            SemaphoreSlim semaphore = new SemaphoreSlim(0);
            void connectedCallback(Socket socket, EndPoint remoteEndPoint)
            {
                localClientConnected = true;
                endPoint = remoteEndPoint;
            }
            void connectErrorCallback(Socket socket, Exception exception)
            {
                tryConnectException = exception;
            }
            void onAccepted(Client.Listener listener, Client remoteClient)
            {
                // Arrange & Act
                bool localClientClosed = localClient!.TryClose(out Exception? tryCloseLocalClientException);
                bool remoteClientClosed = remoteClient!.TryClose(out Exception? tryCloseRemoteClientException);
                bool listenerClosed = listener.TryClose(out Exception? tryCloseListenerException);

                // Assert
                Assert.True(localClientClosed);
                Assert.True(remoteClientClosed);
                Assert.True(listenerClosed);
                Assert.NotNull(localClient);
                Assert.NotNull(remoteClient);
                Assert.NotNull(listener);
                Assert.Null(tryCloseLocalClientException);
                Assert.Null(tryCloseRemoteClientException);
                Assert.Null(tryCloseListenerException);
            }

            // Act
            // Fire
            bool localClientCreated = Client.TryCreate(configuration, out localClient, out Exception? tryCreateException);

            Task task = Task.Run(() => Listen(configuration, listenerLocalEndPoint, semaphore, onAccepted));

            semaphore.Wait();

            bool listenerEndPointCreated = IPv4EndPoint.TryCreate("127.0.0.1", listenerLocalEndPoint.Port, out IPv4EndPoint? listenerEndPoint, out Exception? tryCreateEndPointException);
            await localClient!.BeginConnect(listenerEndPoint!, connectedCallback, connectErrorCallback);

            await task;

            // Assert
            Assert.True(localClientCreated);
            Assert.True(listenerEndPointCreated);
            Assert.True(localClientConnected);
            Assert.NotNull(localClient);
            Assert.NotNull(endPoint);
            Assert.NotNull(listenerEndPoint);
            Assert.Null(tryCreateException);
            Assert.Null(tryCreateEndPointException);
            Assert.Null(tryConnectException);
        }

        [Fact]
        public async Task BeginConnect_WhenFailedToConnectToTheListener_ShouldReturnFalseAndAssignAnException()
        {
            // Arrange
            Configuration configuration = Configuration.IPv4(SocketType.Stream);
            IPv4EndPoint listenerLocalEndPoint = new IPv4EndPoint(3666);
            bool localClientConnected = default;
            EndPoint endPoint = default!;
            Exception tryConnectException = default!;
            void connectedCallback(Socket socket, EndPoint remoteEndPoint)
            {
                localClientConnected = true;
                endPoint = remoteEndPoint;
            }
            void connectErrorCallback(Socket socket, Exception exception)
            {
                tryConnectException = exception;
            }
            // Act
            // Fire
            bool clientCreated = Client.TryCreate(configuration, out Client? client, out Exception? tryCreateException);
            bool listenerEndPointCreated = IPv4EndPoint.TryCreate("127.0.0.1", listenerLocalEndPoint.Port, out IPv4EndPoint? listenerEndPoint, out Exception? tryCreateEndPointException);
            await client!.BeginConnect(listenerEndPoint!, connectedCallback, connectErrorCallback);
            bool clientClosed = client!.TryClose(out Exception? tryCloseClientException);

            // Assert
            Assert.True(clientCreated);
            Assert.True(listenerEndPointCreated);
            Assert.False(localClientConnected);
            Assert.NotNull(client);
            Assert.Null(endPoint);
            Assert.NotNull(listenerEndPoint);
            Assert.Null(tryCreateException);
            Assert.Null(tryCreateEndPointException);
            Assert.NotNull(tryConnectException);
            Assert.Null(tryCloseClientException);
        }

        [Fact]
        public async Task TrySendAndTryReceive_WhenRemoteClientSentByteToLocalClient_ShouldReturnTrue()
        {
            // Arrange
            Configuration configuration = Configuration.IPv4(SocketType.Stream);
            IPv4EndPoint listenerLocalEndPoint = new IPv4EndPoint(3666);
            bool localClientCreated = Client.TryCreate(configuration, out Client? localClient, out Exception? tryCreateException);
            SemaphoreSlim semaphore = new SemaphoreSlim(0);
            void onAccepted(Client.Listener listener, Client remoteClient)
            {
                // Arrange
                byte dataToSend = sizeof(uint);
                byte dataReceived = default;

                // Act
                bool remoteClientSent = remoteClient.TrySend(dataToSend, 0, out Exception? trySendException);
                bool localClientReceived = localClient!.TryReceive(ref dataReceived, 0, out Exception? tryReceiveException);
                bool localClientClosed = localClient.TryClose(out Exception? tryCloseLocalClientException);
                bool remoteClientClosed = remoteClient!.TryClose(out Exception? tryCloseRemoteClientException);
                bool listenerClosed = listener.TryClose(out Exception? tryCloseListenerException);

                // Assert
                Assert.True(remoteClientSent);
                Assert.True(localClientReceived);
                Assert.True(localClientClosed);
                Assert.True(remoteClientClosed);
                Assert.True(listenerClosed);
                Assert.Equal(dataToSend, dataReceived);
                Assert.NotNull(localClient);
                Assert.NotNull(remoteClient);
                Assert.NotNull(listener);
                Assert.Null(trySendException);
                Assert.Null(tryReceiveException);
                Assert.Null(tryCloseLocalClientException);
                Assert.Null(tryCloseRemoteClientException);
                Assert.Null(tryCloseListenerException);
            }

            // Act
            // Fire
            Task task = Task.Run(() => Listen(configuration, listenerLocalEndPoint, semaphore, onAccepted: onAccepted));

            semaphore.Wait();

            bool listenerEndPointCreated = IPv4EndPoint.TryCreate("127.0.0.1", listenerLocalEndPoint.Port, out IPv4EndPoint? listenerEndPoint, out Exception? tryCreateEndPointException);
            bool localClientConnected = localClient!.TryConnect(listenerEndPoint!, out Exception? tryConnectException);

            await task;

            // Assert
            Assert.True(listenerEndPointCreated);
            Assert.True(localClientCreated);
            Assert.True(localClientConnected);
            Assert.NotNull(localClient);
            Assert.NotNull(listenerEndPoint);
            Assert.Null(tryCreateException);
            Assert.Null(tryCreateEndPointException);
            Assert.Null(tryConnectException);
        }

        [Fact]
        public void TrySend_WhenClientIsNotConnectedToRemoteClientAndTriesToSendByte_ShouldReturnFalseAndAssignAnException()
        {
            // Arrange
            byte dataToSend = sizeof(uint);
            Configuration configuration = Configuration.IPv4(SocketType.Stream);
            bool clientCreated = Client.TryCreate(configuration, out Client? client, out Exception? tryCreateException);

            // Act
            bool clientSent = client!.TrySend(dataToSend, 0, out Exception? trySendException);
            bool clientClosed = client.TryClose(out Exception? tryCloseException);

            // Assert
            Assert.True(clientCreated);
            Assert.False(clientSent);
            Assert.True(clientClosed);
            Assert.NotNull(client);
            Assert.Null(tryCreateException);
            Assert.NotNull(trySendException);
            Assert.Null(tryCloseException);
        }

        [Fact]
        public void TryReceive_WhenClientIsNotConnectedToRemoteClientAndTriesToReceiveByte_ShouldReturnFalseAndAssignAnException()
        {
            // Arrange
            byte dataReceived = default;
            Configuration configuration = Configuration.IPv4(SocketType.Stream);
            bool clientCreated = Client.TryCreate(configuration, out Client? client, out Exception? tryCreateException);

            // Act
            bool clientReceived = client!.TryReceive(ref dataReceived, 0, out Exception? tryReceiveException);
            bool clientClosed = client.TryClose(out Exception? tryCloseException);

            // Assert
            Assert.True(clientCreated);
            Assert.False(clientReceived);
            Assert.True(clientClosed);
            Assert.NotNull(client);
            Assert.Null(tryCreateException);
            Assert.NotNull(tryReceiveException);
            Assert.Null(tryCloseException);
        }

        [Fact]
        public async Task TrySendAndTryReceive_WhenRemoteClientSentBytesToLocalClient_ShouldReturnTrue()
        {
            // Arrange
            Configuration configuration = Configuration.IPv4(SocketType.Stream);
            IPv4EndPoint listenerLocalEndPoint = new IPv4EndPoint(3666);
            bool clientCreated = Client.TryCreate(configuration, out Client? client, out Exception? tryCreateException);
            SemaphoreSlim semaphore = new SemaphoreSlim(0);
            void onAccepted(Client.Listener listener, Client remoteClient)
            {
                // Arrange
                byte[] dataToSend = [sizeof(ushort), sizeof(uint), sizeof(ulong)];
                byte[] dataReceived = new byte[3];

                // Act
                bool remoteClientSent = remoteClient.TrySend(dataToSend, dataToSend.Length, 0, out int bytesSent, out Exception? trySendException);
                bool localClientReceived = client!.TryReceive(dataReceived, dataReceived.Length, 0, out int bytesReceived, out Exception? tryReceiveException);
                bool localClientClosed = client.TryClose(out Exception? tryCloseLocalClientException);
                bool remoteClientClosed = remoteClient!.TryClose(out Exception? tryCloseRemoteClientException);
                bool listenerClosed = listener.TryClose(out Exception? tryCloseListenerException);

                // Assert
                Assert.True(remoteClientSent);
                Assert.True(localClientReceived);
                Assert.True(localClientClosed);
                Assert.True(remoteClientClosed);
                Assert.True(listenerClosed);
                Assert.Equal(bytesSent, bytesReceived);
                Assert.Equal(dataToSend, dataReceived);
                Assert.NotNull(client);
                Assert.NotNull(remoteClient);
                Assert.NotNull(listener);
                Assert.Null(trySendException);
                Assert.Null(tryReceiveException);
                Assert.Null(tryCloseLocalClientException);
                Assert.Null(tryCloseRemoteClientException);
                Assert.Null(tryCloseListenerException);
            }

            // Act
            // Fire
            Task task = Task.Run(() => Listen(configuration, listenerLocalEndPoint, semaphore, onAccepted: onAccepted));

            semaphore.Wait();

            bool listenerEndPointCreated = IPv4EndPoint.TryCreate("127.0.0.1", listenerLocalEndPoint.Port, out IPv4EndPoint? listenerEndPoint, out Exception? tryCreateEndPointException);
            bool clientConnected = client!.TryConnect(listenerEndPoint!, out Exception? tryConnectException);

            await task;

            // Assert
            Assert.True(listenerEndPointCreated);
            Assert.True(clientCreated);
            Assert.True(clientConnected);
            Assert.NotNull(client);
            Assert.NotNull(listenerEndPoint);
            Assert.Null(tryCreateException);
            Assert.Null(tryCreateEndPointException);
            Assert.Null(tryConnectException);
        }

        [Fact]
        public void TrySend_WhenClientIsNotConnectedToRemoteClientAndTriesToSendBytes_ShouldReturnFalseAndAssignAnException()
        {
            // Arrange
            byte[] dataToSend = [sizeof(ushort), sizeof(uint), sizeof(ulong)];
            Configuration configuration = Configuration.IPv4(SocketType.Stream);
            bool clientCreated = Client.TryCreate(configuration, out Client? client, out Exception? tryCreateException);

            // Act
            bool clientSent = client!.TrySend(dataToSend, dataToSend.Length, 0, out int bytesSent, out Exception? trySendException);
            bool clientClosed = client.TryClose(out Exception? tryCloseClientException);

            // Assert
            Assert.True(clientCreated);
            Assert.False(clientSent);
            Assert.True(clientClosed);
            Assert.Equal(default, bytesSent);
            Assert.NotNull(client);
            Assert.Null(tryCreateException);
            Assert.NotNull(trySendException);
            Assert.Null(tryCloseClientException);
        }

        [Fact]
        public void TryReceive_WhenClientIsNotConnectedToRemoteClientAndTriesToReceiveBytes_ShouldReturnFalseAndAssignAnException()
        {
            // Arrange
            byte[] dataReceived = new byte[3];
            Configuration configuration = Configuration.IPv4(SocketType.Stream);
            bool clientCreated = Client.TryCreate(configuration, out Client? client, out Exception? tryCreateException);

            // Act
            bool clientReceived = client!.TryReceive(dataReceived, dataReceived.Length, 0, out int bytesReceived, out Exception? tryReceiveException);
            bool clientClosed = client.TryClose(out Exception? tryCloseClientException);

            // Assert
            Assert.True(clientCreated);
            Assert.False(clientReceived);
            Assert.True(clientClosed);
            Assert.Equal(default, bytesReceived);
            Assert.NotNull(client);
            Assert.Null(tryCreateException);
            Assert.NotNull(tryReceiveException);
            Assert.Null(tryCloseClientException);
        }

        [Fact]
        public async Task BeginSendAndBeginReceive_WhenRemoteClientSentBytesToLocalClient_ShouldReturnTrue()
        {
            // Arrange
            Configuration configuration = Configuration.IPv4(SocketType.Stream);
            IPv4EndPoint listenerLocalEndPoint = new IPv4EndPoint(3666);
            Client? client = default;
            SemaphoreSlim semaphore = new SemaphoreSlim(0);
            bool remoteClientSent = false;
            bool localClientReceived = false;
            int bytesSent = 0;
            int bytesReceived = 0;
            Exception trySendException = default!;
            Exception tryReceiveException = default!;
            void sendCallback(Socket socket, EndPoint clientEndPoint, byte[] buffer, int bytesTranferred)
            {
                remoteClientSent = true;
                bytesSent = bytesTranferred;
            }
            void receiveCallback(Socket socket, EndPoint clientEndPoint, byte[] buffer, int bytesTranferred)
            {
                localClientReceived = true;
                bytesReceived = bytesTranferred;
            }
            void sendErrorCallback(Socket socket, Exception exception)
            {
                trySendException = exception;
            }
            void receiveErrorCallback(Socket socket, Exception exception)
            {
                tryReceiveException = exception;
            }
            async Task onAccepted(Client.Listener listener, Client remoteClient)
            {
                // Arrange
                byte[] dataToSend = [sizeof(ushort), sizeof(uint), sizeof(ulong)];
                byte[] dataReceived = new byte[3];

                // Act
                await remoteClient.BeginSend(dataToSend, dataToSend.Length, 0, sendCallback, sendErrorCallback);
                await client!.BeginReceive(dataReceived, dataReceived.Length, 0, receiveCallback, receiveErrorCallback);
                bool localClientClosed = client.TryClose(out Exception? tryCloseLocalClientException);
                bool remoteClientClosed = remoteClient!.TryClose(out Exception? tryCloseRemoteClientException);
                bool listenerClosed = listener.TryClose(out Exception? tryCloseListenerException);

                // Assert
                Assert.True(remoteClientSent);
                Assert.True(localClientReceived);
                Assert.True(localClientClosed);
                Assert.True(remoteClientClosed);
                Assert.True(listenerClosed);
                Assert.Equal(bytesSent, bytesReceived);
                Assert.Equal(dataToSend, dataReceived);
                Assert.NotNull(client);
                Assert.NotNull(remoteClient);
                Assert.NotNull(listener);
                Assert.Null(trySendException);
                Assert.Null(tryReceiveException);
                Assert.Null(tryCloseLocalClientException);
                Assert.Null(tryCloseRemoteClientException);
                Assert.Null(tryCloseListenerException);
            }

            // Act
            bool clientCreated = Client.TryCreate(configuration, out client, out Exception? tryCreateException);

            // Fire
            Task task = Task.Run(() => Listen(configuration, listenerLocalEndPoint, semaphore, onAccepted: onAccepted));

            semaphore.Wait();

            bool listenerEndPointCreated = IPv4EndPoint.TryCreate("127.0.0.1", listenerLocalEndPoint.Port, out IPv4EndPoint? listenerEndPoint, out Exception? tryCreateEndPointException);
            bool clientConnected = client!.TryConnect(listenerEndPoint!, out Exception? tryConnectException);

            await task;

            // Assert
            Assert.True(listenerEndPointCreated);
            Assert.True(clientCreated);
            Assert.True(clientConnected);
            Assert.NotNull(client);
            Assert.NotNull(listenerEndPoint);
            Assert.Null(tryCreateException);
            Assert.Null(tryCreateEndPointException);
            Assert.Null(tryConnectException);
        }

        [Fact]
        public async Task BeginSend_WhenClientIsNotConnectedToRemoteClientAndTriesToSendBytes_ShouldReturnFalseAndAssignAnException()
        {
            // Arrange
            byte[] dataToSend = [sizeof(ushort), sizeof(uint), sizeof(ulong)];
            bool clientSent = false;
            int bytesSent = default;
            Exception trySendException = default!;
            Configuration configuration = Configuration.IPv4(SocketType.Stream);
            IPv4EndPoint listenerLocalEndPoint = new IPv4EndPoint(3666);
            void sendCallback(Socket socket, EndPoint clientEndPoint, byte[] buffer, int bytesTranferred)
            {
                clientSent = true;
                bytesSent = bytesTranferred;
            }
            void sendErrorCallback(Socket socket, Exception exception)
            {
                trySendException = exception;
            }

            // Act
            bool clientCreated = Client.TryCreate(configuration, out Client? client, out Exception? tryCreateException);
            await client!.BeginSend(dataToSend, dataToSend.Length, 0, sendCallback, sendErrorCallback);
            bool clientClosed = client.TryClose(out Exception? tryCloseClientException);

            // Assert
            Assert.True(clientCreated);
            Assert.False(clientSent);
            Assert.True(clientClosed);
            Assert.Equal(default, bytesSent);
            Assert.NotNull(client);
            Assert.Null(tryCreateException);
            Assert.NotNull(trySendException);
            Assert.Null(tryCloseClientException);
        }

        [Fact]
        public async Task BeginReceive_WhenClientIsNotConnectedToRemoteClientAndTriesToReceiveBytes_ShouldReturnFalseAndAssignAnException()
        {
            // Arrange
            byte[] dataReceived = new byte[3];
            bool clientReceived = false;
            int bytesReceived = default;
            Exception tryReceiveException = default!;
            Configuration configuration = Configuration.IPv4(SocketType.Stream);
            IPv4EndPoint listenerLocalEndPoint = new IPv4EndPoint(3666);
            void receiveCallback(Socket socket, EndPoint clientEndPoint, byte[] buffer, int bytesTranferred)
            {
                clientReceived = true;
                bytesReceived = bytesTranferred;
            }
            void receiveErrorCallback(Socket socket, Exception exception)
            {
                tryReceiveException = exception;
            }

            // Act
            bool clientCreated = Client.TryCreate(configuration, out Client? client, out Exception? tryCreateException);
            await client!.BeginReceive(dataReceived, dataReceived.Length, 0, receiveCallback, receiveErrorCallback);
            bool clientClosed = client.TryClose(out Exception? tryCloseClientException);

            // Assert
            Assert.True(clientCreated);
            Assert.False(clientReceived);
            Assert.True(clientClosed);
            Assert.Equal(default, bytesReceived);
            Assert.NotNull(client);
            Assert.Null(tryCreateException);
            Assert.NotNull(tryReceiveException);
            Assert.Null(tryCloseClientException);
        }

        private static void Listen(Configuration configuration, EndPoint listenerLocalEndPoint, SemaphoreSlim semaphore, Action<Client.Listener, Client>? onAccepted = null)
        {
            // Arrange
            bool listenerCreated = Client.Listener.TryCreate(configuration, out Client.Listener? listener, out Exception? tryCreateException);
            bool listenerBound = listener!.TryBind(listenerLocalEndPoint, out Exception? tryBindException);

            // Act
            bool listenerListening = listener.TryListen(1000, out Exception? tryListenException);

            semaphore.Release();

            bool listenerAccepted = listener.TryAccept(out Client? remoteClient, out Exception? tryAcceptException);

            onAccepted?.Invoke(listener, remoteClient!);

            // Assert
            Assert.True(listenerCreated);
            Assert.True(listenerBound);
            Assert.True(listenerListening);
            Assert.True(listenerAccepted);
            Assert.NotNull(listener);
            Assert.NotNull(remoteClient);
            Assert.Null(tryCreateException);
            Assert.Null(tryBindException);
            Assert.Null(tryListenException);
            Assert.Null(tryAcceptException);
        }

        private static async Task Listen(Configuration configuration, EndPoint listenerLocalEndPoint, SemaphoreSlim semaphore, Func<Client.Listener, Client, Task>? onAccepted = null)
        {
            // Arrange
            bool listenerCreated = Client.Listener.TryCreate(configuration, out Client.Listener? listener, out Exception? tryCreateException);
            bool listenerBound = listener!.TryBind(listenerLocalEndPoint, out Exception? tryBindException);

            // Act
            bool listenerListening = listener.TryListen(1000, out Exception? tryListenException);

            semaphore.Release();

            bool listenerAccepted = listener.TryAccept(out Client? remoteClient, out Exception? tryAcceptException);

            if (onAccepted is not null)
                await onAccepted.Invoke(listener, remoteClient!);

            // Assert
            Assert.True(listenerCreated);
            Assert.True(listenerBound);
            Assert.True(listenerListening);
            Assert.True(listenerAccepted);
            Assert.NotNull(listener);
            Assert.NotNull(remoteClient);
            Assert.Null(tryCreateException);
            Assert.Null(tryBindException);
            Assert.Null(tryListenException);
            Assert.Null(tryAcceptException);
        }
    }
}