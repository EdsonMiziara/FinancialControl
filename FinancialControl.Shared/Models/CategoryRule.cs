namespace FinancialControl.Shared.Models;

public class CategoryRule
{
    public int Id { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; }

    public string KeyWord { get; set; }
    public int weight { get; set; }
}
