using Dapper;
using FinancialControl.Shared.Interfaces;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace FinancialControl.Shared.SupportModels;

public class CategoryRepository : ICategoryRepository
{
    protected readonly string _connectionString;

    /// <summary>
    /// Constructor for CategoryRepository that initializes the repository with a database connection string
    /// </summary>
    /// <param name="configuration"></param>
    public CategoryRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    /// <summary>
    /// Verifys if the category with the given Id exists.
    /// If it doesn't, ensures that the "Extra" category exists and returns its Id.
    /// If the category exists, returns its Id.
    /// </summary>
    /// <param name="categoriaId"></param>
    /// <returns>
    /// Returns the Id of the existing category if it exists, or the Id of the "Extra" category if the original category does not exist.
    /// </returns>
    
    public async Task<int> EnsureCategoryAsync(int categoriaId)
    {
        using var conexao = new MySqlConnection(_connectionString);

        var existe = await conexao.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM categorias WHERE Id = @Id",
            new { Id = categoriaId });

        if (existe > 0)
            return categoriaId;

        var categoriaExtra = await conexao.QueryFirstOrDefaultAsync<int?>(
            "SELECT Id FROM categorias WHERE Nome = 'Extra' LIMIT 1");

        if (!categoriaExtra.HasValue)
        {
            categoriaExtra = await conexao.ExecuteScalarAsync<int>(
                @"INSERT INTO categorias (Nome) VALUES ('Extra');
                  SELECT LAST_INSERT_ID();");
        }

        return categoriaExtra.Value;
    }

    /// <summary>
    /// Verify if a category with the same name already exists (ignoring case).
    /// </summary>
    /// <param name="nome"></param>
    /// <returns>
    /// Returns the Id of the existing category if it exists, or creates a new category and returns its Id if it does not exist.
    /// </returns>
    
    public async Task<int> VerifyCategoryAsync(string nome)
    {
        using var conexao = new MySqlConnection(_connectionString);

        var id = await conexao.QueryFirstOrDefaultAsync<int?>(
            "SELECT Id FROM categorias WHERE Nome = @Nome",
            new { Nome = nome });

        if (id.HasValue)
            return id.Value;

        var newId = await conexao.ExecuteScalarAsync<int>(
            @"INSERT INTO categorias (Nome) VALUES (@Nome);
              SELECT LAST_INSERT_ID();",
            new { Nome = nome });

        return newId;
    }

    /// <summary>
    /// Get the name of the category based on the Id, or "Extra" if the category does not exist.
    /// </summary>
    /// <param name="categoriaId"></param>
    /// <returns>
    /// Returns the name of the category based on the Id, or "Extra" if the category does not exist.
    /// </returns>
    
    public async Task<string> GetCategoryNameAsync(int categoriaId)
    {
        using var conexao = new MySqlConnection(_connectionString);

        var nome = await conexao.QueryFirstOrDefaultAsync<string>(
            "SELECT Nome FROM categorias WHERE Id = @Id",
            new { Id = categoriaId });

        return nome ?? "Extra";
    }

    /// <summary>
    /// Get a dictionary of all categories, where the key is the Id and the value is the Name.
    /// </summary>
    /// <returns>
    /// Returns a dictionary of all categories, where the key is the Id and the value is the Name.
    /// </returns>
    
    public async Task<Dictionary<int, string>> GetCategoriesAsync()
    {
        using var conexao = new MySqlConnection(_connectionString);

        var categorias = await conexao.QueryAsync<(int Id, string Nome)>(
            "SELECT Id, Nome FROM categorias");

        return categorias.ToDictionary(c => c.Id, c => c.Nome);
    }
}
