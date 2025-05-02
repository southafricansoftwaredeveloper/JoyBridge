using System.ComponentModel.DataAnnotations;

namespace Contracts.Messages;

public record PatientEvent
{
    [Key]
    public long Id { get; set; }
    public string  PatientId  { get; init; } = default!;
    public string  DeviceId   { get; init; } = default!;
    public DateTimeOffset Timestamp { get; init; }
    public double  HeartRate  { get; init; }
    public double  SpO2       { get; init; }
}
