using Core.Domain;

namespace Core.Services;

public interface IOrderRepository
{
    void Add(Order order);
    int CountConfirmedByCustomer(string customerId);
}