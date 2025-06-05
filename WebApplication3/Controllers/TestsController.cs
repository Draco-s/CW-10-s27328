using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;
using Microsoft.EntityFrameworkCore;
using WebApplication3.DTO;
using WebApplication3.Models;

namespace WebApplication3.Controllers;

[ApiController]
[Route("api/[controller]/")]

public class TestsController(ApbdContext data) : ControllerBase
{
    [HttpGet("trips")]
    public async Task<IActionResult> GetTrips([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var totalCount = await data.Trips.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var trips = await data.Trips
            .Include(t => t.ClientTrips)
            .ThenInclude(ct => ct.IdClientNavigation)
            .Include(t => t.CountryTrips)
            .OrderByDescending(t => t.DateFrom)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.Name,
                t.Description,
                t.DateFrom,
                t.DateTo,
                t.MaxPeople,
                Countries = t.CountryTrips.Select(c => new
                {
                    Name = $"Country {c.IdCountry}" 
                }),
                Clients = t.ClientTrips.Select(c => new
                {
                    c.IdClientNavigation.FirstName,
                    c.IdClientNavigation.LastName
                })
            })
            .ToListAsync();

        return Ok(new
        {
            pageNum = page,
            pageSize,
            allPages = totalPages,
            trips
        });
    }

    [HttpDelete("clients/{idClient}")]
    public async Task<IActionResult> DeleteClient(int idClient)
    {
        var client = await data.Clients
            .Include(c => c.ClientTrips)
            .FirstOrDefaultAsync(c => c.IdClient == idClient);

        if (client == null)
            return NotFound("Client not found.");

        if (client.ClientTrips.Any())
            return BadRequest("Client has assigned trips and cannot be deleted.");

        data.Clients.Remove(client);
        await data.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("trips/{idTrip}/clients")]
    public async Task<IActionResult> AssignClientToTrip(int idTrip, [FromBody] AssignClientDto request)
    {
        if (idTrip != request.IdTrip)
            return BadRequest("Trip ID in URL does not match body.");

        var trip = await data.Trips.FindAsync(idTrip);
        if (trip == null)
            return NotFound("Trip not found.");

        if (trip.DateFrom <= DateTime.Now)
            return BadRequest("Cannot assign client to a past trip.");

        var existingClient = await data.Clients.FirstOrDefaultAsync(c => c.Pesel == request.Pesel);
        if (existingClient != null)
        {
            var alreadyAssigned = await data.ClientTrips
                .AnyAsync(ct => ct.IdClient == existingClient.IdClient && ct.IdTrip == idTrip);

            if (alreadyAssigned)
                return BadRequest("Client is already assigned to this trip.");
        }
        else
        {
            existingClient = new Client
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Telephone = request.Telephone,
                Pesel = request.Pesel
            };

            data.Clients.Add(existingClient);
            await data.SaveChangesAsync();
        }

        DateTimeOffset? paymentDate = null;
        if (!string.IsNullOrEmpty(request.PaymentDate))
        {
            if (DateTimeOffset.TryParse(request.PaymentDate, out var parsedDate))
            {
                paymentDate = parsedDate;
            }
            else
            {
                return BadRequest("Invalid PaymentDate format.");
            }
        }

        var clientTrip = new ClientTrip
        {
            IdClient = existingClient.IdClient,
            IdTrip = idTrip,
            RegisteredAt = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            PaymentDate = paymentDate.HasValue ? (int?)paymentDate.Value.ToUnixTimeSeconds() : null
        };

        data.ClientTrips.Add(clientTrip);
        await data.SaveChangesAsync();

        return Ok("Client successfully assigned to the trip.");
    }
}