using CarInsurance.Api.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarInsurance.Tests
{
    public class InsuranceValidityServiceTests
    {
        [Fact]
        public async Task OnStartDate_IsValid()
        {
            var (db, conn) = TestHelpers.CreateDb();
            try
            {
                TestHelpers.SeedBasic(db);
                var svc = new CarService(db);

                var ok = await svc.IsInsuranceValidAsync(1, new DateOnly(2024, 6, 1));
                Assert.True(ok);
            }
            finally { conn.Dispose(); db.Dispose(); }
        }

        [Fact]
        public async Task DayBeforeStart_IsInvalid()
        {
            var (db, conn) = TestHelpers.CreateDb();
            try
            {
                TestHelpers.SeedBasic(db);
                var svc = new CarService(db);

                var ok = await svc.IsInsuranceValidAsync(1, new DateOnly(2024, 5, 31));
                Assert.False(ok);
            }
            finally { conn.Dispose(); db.Dispose(); }
        }

        [Fact]
        public async Task OnEndDate_IsValid()
        {
            var (db, conn) = TestHelpers.CreateDb();
            try
            {
                TestHelpers.SeedBasic(db);
                var svc = new CarService(db);

                var ok = await svc.IsInsuranceValidAsync(1, new DateOnly(2024, 6, 30));
                Assert.True(ok);
            }
            finally { conn.Dispose(); db.Dispose(); }
        }

        [Fact]
        public async Task DayAfterEnd_IsInvalid()
        {
            var (db, conn) = TestHelpers.CreateDb();
            try
            {
                TestHelpers.SeedBasic(db);
                var svc = new CarService(db);

                var ok = await svc.IsInsuranceValidAsync(1, new DateOnly(2024, 7, 1));
                Assert.False(ok);
            }
            finally { conn.Dispose(); db.Dispose(); }
        }

        [Fact]
        public async Task NonexistentCar_ThrowsKeyNotFound()
        {
            var (db, conn) = TestHelpers.CreateDb();
            try
            {
                TestHelpers.SeedBasic(db);
                var svc = new CarService(db);

                await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                    svc.IsInsuranceValidAsync(999, new DateOnly(2024, 6, 1)));
            }
            finally { conn.Dispose(); db.Dispose(); }
        }
    }
}
