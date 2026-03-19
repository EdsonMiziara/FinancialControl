namespace FinancialControl.Shared.Models;

public class Transacao
{
    public int Id { get; set; }

    public DateTime Data { get; set; }
    public decimal Valor { get; set; }

    public string Descricao { get; set; }
    public string Tipo { get; set; }
    public int CategoriaId { get; set; }
    public Categoria Categoria { get; set; }

    public string NomeOriginal { get; set; }
}