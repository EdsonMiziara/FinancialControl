using Dapper;
using FinancialControl.Shared.Interfaces;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace FinancialControl.Shared.SupportModels.Repository;

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
    /// <param name="categoryId"></param>
    /// <returns>
    /// Returns the Id of the existing category if it exists, or the Id of the "Extra" category if the original category does not exist.
    /// </returns>
    
    public async Task<int> EnsureCategoryAsync(int categoryId)
    {
        using var connection = new MySqlConnection(_connectionString);

        var exists = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM categories WHERE Id = @Id",
            new { Id = categoryId });

        if (exists > 0)
            return categoryId;

        var extraCategory = await connection.QueryFirstOrDefaultAsync<int?>(
            "SELECT Id FROM categories WHERE Name = 'Extra' LIMIT 1");

        if (!extraCategory.HasValue)
        {
            extraCategory = await connection.ExecuteScalarAsync<int>(
                @"INSERT INTO categorias (Nome) VALUES ('Extra');
                  SELECT LAST_INSERT_ID();");
        }

        return extraCategory.Value;
    }

    /// <summary>
    /// Verify if a category with the same name already exists (ignoring case).
    /// </summary>
    /// <param name="name"></param>
    /// <returns>
    /// Returns the Id of the existing category if it exists, or creates a new category and returns its Id if it does not exist.
    /// </returns>
    
    public async Task<int> VerifyCategoryAsync(string name)
    {
        using var connection = new MySqlConnection(_connectionString);

        var id = await connection.QueryFirstOrDefaultAsync<int?>(
            "SELECT Id FROM categories WHERE Name = @Name",
            new { Nome = name });

        if (id.HasValue)
            return id.Value;

        var newId = await connection.ExecuteScalarAsync<int>(
            @"INSERT INTO categories (Name) VALUES (@Name);
              SELECT LAST_INSERT_ID();",
            new { Nome = name });

        return newId;
    }

    /// <summary>
    /// Get the name of the category based on the Id, or "Extra" if the category does not exist.
    /// </summary>
    /// <param name="categoryId"></param>
    /// <returns>
    /// Returns the name of the category based on the Id, or "Extra" if the category does not exist.
    /// </returns>
    
    public async Task<string> GetCategoryNameAsync(int categoryId)
    {
        using var conexao = new MySqlConnection(_connectionString);

        var name = await conexao.QueryFirstOrDefaultAsync<string>(
            "SELECT Name FROM categories WHERE Id = @Id",
            new { Id = categoryId });

        return name ?? "Extra";
    }

    /// <summary>
    /// Get a dictionary of all categories, where the key is the Id and the value is the Name.
    /// </summary>
    /// <returns>
    /// Returns a dictionary of all categories, where the key is the Id and the value is the Name.
    /// </returns>
    
    public async Task<Dictionary<int, string>> GetCategoriesAsync()
    {
        using var connection = new MySqlConnection(_connectionString);

        var categories = await connection.QueryAsync<(int Id, string Name)>(
            "SELECT Id, Name FROM categories");

        return categories.ToDictionary(c => c.Id, c => c.Name);
    }
}
