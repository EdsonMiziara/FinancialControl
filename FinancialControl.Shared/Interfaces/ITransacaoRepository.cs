using FinancialControl.Shared.Models;

namespace FinancialControl.Shared.Interfaces;

public interface ITransacaoRepository
{
    Task InsertAsync(Transacao transacao);
    Task<int> GetOrCreateCategoryIdAsync(string nome);
    Task<string> GetCategoriaNomeAsync(int categoriaId);
    Task<Dictionary<int, string>> GetCategoriasAsync();
}