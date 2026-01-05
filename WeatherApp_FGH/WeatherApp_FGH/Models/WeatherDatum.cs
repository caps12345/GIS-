using System;
using System.Collections.Generic;

namespace WeatherApp_FGH.Models;

public partial class WeatherDatum
{
    public int DataId { get; set; }

    public string StationCode { get; set; } = null!;

    public DateOnly RecordDate { get; set; }

    public decimal? MaxTemperature { get; set; }

    public decimal? MinTemperature { get; set; }

    public decimal? TemperatureRange { get; set; }

    public decimal? AvgTemperature { get; set; }

    public int? AvgHumidity { get; set; }

    public string? WeatherCondition { get; set; }

    public string? WindDirectionSpeed { get; set; }

    public decimal? Precipitation24h { get; set; }

    public decimal? FeelsLikeTemperature { get; set; }

    public string? SunriseSunset { get; set; }

    public string? MoonriseMoonset { get; set; }

    public int? Aqi { get; set; }

    public double? DaylightDuration { get; set; }

    public double? SunshineDuration { get; set; }

    public decimal? SolarRadiation { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual WeatherStation StationCodeNavigation { get; set; } = null!;
}
