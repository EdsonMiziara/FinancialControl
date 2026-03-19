namespace FinancialControl.Shared.Models;

public class AprendizadoCategoria
{
    public int Id { get; set; }
    public string Descricao { get; set; }
    public string DescricaoLimpa { get; set; }
    public int CategoriaId { get; set; }
    public int Vezes { get; set; } = 1;
}