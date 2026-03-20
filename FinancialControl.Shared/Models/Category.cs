namespace FinancialControl.Shared.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; }

    public ICollection<CategoryRule> Rules { get; set; }
}
