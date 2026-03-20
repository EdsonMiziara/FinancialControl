namespace FinancialControl.Shared.Models;

public class LearningCategory
{
    public int Id { get; set; }
    public string Description { get; set; }
    public string CleanDescription { get; set; }
    public int CategoryId { get; set; }
    public int Times { get; set; } = 1;
}