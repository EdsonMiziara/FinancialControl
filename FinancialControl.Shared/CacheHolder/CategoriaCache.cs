namespace FinancialControl.Shared.CacheHolder;

public class CategoriaCache
{
    public int Id { get; set; }
    public string Nome { get; set; } 
    public List<RegraCache> Regras { get; set; }
}
