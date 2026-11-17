using Xunit;
using Core;
using Core.Domain;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class OrderServiceTests
{
    private static OrderService NewService() => new(new InMemoryOrderStore());

    [Fact]
    public void CreateOrder_RaisesChangedExactlyOnce()
    {
        var service = NewService();
        int raised = 0;
        service.Changed += (_, _) => raised++;

        service.CreateOrder("CU-01");

        Assert.Equal(1, raised);
    }

    [Fact]
    public void AddLine_WhenInvariantFails_DoesNotRaiseChanged()
    {
        var service = NewService();
        Order order = service.CreateOrder("CU-01");
        service.AddLine(order.Id, "V-1", "Car", 100m, 1);
        service.Confirm(order.Id);

        int raised = 0;
        service.Changed += (_, _) => raised++;

        Assert.Throws<DomainException>(() => service.AddLine(order.Id, "V-2", "Bike", 10m, 1));
        Assert.Equal(0, raised);
    }

    [Fact]
    public void AddLine_UnknownOrder_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => NewService().AddLine("nope", "V-1", "Car", 1m, 1));
    }

    [Fact]
    public void Confirm_FourthConfirmedOrder_IsRejectedByLimit()
    {
        var service = NewService();
        for (int i = 0; i < OrderService.MaxConfirmedOrdersPerCustomer; i++)
        {
            Order o = service.CreateOrder("CU-01");
            service.AddLine(o.Id, "V-1", "Car", 100m, 1);
            service.Confirm(o.Id);
        }

        Order extra = service.CreateOrder("CU-01");
        service.AddLine(extra.Id, "V-1", "Car", 100m, 1);

        var ex = Assert.Throws<DomainException>(() => service.Confirm(extra.Id));
        Assert.Contains("limit", ex.Message);
        Assert.False(extra.IsConfirmed);
    }
}
