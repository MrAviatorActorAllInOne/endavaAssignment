using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using CarInsurance.Api.Data;
using CarInsurance.Api.Models;

namespace CarInsurance.Tests;

public static class TestHelpers
{
    public static (AppDbContext db, SqliteConnection conn) CreateDb()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open(); // keep open for the lifetime of the context

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conn)
            .Options;

        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return (db, conn);
    }

    public static void SeedBasic(AppDbContext db)
    {
        var owner = new Owner { Id = 1, Name = "Ana", Email = "ana@example.com" };
        var car = new Car { Id = 1, Vin = "VIN1", Make = "Dacia", Model = "Logan", YearOfManufacture = 2018, OwnerId = 1, Owner = owner };

        db.Owners.Add(owner);
        db.Cars.Add(car);

        db.Policies.Add(new InsurancePolicy
        {
            Id = 1,
            CarId = 1,
            StartDate = new DateOnly(2024, 6, 1),
            EndDate = new DateOnly(2024, 6, 30),
            Provider = "AXA"
        });

        db.SaveChanges();
    }
}
