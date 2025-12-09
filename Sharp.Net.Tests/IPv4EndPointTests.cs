using Sharp.Net.EndPoints;
using System;
using Xunit;

namespace Sharp.Net.Tests
{
    public class IPv4EndPointTests
    {
        [Fact]
        public void TryCreate_WhenUsedWithCorrectAddressAndPort_ShouldReturnTrue()
        {
            // Arrange
            ushort port = 3666;

            // Act
            bool endPointCreated = IPv4EndPoint.TryCreate("127.0.0.1", port, out IPv4EndPoint? endPoint, out Exception? exception);

            // Assert
            Assert.True(endPointCreated);
            Assert.Equal(endPoint!.AddressFamily, (ushort)AddressFamily.IPv4);
            Assert.Equal(endPoint.Address, IPv4EndPoint.Loopback);
            Assert.Equal(endPoint.Port, port);
            Assert.Null(exception);
        }

        [Fact]
        public void TryCreate_WhenUsedWithWrongAddress_ShouldReturnFalseAndAssignException()
        {
            // Arrange
            ushort port = 3666;

            // Act
            bool endPointCreated = IPv4EndPoint.TryCreate("260.0.0.1", port, out IPv4EndPoint? endPoint, out Exception? exception);

            // Assert
            Assert.False(endPointCreated);
            Assert.Null(endPoint);
            Assert.NotNull(exception);
        }
    }
}