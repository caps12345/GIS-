using System;
using System.Collections.Generic;

namespace WeatherApp_FGH.Models;

public partial class WeatherStation
{
    public int StationId { get; set; }

    public string City { get; set; } = null!;

    public string District { get; set; } = null!;

    public string StationCode { get; set; } = null!;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<WeatherDatum> WeatherData { get; set; } = new List<WeatherDatum>();
}
