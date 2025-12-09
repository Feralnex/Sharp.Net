using Sharp.Net.Configurations;
using Sharp.Net.EndPoints;
using Sharp.Net.Sockets;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Sharp.Net.Tests
{
    public class NodeTests
    {

        [Fact]
        public void TryCreate_WhenUsedIPv4ConfigurationWithSupportedSocketType_ShouldReturnTrueAndAssignNode()
        {
            // Arrange
            Configuration configuration = Configuration.IPv4(SocketType.Stream);

            // Act
            bool nodeCreated = Node.TryCreate(configuration, out Node? node, out Exception? tryCreateException);
            bool nodeClosed = node!.TryClose(out Exception? tryCloseException);

            // Assert
            Assert.True(nodeCreated);
            Assert.True(nodeClosed);
            Assert.NotNull(node);
            Assert.Null(tryCreateException);
            Assert.Null(tryCloseException);
        }

        [Fact]
        public void TryCreate_WhenUsedWithSupportedConfigurationOfSocketTypeAndProtocol_ShouldReturnTrueAndAssignNode()
        {
            // Arrange
            Configuration configuration = Configuration.IPv4(SocketType.Stream, Protocol.Tcp);

            // Act
            bool nodeCreated = Node.TryCreate(configuration, out Node? node, out Exception? tryCreateException);
            bool nodeClosed = node!.TryClose(out Exception? tryCloseException);

            // Assert
            Assert.True(nodeCreated);
            Assert.True(nodeClosed);
            Assert.NotNull(node);
            Assert.Null(tryCreateException);
            Assert.Null(tryCloseException);
        }

        [Fact]
        public void TryCreate_WhenUsedWithNotSupportedConfigurationOfSocketTypeAndProtocol_ShouldReturnFalseAndAssignAnException()
        {
            // Arrange
            Configuration configuration = Configuration.IPv4(SocketType.Stream, Protocol.Udp);

            // Act
            bool nodeCreated = Node.TryCreate(configuration, out Node? node, out Exception? tryCreateException);

            // Assert
            Assert.False(nodeCreated);
            Assert.Null(node);
            Assert.NotNull(tryCreateException);
        }

        [Fact]
        public void TryBind_WhenUsedWithoutEndPoint_ShouldReturnTrue()
        {
            // Arrange
            Configuration configuration = Configuration.IPv4(SocketType.Datagram);
            bool nodeCreated = Node.TryCreate(configuration, out Node? node, out Exception? tryCreateException);

            // Act
            bool nodeBound = node!.TryBind(out Exception? tryBindException);
            bool nodeClosed = node!.TryClose(out Exception? tryCloseException);

            // Assert
            Assert.True(nodeCreated);
            Assert.True(nodeBound);
            Assert.True(nodeClosed);
            Assert.NotNull(node);
            Assert.Null(tryCreateException);
            Assert.Null(tryBindException);
            Assert.Null(tryCloseException);
        }

        [Fact]
        public void TryBind_WhenUsedWithEndPoint_ShouldReturnTrue()
        {
            // Arrange
            Configuration configuration = Configuration.IPv4(SocketType.Datagram);
            bool endPointCreated = IPv4EndPoint.TryCreate("127.0.0.1", 3666, out IPv4EndPoint? endPoint, out Exception? tryCreateEndPointException);
            bool nodeCreated = Node.TryCreate(configuration, out Node? node, out Exception? tryCreateNodeException);

            // Act
            bool nodeBound = node!.TryBind(endPoint!, out Exception? tryBindException);
            bool nodeClosed = node!.TryClose(out Exception? tryCloseException);

            // Assert
            Assert.True(endPointCreated);
            Assert.True(nodeCreated);
            Assert.True(nodeBound);
            Assert.True(nodeClosed);
            Assert.NotNull(endPoint);
            Assert.NotNull(node);
            Assert.Null(tryCreateEndPointException);
            Assert.Null(tryCreateNodeException);
            Assert.Null(tryBindException);
            Assert.Null(tryCloseException);
        }

        [Fact]
        public void TrySendToAndTryReceiveFrom_WhenRemoteNodeSentByteToLocalNode_ShouldReturnTrue()
        {
            // Arrange
            Configuration configuration = Configuration.IPv4(SocketType.Datagram);
            IPv4EndPoint remoteNodeLocalEndPoint = new IPv4EndPoint(3666);
            bool localNodeCreated = Node.TryCreate(configuration, out Node? localNode, out Exception? tryCreateLocalNodeException);
            bool remoteNodeCreated = Node.TryCreate(configuration, out Node? remoteNode, out Exception? tryCreateRemoteNodeException);
            byte dataToSend = sizeof(uint);
            byte dataReceived = default;

            // Act
            bool remoteNodeBound = remoteNode!.TryBind(remoteNodeLocalEndPoint, out Exception? tryBindException);
            bool localNodeEndPointCreated = IPv4EndPoint.TryCreate("127.0.0.1", remoteNodeLocalEndPoint.Port, out IPv4EndPoint? remoteNodeEndPoint, out Exception? tryCreateEndPointException);
            bool localNodeSent = localNode!.TrySendTo(dataToSend, 0, remoteNodeEndPoint!, out Exception? trySendToException);
            bool remoteNodeReceived = remoteNode.TryReceiveFrom(ref dataReceived, 0, out EndPoint? remoteEndPoint, out Exception? tryReceiveFromException);
            bool remoteNodeClosed = remoteNode!.TryClose(out Exception? tryCloseRemoteNodeException);
            bool localNodeClosed = localNode.TryClose(out Exception? tryCloseLocalNodeException);

            // Assert
            Assert.True(localNodeCreated);
            Assert.True(remoteNodeCreated);
            Assert.True(remoteNodeBound);
            Assert.True(localNodeEndPointCreated);
            Assert.True(localNodeSent);
            Assert.True(remoteNodeReceived);
            Assert.True(remoteNodeClosed);
            Assert.True(localNodeClosed);
            Assert.Equal(dataToSend, dataReceived);
            Assert.NotNull(localNode);
            Assert.NotNull(remoteNode);
            Assert.NotNull(remoteNodeEndPoint);
            Assert.NotNull(remoteEndPoint);
            Assert.Null(tryCreateLocalNodeException);
            Assert.Null(tryCreateRemoteNodeException);
            Assert.Null(tryBindException);
            Assert.Null(tryCreateEndPointException);
            Assert.Null(trySendToException);
            Assert.Null(tryReceiveFromException);
            Assert.Null(tryCloseRemoteNodeException);
            Assert.Null(tryCloseLocalNodeException);
        }

        [Fact]
        public void TrySend_WhenNodeTriesToSendByteToNonExistentNode_ShouldReturnFalseAndAssignAnException()
        {
            // Arrange
            byte dataToSend = sizeof(uint);
            Configuration configuration = Configuration.IPv4(SocketType.Stream);
            bool localClientCreated = Node.TryCreate(configuration, out Node? node, out Exception? tryCreateException);

            // Act
            bool localNodeSent = node!.TrySendTo(dataToSend, 0, null!, out Exception? trySendToException);
            bool localClientClosed = node.TryClose(out Exception? tryCloseLocalClientException);

            // Assert
            Assert.True(localClientCreated);
            Assert.False(localNodeSent);
            Assert.True(localClientClosed);
            Assert.NotNull(node);
            Assert.Null(tryCreateException);
            Assert.NotNull(trySendToException);
            Assert.Null(tryCloseLocalClientException);
        }

        [Fact]
        public void TrySendToAndTryReceiveFrom_WhenRemoteNodeSentBytesToLocalNode_ShouldReturnTrue()
        {
            // Arrange
            Configuration configuration = Configuration.IPv4(SocketType.Datagram);
            IPv4EndPoint remoteNodeLocalEndPoint = new IPv4EndPoint(3666);
            bool localNodeCreated = Node.TryCreate(configuration, out Node? localNode, out Exception? tryCreateLocalNodeException);
            bool remoteNodeCreated = Node.TryCreate(configuration, out Node? remoteNode, out Exception? tryCreateRemoteNodeException);
            byte[] dataToSend = [sizeof(ushort), sizeof(uint), sizeof(ulong)];
            byte[] dataReceived = new byte[3];

            // Act
            bool remoteNodeBound = remoteNode!.TryBind(remoteNodeLocalEndPoint, out Exception? tryBindException);
            bool localNodeEndPointCreated = IPv4EndPoint.TryCreate("127.0.0.1", remoteNodeLocalEndPoint.Port, out IPv4EndPoint? remoteNodeEndPoint, out Exception? tryCreateEndPointException);
            bool localNodeSent = localNode!.TrySendTo(dataToSend, dataToSend.Length, 0, remoteNodeEndPoint!, out int bytesSent, out Exception? trySendToException);
            bool remoteNodeReceived = remoteNode.TryReceiveFrom(dataReceived, dataReceived.Length, 0, out EndPoint? remoteEndPoint, out int bytesReceived, out Exception? tryReceiveFromException);
            bool remoteNodeClosed = remoteNode!.TryClose(out Exception? tryCloseRemoteNodeException);
            bool localNodeClosed = localNode.TryClose(out Exception? tryCloseLocalNodeException);

            // Assert
            Assert.True(localNodeCreated);
            Assert.True(remoteNodeCreated);
            Assert.True(remoteNodeBound);
            Assert.True(localNodeEndPointCreated);
            Assert.True(localNodeSent);
            Assert.True(remoteNodeReceived);
            Assert.Equal(bytesSent, bytesReceived);
            Assert.Equal(dataToSend, dataReceived);
            Assert.True(remoteNodeClosed);
            Assert.True(localNodeClosed);
            Assert.NotNull(localNode);
            Assert.NotNull(remoteNode);
            Assert.NotNull(remoteNodeEndPoint);
            Assert.NotNull(remoteEndPoint);
            Assert.Null(tryCreateLocalNodeException);
            Assert.Null(tryCreateRemoteNodeException);
            Assert.Null(tryBindException);
            Assert.Null(tryCreateEndPointException);
            Assert.Null(trySendToException);
            Assert.Null(tryReceiveFromException);
            Assert.Null(tryCloseRemoteNodeException);
            Assert.Null(tryCloseLocalNodeException);
        }

        [Fact]
        public void TrySend_WhenNodeTriesToSendBytesToNonExistentNode_ShouldReturnFalseAndAssignAnException()
        {
            // Arrange
            byte[] dataToSend = [sizeof(ushort), sizeof(uint), sizeof(ulong)];
            Configuration configuration = Configuration.IPv4(SocketType.Stream);
            bool localNodeCreated = Node.TryCreate(configuration, out Node? node, out Exception? tryCreateException);

            // Act
            bool localNodeSent = node!.TrySendTo(dataToSend, dataToSend.Length, 0, null!, out int bytesSent, out Exception? trySendToException);
            bool localNodeClosed = node.TryClose(out Exception? tryCloseLocalClientException);

            // Assert
            Assert.True(localNodeCreated);
            Assert.False(localNodeSent);
            Assert.True(localNodeClosed);
            Assert.NotNull(node);
            Assert.Equal(default, bytesSent);
            Assert.Null(tryCreateException);
            Assert.NotNull(trySendToException);
            Assert.Null(tryCloseLocalClientException);
        }

        [Fact]
        public async Task BeginSendToAndTryReceiveFrom_WhenRemoteNodeSentBytesToLocalNode_ShouldReturnTrue()
        {
            // Arrange
            Node? localNode = default;
            Node? remoteNode = default;
            Configuration configuration = Configuration.IPv4(SocketType.Datagram);
            bool remoteNodeEndPointCreated = IPv4EndPoint.TryCreate("127.0.0.1", 3666, out IPv4EndPoint? remoteNodeEndPoint, out Exception? tryCreateRemoteEndPointException);
            IPv4EndPoint? localNodeEndPoint = default;
            byte[] dataToSend = [sizeof(ushort), sizeof(uint), sizeof(ulong)];
            byte[] dataReceived = new byte[3];
            bool localNodeSent = false;
            bool remoteNodeReceived = false;
            int bytesSent = default;
            int bytesReceived = default;
            EndPoint? remoteEndPoint = default;
            Exception? trySendToException = default;
            Exception? tryReceiveFromException = default;
            void sendCallback(Socket socket, EndPoint nodeEndPoint, byte[] buffer, int bytesTranferred)
            {
                localNodeSent = true;
                bytesSent = bytesTranferred;

                Assert.Equal(localNode, socket);
                Assert.Same(remoteNodeEndPoint, nodeEndPoint);
                Assert.Equal(dataToSend, buffer);
                Assert.Equal(buffer.Length, bytesTranferred);
            }
            void sendErrorCallback(Socket socket, Exception exception)
            {
                trySendToException = exception;

                Assert.Equal(localNode, socket);
            }
            void receiveCallback(Socket socket, EndPoint nodeEndPoint, byte[] buffer, int bytesTranferred)
            {
                IPEndPoint? endPoint = nodeEndPoint as IPEndPoint;

                remoteNodeReceived = true;
                bytesReceived = bytesTranferred;
                remoteEndPoint = nodeEndPoint;

                Assert.Equal(remoteNode, socket);
                Assert.Equal(localNodeEndPoint!.Address, endPoint!.Address);
                Assert.Equal(localNodeEndPoint.Port, endPoint.Port);
                Assert.Equal(dataToSend, buffer);
                Assert.Equal(dataReceived, buffer);
                Assert.Equal(buffer.Length, bytesTranferred);
            }
            void receiveErrorCallback(Socket socket, Exception exception)
            {
                tryReceiveFromException = exception;

                Assert.Equal(localNode, socket);
            }

            // Act
            bool localNodeEndPointCreated = IPv4EndPoint.TryCreate("127.0.0.1", 3667, out localNodeEndPoint, out Exception? tryCreateLocalEndPointException);
            bool localNodeCreated = Node.TryCreate(configuration, out localNode, out Exception? tryCreateLocalNodeException);
            bool remoteNodeCreated = Node.TryCreate(configuration, out remoteNode, out Exception? tryCreateRemoteNodeException);
            bool localNodeBound = localNode!.TryBind(localNodeEndPoint!, out Exception? tryBindLocalNodeException);
            bool remoteNodeBound = remoteNode!.TryBind(remoteNodeEndPoint!, out Exception? tryBindRemoteNodeException);
            await localNode!.BeginSendTo(dataToSend, dataToSend.Length, 0, remoteNodeEndPoint!, sendCallback, sendErrorCallback);
            await remoteNode.BeginReceiveFrom(dataReceived, dataReceived.Length, 0, receiveCallback, receiveErrorCallback);
            bool remoteNodeClosed = remoteNode!.TryClose(out Exception? tryCloseRemoteNodeException);
            bool localNodeClosed = localNode.TryClose(out Exception? tryCloseLocalNodeException);

            // Assert
            Assert.True(localNodeCreated);
            Assert.True(remoteNodeCreated);
            Assert.True(localNodeBound);
            Assert.True(remoteNodeBound);
            Assert.True(localNodeEndPointCreated);
            Assert.True(remoteNodeEndPointCreated);
            Assert.True(localNodeSent);
            Assert.True(remoteNodeReceived);
            Assert.Equal(bytesSent, bytesReceived);
            Assert.Equal(dataToSend, dataReceived);
            Assert.True(remoteNodeClosed);
            Assert.True(localNodeClosed);
            Assert.NotNull(localNode);
            Assert.NotNull(remoteNode);
            Assert.NotNull(localNodeEndPoint);
            Assert.NotNull(remoteNodeEndPoint);
            Assert.NotNull(remoteEndPoint);
            Assert.Null(tryCreateLocalEndPointException);
            Assert.Null(tryCreateRemoteEndPointException);
            Assert.Null(tryCreateLocalNodeException);
            Assert.Null(tryCreateRemoteNodeException);
            Assert.Null(tryBindLocalNodeException);
            Assert.Null(tryBindRemoteNodeException);
            Assert.Null(trySendToException);
            Assert.Null(tryReceiveFromException);
            Assert.Null(tryCloseRemoteNodeException);
            Assert.Null(tryCloseLocalNodeException);
        }
    }
}