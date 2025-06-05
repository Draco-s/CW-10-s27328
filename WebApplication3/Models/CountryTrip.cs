using System;
using System.Collections.Generic;

namespace WebApplication3.Models;

public partial class CountryTrip
{
    public int IdCountry { get; set; }

    public int IdTrip { get; set; }

    public virtual Trip IdTripNavigation { get; set; } = null!;
}
