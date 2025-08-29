using CarInsurance.Api.Data;
using CarInsurance.Api.Dtos;
using CarInsurance.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.Services;

public class CarService(AppDbContext db)
{
    private readonly AppDbContext _db = db;

    public async Task<List<CarDto>> ListCarsAsync()
    {
        return await _db.Cars.Include(c => c.Owner)
            .Select(c => new CarDto(c.Id, c.Vin, c.Make, c.Model, c.YearOfManufacture,
                                    c.OwnerId, c.Owner.Name, c.Owner.Email))
            .ToListAsync();
    }

    public async Task<bool> IsInsuranceValidAsync(long carId, DateOnly date)
    {
        var carExists = await _db.Cars.AnyAsync(c => c.Id == carId);
        if (!carExists) throw new KeyNotFoundException($"Car {carId} not found");

        return await _db.Policies.AnyAsync(p =>
            p.CarId == carId &&
            p.StartDate <= date &&
            (p.EndDate == null || p.EndDate >= date)
        );
    }

    public async Task<ClaimDto> RegisterClaimAsync(long carId, DateOnly date, string description, decimal amount)
    {
        var car = await _db.Cars.FindAsync(carId);
        if (car is null)
            throw new KeyNotFoundException($"Car {carId} not found");

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        var claim = new InsuranceClaim
        {
            CarId = carId,
            ClaimDate = date,
            Description = description.Trim(),
            Amount = amount
        };

        _db.Claims.Add(claim);
        await _db.SaveChangesAsync();

        return new ClaimDto(
            claim.Id,
            carId,
            claim.ClaimDate.ToString("yyyy-MM-dd"),
            claim.Description,
            claim.Amount
        );
    }

    public async Task<List<CarHistoryItem>> GetCarHistoryAsync(long carId)
    {
        var exists = await _db.Cars.AnyAsync(c => c.Id == carId);
        if (!exists) throw new KeyNotFoundException($"Car {carId} not found");

        var policies = await _db.Policies
            .Where(p => p.CarId == carId)
            .Select(p => new
            {
                p.StartDate,
                p.EndDate,
                p.Provider
            })
            .ToListAsync();

        var claims = await _db.Claims
            .Where(c => c.CarId == carId)
            .Select(c => new
            {
                c.ClaimDate,
                c.Description,
                c.Amount
            })
            .ToListAsync();

        var items = new List<(DateOnly sortKey, CarHistoryItem item)>();

        foreach (var p in policies)
        {
            items.Add((
                p.StartDate,
                new CarHistoryItem(
                    Kind: "PolicyPeriod",
                    Date: p.StartDate.ToString("yyyy-MM-dd"),
                    EndDate: p.EndDate.ToString("yyyy-MM-dd"),
                    Provider: p.Provider,
                    Description: null,
                    Amount: null
                )));
        }

        foreach (var c in claims)
        {
            items.Add((
                c.ClaimDate,
                new CarHistoryItem(
                    Kind: "Claim",
                    Date: c.ClaimDate.ToString("yyyy-MM-dd"),
                    EndDate: null,
                    Provider: null,
                    Description: c.Description,
                    Amount: c.Amount
                )));
        }

        return items
            .OrderBy(t => t.sortKey)
            .Select(t => t.item)
            .ToList();
    }
}
