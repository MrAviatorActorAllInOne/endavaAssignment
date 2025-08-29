using CarInsurance.Api.Data;
using CarInsurance.Api.Models;
using CarInsurance.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarInsurance.Tests
{
    public sealed class FakeClock : IClock
    {
        public DateTimeOffset Now { get; set; }
    }

    public class ScheduledTaskTest
    {
        private static (AppDbContext db, SqliteConnectionHandle handle) NewDb()
        {
            var handle = SqliteConnectionHandle.Create();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(handle.Connection)
                .Options;

            var db = new AppDbContext(options);
            db.Database.EnsureCreated();
            return (db, handle);
        }

        [Fact]
        public async Task LogsPoliciesThatEndedYesterday()
        {
            var (db, handle) = NewDb();
            try
            {
                var car = new Car { Id = 1, Vin = "VIN1", Make = "Dacia", Model = "Logan", YearOfManufacture = 2018, OwnerId = 1, Owner = new Owner { Id = 1, Name = "Ana", Email = "ana@example.com" } };
                db.Owners.Add(car.Owner);
                db.Cars.Add(car);

                var today = new DateOnly(2025, 8, 30);
                db.Policies.Add(new InsurancePolicy
                {
                    CarId = 1,
                    StartDate = today.AddDays(-30),
                    EndDate = today.AddDays(-1),
                    Provider = "AXA"
                });
                await db.SaveChangesAsync();

                var clock = new FakeClock { Now = new DateTimeOffset(new DateTime(2025, 8, 30, 0, 30, 0), TimeSpan.Zero) };
                var logger = NullLogger<PolicyExpirationJob>.Instance;
                var job = new PolicyExpirationJob(clock, logger);

                var count = await job.RunOnceAsync(db);
                Assert.Equal(1, count);

                var logged = await db.PolicyExpirationLogs.CountAsync();
                Assert.Equal(1, logged);
            }
            finally { handle.Dispose(); db.Dispose(); }
        }

        [Fact]
        public async Task IsIdempotent_DoesNotLogTwice()
        {
            var (db, handle) = NewDb();
            try
            {
                var car = new Car { Id = 1, Vin = "VIN1", Make = "VW", Model = "Golf", YearOfManufacture = 2020, OwnerId = 1, Owner = new Owner { Id = 1, Name = "B", Email = "b@example.com" } };
                db.Owners.Add(car.Owner);
                db.Cars.Add(car);

                var today = new DateOnly(2025, 8, 30);
                db.Policies.Add(new InsurancePolicy
                {
                    CarId = 1,
                    StartDate = today.AddDays(-10),
                    EndDate = today.AddDays(-1),
                    Provider = "Test"
                });
                await db.SaveChangesAsync();

                var clock = new FakeClock { Now = new DateTimeOffset(new DateTime(2025, 8, 30, 0, 30, 0), TimeSpan.Zero) };
                var job = new PolicyExpirationJob(clock, NullLogger<PolicyExpirationJob>.Instance);

                var n1 = await job.RunOnceAsync(db);
                var n2 = await job.RunOnceAsync(db);

                Assert.Equal(1, n1);
                Assert.Equal(0, n2);
            }
            finally { handle.Dispose(); db.Dispose(); }
        }

        [Fact]
        public async Task IgnoresPoliciesEndingTodayOrFuture()
        {
            var (db, handle) = NewDb();
            try
            {
                var owner = new Owner { Id = 1, Name = "X", Email = "x@example.com" };
                var car = new Car { Id = 1, Vin = "VIN2", Make = "Skoda", Model = "Octavia", YearOfManufacture = 2019, OwnerId = 1, Owner = owner };
                db.Owners.Add(owner);
                db.Cars.Add(car);

                var today = new DateOnly(2025, 8, 30);

                db.Policies.Add(new InsurancePolicy
                {
                    CarId = 1,
                    StartDate = today.AddDays(-5),
                    EndDate = today,
                    Provider = "T1"
                });

                db.Policies.Add(new InsurancePolicy
                {
                    CarId = 1,
                    StartDate = today,
                    EndDate = today.AddDays(3),
                    Provider = "T2"
                });

                await db.SaveChangesAsync();

                var clock = new FakeClock { Now = new DateTimeOffset(new DateTime(2025, 8, 30, 0, 30, 0), TimeSpan.Zero) };
                var job = new PolicyExpirationJob(clock, NullLogger<PolicyExpirationJob>.Instance);

                var n = await job.RunOnceAsync(db);
                Assert.Equal(0, n);

                var logged = await db.PolicyExpirationLogs.CountAsync();
                Assert.Equal(0, logged);
            }
            finally { handle.Dispose(); db.Dispose(); }
        }
    }
    internal sealed class SqliteConnectionHandle : IDisposable
    {
        public Microsoft.Data.Sqlite.SqliteConnection Connection { get; }

        private SqliteConnectionHandle(Microsoft.Data.Sqlite.SqliteConnection conn)
        {
            Connection = conn;
            Connection.Open();
        }

        public static SqliteConnectionHandle Create()
            => new SqliteConnectionHandle(new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:"));

        public void Dispose() => Connection.Dispose();
    }
}