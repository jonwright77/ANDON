using System.ComponentModel.DataAnnotations;

namespace AndonApp.Data.Models;

public class LineSchedule
{
    public int Id { get; set; }
    public int ProductionLineId { get; set; }
    public ProductionLine ProductionLine { get; set; } = null!;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsWorkday { get; set; } = true;
    public ICollection<ScheduleBreak> Breaks { get; set; } = new List<ScheduleBreak>();
}
