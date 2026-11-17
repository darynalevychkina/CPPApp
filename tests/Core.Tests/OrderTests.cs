using Xunit;
using Core;
using Core.Domain;

namespace Core.Tests;

public class OrderTests
{
    [Fact]
    public void Confirm_EmptyOrder_ThrowsDomainException_AndKeepsDraft()
    {
        // Arrange
        Order order = Order.Create("O-1", "CU-01");

        // Act
        var ex = Assert.Throws<DomainException>(() => order.Confirm());

        // Assert: message is about the rule, state did not change
        Assert.Contains("empty", ex.Message);
        Assert.Equal(OrderStatus.Draft, order.Status);
    }

    [Fact]
    public void AddLine_ToConfirmedOrder_ThrowsDomainException_AndKeepsLines()
    {
        Order order = Order.Create("O-1", "CU-01");
        order.AddLine("V-1", "Car", 100m, 1);
        order.Confirm();

        Assert.Throws<DomainException>(() => order.AddLine("V-2", "Bike", 50m, 1));

        Assert.Single(order.Lines);
        Assert.Equal(100m, order.Total);
    }

    [Fact]
    public void Cancel_AlreadyCancelled_ThrowsDomainException()
    {
        Order order = Order.Create("O-1", "CU-01");
        order.Cancel();

        Assert.Throws<DomainException>(() => order.Cancel());
    }

    [Fact]
    public void Create_EmptyId_ThrowsArgumentException_NotDomainException()
    {
        // a wrong argument is a programmer error, not a business rule
        Assert.Throws<ArgumentException>(() => Order.Create(" ", "CU-01"));
    }

    [Fact]
    public void Total_IsSumOfLineTotals()
    {
        Order order = Order.Create("O-1", "CU-01");
        order.AddLine("V-1", "Car", 100m, 2);
        order.AddLine("V-2", "Bike", 50.5m, 1);

        Assert.Equal(250.5m, order.Total);
    }
}
