using Api;
using Api.Endpoints;
using Core;
using Core.Abstractions;
using Core.Services;
using Core.Storage;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();                       // errors in the RFC 9457 format
builder.Services.AddExceptionHandler<ApiExceptionHandler>(); // DomainException -> 409, bad body -> 400

// Singleton: one store for the whole run; it is thread-safe (see InMemoryOrderStore).
builder.Services.AddSingleton<IOrderStore>(_ => new InMemoryOrderStore(SampleData.Orders()));
builder.Services.AddSingleton<OrderService>();

var app = builder.Build();

app.UseExceptionHandler();                                  // no stack trace for the client
app.UseStatusCodePages();                                   // 404/415/... as ProblemDetails too

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                                       // /openapi/v1.json
    app.MapScalarApiReference();                            // /scalar/v1
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapOrders();

app.Run();
