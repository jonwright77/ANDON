namespace AndonApp.Data.Models;

public class LineOperatorTarget
{
    public int Id { get; set; }
    public int ProductionLineId { get; set; }
    public ProductionLine ProductionLine { get; set; } = null!;
    public DateOnly Date { get; set; }
    public int Target { get; set; }
}
