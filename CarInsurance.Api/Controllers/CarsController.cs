using CarInsurance.Api.Dtos;
using CarInsurance.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CarInsurance.Api.Controllers;
using System.Globalization;

[ApiController]
[Route("api")]
public class CarsController(CarService service) : ControllerBase
{
    private readonly CarService _service = service;

    [HttpGet("cars")]
    public async Task<ActionResult<List<CarDto>>> GetCars()
        => Ok(await _service.ListCarsAsync());

    [HttpGet("cars/{carId:long}/insurance-valid")]
    public async Task<ActionResult<InsuranceValidityResponse>> IsInsuranceValid(long carId, [FromQuery] string date)
    {
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return BadRequest("Invalid date format. Use YYYY-MM-DD.");

        try
        {
            var valid = await _service.IsInsuranceValidAsync(carId, parsed);
            return Ok(new InsuranceValidityResponse(carId, parsed.ToString("yyyy-MM-dd"), valid));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("cars/{carId:long}/claims")]
    public async Task<ActionResult<ClaimDto>> CreateClaim(long carId, [FromBody] CreateClaimRequest req)
    {
        if (!DateOnly.TryParse(req.ClaimDate, out var date))
            return BadRequest("Invalid date format. Use YYYY-MM-DD.");

        if (req.Amount <= 0)
            return BadRequest("Amount must be positive.");

        if (string.IsNullOrWhiteSpace(req.Description))
            return BadRequest("Description is required.");

        try
        {
            var created = await _service.RegisterClaimAsync(carId, date, req.Description, req.Amount);

            return Created(string.Empty, created);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("cars/{carId:long}/history")]
    public async Task<ActionResult<List<CarHistoryItem>>> GetCarHistory(long carId)
    {
        try
        {
            var timeline = await _service.GetCarHistoryAsync(carId);
            return Ok(timeline);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
