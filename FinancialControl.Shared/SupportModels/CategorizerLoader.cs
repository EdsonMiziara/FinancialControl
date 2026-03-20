using FinancialControl.Shared.CacheHolder;
using FinancialControl.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace FinancialControl.Shared.SupportModels;
public class CategorizerLoader
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Constructor for CategorizerLoader that initializes the loader with a database context.
    /// </summary>
    /// <param name="context"></param>
    public CategorizerLoader(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Loads the categorizer cache by fetching categories and their associated rules,
    /// as well as learning data from the database.
    /// This method constructs a CategorizerCache object that can be used for
    /// efficient categorization of financial transactions based on the loaded data.
    /// </summary>
    /// <returns>
    /// Returns a Task that represents the asynchronous operation of loading the categorizer cache.
    /// </returns>
    public async Task<CategorizerCache> LoadAsync()
    {
        var categories = await _context.Categories
            .Include(c => c.Rules)
            .ToListAsync();

        var learnings = await _context.Set<CategoryLearning>().ToListAsync();

        return new CategorizerCache
        {
            Categories = categories.Select(c => new CategoriaCache
            {
                Id = c.Id,
                Name = c.Name,
                Rules = c.Rules.Select(r => new RegraCache
                {
                    Term = Categorizer.CleanText(r.KeyWord),
                    Weight = r.weight
                }).ToList()
            }).ToList(),

            Learnings = learnings.Select(a => new AprendizadoCache
            {
                Description = a.CleanDescription,
                CategoryId = a.CategoryId,
                Weight = a.Count
            }).ToList()
        };
    }
}
