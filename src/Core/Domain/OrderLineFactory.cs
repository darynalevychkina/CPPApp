using System;
using System.Collections.Generic;
using Core.Dto;

namespace Core.Domain;

public static class OrderLineFactory
{
    public static ImportResult<OrderLine> FromVehicles(ImportResult<VehicleDto> import)
    {
        var lines = new List<OrderLine>();
        var errors = new List<string>(import.Errors);

        foreach (VehicleDto v in import.Items)
        {
            try
            {
                OrderLine line = OrderLine.Create(v.Id, $"{v.Brand} {v.Model}", v.Price, quantity: 1);
                lines.Add(line);
            }
            catch (Exception ex)
            {
                errors.Add($"{v.Id}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        return new ImportResult<OrderLine>(lines, errors);
    }
}