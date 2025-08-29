using CarInsurance.Api.Controllers;
using CarInsurance.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarInsurance.Tests
{
    public class InsuranceValidityControllerTests
    {
        [Fact]
        public async Task BadDateFormat_Returns400()
        {
            var (db, conn) = TestHelpers.CreateDb();
            try
            {
                TestHelpers.SeedBasic(db);
                var ctrl = new CarsController(new CarService(db));

                var result = await ctrl.IsInsuranceValid(1, "06-01-2024");

                var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
                Assert.Equal(400, bad.StatusCode);
            }
            finally { conn.Dispose(); db.Dispose(); }
        }

        [Fact]
        public async Task ImpossibleDate_Returns400()
        {
            var (db, conn) = TestHelpers.CreateDb();
            try
            {
                TestHelpers.SeedBasic(db);
                var ctrl = new CarsController(new CarService(db));

                var result = await ctrl.IsInsuranceValid(1, "2024-02-30");

                var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
                Assert.Equal(400, bad.StatusCode);
            }
            finally { conn.Dispose(); db.Dispose(); }
        }

        [Fact]
        public async Task MissingCar_Returns404()
        {
            var (db, conn) = TestHelpers.CreateDb();
            try
            {
                TestHelpers.SeedBasic(db);
                var ctrl = new CarsController(new CarService(db));

                var result = await ctrl.IsInsuranceValid(999, "2024-06-01");

                var nf = Assert.IsType<NotFoundResult>(result.Result);
                Assert.Equal(404, nf.StatusCode);
            }
            finally { conn.Dispose(); db.Dispose(); }
        }
    }
}
