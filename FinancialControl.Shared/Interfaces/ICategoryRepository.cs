namespace FinancialControl.Shared.Interfaces;

public interface ICategoryRepository
{
    Task<int> VerifyCategoryAsync(string nome);
    Task<string> GetCategoryNameAsync(int categoriaId);
    Task<Dictionary<int, string>> GetCategoriesAsync();
    Task<int> EnsureCategoryAsync(int categoriaId);
}
