using System.Security.Cryptography;
using Api.Contracts;
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrders(this WebApplication app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders");

        // 1) list
        group.MapGet("/", (IOrderStore store) =>
                TypedResults.Ok(store.List().Select(o => o.ToResponse()).ToList()))
            .WithSummary("List all orders");

        // 2) one order
        group.MapGet("/{id}", Results<Ok<OrderResponse>, NotFound>
                (string id, IOrderStore store) =>
                    store.GetById(id) is { } found
                        ? TypedResults.Ok(found.ToResponse())
                        : TypedResults.NotFound())
            .WithSummary("Get an order by id");

        // 3) create: 201 + Location, or 400 with the list of fields
        group.MapPost("/", Results<Created<OrderResponse>, ValidationProblem>
                (SaveOrderRequest request, IOrderStore store) =>
            {
                if (!request.TryValidate(out var errors))
                    return TypedResults.ValidationProblem(errors);

                string id = "O-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant();
                Order created = request.ToOrder(id);
                store.Add(created);

                return TypedResults.Created($"/api/orders/{created.Id}", created.ToResponse());
            })
            .WithSummary("Create a Draft order");

        // 4) replace: only a Draft order can be replaced (otherwise DomainException -> 409)
        group.MapPut("/{id}", Results<Ok<OrderResponse>, NotFound, ValidationProblem>
                (string id, SaveOrderRequest request, IOrderStore store) =>
            {
                if (!request.TryValidate(out var errors))
                    return TypedResults.ValidationProblem(errors);

                if (store.GetById(id) is not { } existing)
                    return TypedResults.NotFound();

                if (existing.Status != OrderStatus.Draft)
                    throw new DomainException($"Order {id} is {existing.Status}; only a Draft order can be replaced.");

                // PUT replaces the state: a new valid order with the same id
                Order replacement = request.ToOrder(existing.Id);
                store.Update(replacement);

                return TypedResults.Ok(replacement.ToResponse());
            })
            .WithSummary("Replace a Draft order");

        // 5) delete
        group.MapDelete("/{id}", Results<NoContent, NotFound>
                (string id, IOrderStore store) =>
                    store.Remove(id) ? TypedResults.NoContent() : TypedResults.NotFound())
            .WithSummary("Delete an order");

        // variant of the domain: the total of an order
        group.MapGet("/{id}/total", Results<Ok<OrderTotalResponse>, NotFound>
                (string id, IOrderStore store) =>
                    store.GetById(id) is { } found
                        ? TypedResults.Ok(new OrderTotalResponse(found.Id, found.Lines.Count, found.Total))
                        : TypedResults.NotFound())
            .WithSummary("Total of an order");

        // business action: Draft -> Confirmed; rules of the domain (empty order, customer limit) -> 409
        group.MapPost("/{id}/confirm", Results<Ok<OrderResponse>, NotFound>
                (string id, IOrderStore store, OrderService service) =>
            {
                if (store.GetById(id) is null)
                    return TypedResults.NotFound();

                service.Confirm(id);                       // may throw DomainException -> 409
                return TypedResults.Ok(store.GetById(id)!.ToResponse());
            })
            .WithSummary("Confirm a Draft order");
    }
}
