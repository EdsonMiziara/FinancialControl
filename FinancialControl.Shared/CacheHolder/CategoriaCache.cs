namespace FinancialControl.Shared.CacheHolder;

public class CategoriaCache
{
    public int Id { get; set; }
    public string Name { get; set; } 
    public List<RegraCache> Rules { get; set; }
}
