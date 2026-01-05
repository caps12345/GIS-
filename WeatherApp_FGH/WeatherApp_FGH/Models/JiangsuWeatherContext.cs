using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace WeatherApp_FGH.Models;

public partial class JiangsuWeatherContext : DbContext
{
    public JiangsuWeatherContext()
    {
    }

    public JiangsuWeatherContext(DbContextOptions<JiangsuWeatherContext> options)
        : base(options)
    {
    }

    public virtual DbSet<WeatherDatum> WeatherData { get; set; }

    public virtual DbSet<WeatherStation> WeatherStations { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlite("Data Source=jiangsu_weather.db");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WeatherDatum>(entity =>
        {
            entity.HasKey(e => e.DataId);

            entity.ToTable("weather_data");

            entity.HasIndex(e => new { e.StationCode, e.RecordDate }, "idx_data_station_date");

            entity.HasIndex(e => new { e.StationCode, e.RecordDate }, "idx_data_unique").IsUnique();

            entity.Property(e => e.DataId).HasColumnName("data_id");
            entity.Property(e => e.Aqi).HasColumnName("aqi");
            entity.Property(e => e.AvgHumidity)
                .HasColumnType("DECIMAL(4,1)")
                .HasColumnName("avg_humidity");
            entity.Property(e => e.AvgTemperature)
                .HasColumnType("DECIMAL(4,1)")
                .HasColumnName("avg_temperature");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("DATETIME")
                .HasColumnName("created_at");
            entity.Property(e => e.DaylightDuration)
                .HasColumnType("INTEGER")
                .HasColumnName("daylight_duration");
            entity.Property(e => e.FeelsLikeTemperature)
                .HasColumnType("DECIMAL(4,1)")
                .HasColumnName("feels_like_temperature");
            entity.Property(e => e.MaxTemperature)
                .HasColumnType("DECIMAL(4,1)")
                .HasColumnName("max_temperature");
            entity.Property(e => e.MinTemperature)
                .HasColumnType("DECIMAL(4,1)")
                .HasColumnName("min_temperature");
            entity.Property(e => e.MoonriseMoonset)
                .HasColumnType("VARCHAR(20)")
                .HasColumnName("moonrise_moonset");
            entity.Property(e => e.Precipitation24h)
                .HasColumnType("DECIMAL(6,1)")
                .HasColumnName("precipitation_24h");
            entity.Property(e => e.RecordDate)
                .HasColumnType("DATE")
                .HasColumnName("record_date");
            entity.Property(e => e.SolarRadiation)
                .HasColumnType("DECIMAL(8,2)")
                .HasColumnName("solar_radiation");
            entity.Property(e => e.StationCode)
                .HasColumnType("VARCHAR(20)")
                .HasColumnName("station_code");
            entity.Property(e => e.SunriseSunset)
                .HasColumnType("VARCHAR(20)")
                .HasColumnName("sunrise_sunset");
            entity.Property(e => e.SunshineDuration)
                .HasColumnType("INTEGER")
                .HasColumnName("sunshine_duration");
            entity.Property(e => e.TemperatureRange)
                .HasColumnType("DECIMAL(4,1)")
                .HasColumnName("temperature_range");
            entity.Property(e => e.WeatherCondition)
                .HasColumnType("VARCHAR(20)")
                .HasColumnName("weather_condition");
            entity.Property(e => e.WindDirectionSpeed)
                .HasColumnType("VARCHAR(50)")
                .HasColumnName("wind_direction_speed");

            entity.HasOne(d => d.StationCodeNavigation).WithMany(p => p.WeatherData)
                .HasPrincipalKey(p => p.StationCode)
                .HasForeignKey(d => d.StationCode)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<WeatherStation>(entity =>
        {
            entity.HasKey(e => e.StationId);

            entity.ToTable("weather_stations");

            entity.HasIndex(e => e.StationCode, "IX_weather_stations_station_code").IsUnique();

            entity.HasIndex(e => e.City, "idx_stations_city");

            entity.HasIndex(e => e.District, "idx_stations_district");

            entity.Property(e => e.StationId).HasColumnName("station_id");
            entity.Property(e => e.City)
                .HasColumnType("VARCHAR(20)")
                .HasColumnName("city");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("DATETIME")
                .HasColumnName("created_at");
            entity.Property(e => e.District)
                .HasColumnType("VARCHAR(20)")
                .HasColumnName("district");
            entity.Property(e => e.Latitude)
                .HasColumnType("DECIMAL(10, 6)")
                .HasColumnName("latitude");
            entity.Property(e => e.Longitude)
                .HasColumnType("DECIMAL(10, 6)")
                .HasColumnName("longitude");
            entity.Property(e => e.StationCode)
                .HasColumnType("VARCHAR(20)")
                .HasColumnName("station_code");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
