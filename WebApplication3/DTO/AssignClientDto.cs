namespace WebApplication3.DTO;

public class AssignClientDto
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Telephone { get; set; } = null!;
    public string Pesel { get; set; } = null!;
    public int IdTrip { get; set; }
    public string TripName { get; set; } = null!;
    public string? PaymentDate { get; set; } 
}