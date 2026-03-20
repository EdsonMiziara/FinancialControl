namespace FinancialControl.Shared.Models;

public class CategoryLearning
{
    public int Id { get; set; }
    public string Description { get; set; }
    public string CleanDescription { get; set; }
    public int CategoryId { get; set; }
    public int Count { get; set; } = 1;
}