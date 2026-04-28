using System.ComponentModel.DataAnnotations;

namespace AndonApp.Data.Models;

public class ScheduleBreak
{
    public int Id { get; set; }
    public int LineScheduleId { get; set; }
    public LineSchedule LineSchedule { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}
