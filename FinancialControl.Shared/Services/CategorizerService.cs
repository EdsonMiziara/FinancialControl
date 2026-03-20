using FinancialControl.Shared.CacheHolder;
using FinancialControl.Shared.Models;
using FuzzySharp;
using Microsoft.EntityFrameworkCore;

public class CategorizerService
{
    private readonly CategorizerCache _cache;
    private readonly AppDbContext _context;

    /// <summary>
    /// Constructor for CategorizerService that initializes the service with a categorizer cache and a database context.
    /// </summary>
    /// <param name="cache"></param>
    /// <param name="context"></param>
    public CategorizerService(CategorizerCache cache, AppDbContext context)
    {
        _cache = cache;
        _context = context;
    }

    /// <summary>
    /// Identifies the most likely category for a given description and value using a combination of direct matching,
    /// fuzzy matching, and learned associations.
    /// </summary>
    /// <param name="description"></param>
    /// <param name="value"></param>
    /// <returns>
    /// Returns the ID of the most likely category based on the provided description and value. The method first attempts
    /// </returns>
    
    public int Identify(string description, decimal value)
    {
        if (string.IsNullOrWhiteSpace(description))
            return 0;

        description = Categorizer.CleanText(description);

        var scores = new Dictionary<int, double>();

        // 1. MATCH DIRETO
        foreach (var categoria in _cache.Categorias)
        {
            double score = 0;

            foreach (var regra in categoria.Regras)
            {
                if (description.Contains(regra.Termo))
                {
                    score += 2 * regra.Peso;
                }
            }

            if (score > 0)
                scores[categoria.Id] = score;
        }

        // 2. FUZZY
        if (!scores.Any())
        {
            foreach (var categoria in _cache.Categorias)
            {
                double score = 0;

                foreach (var regra in categoria.Regras)
                {
                    int similarity = Fuzz.PartialRatio(description, regra.Termo);

                    if (similarity > 85)
                        score += 3 * regra.Peso;
                }

                if (score > 0)
                    scores[categoria.Id] = score;
            }
        }

        // 3. APRENDIZADO (PRIORIDADE)
        foreach (var aprendizado in _cache.Aprendizados)
        {
            int similarity = Fuzz.PartialRatio(description, aprendizado.Descricao);

            if (similarity > 90)
            {
                if (!scores.ContainsKey(aprendizado.CategoriaId))
                    scores[aprendizado.CategoriaId] = 0;

                scores[aprendizado.CategoriaId] += 10 * aprendizado.Peso;
            }
        }

        if (!scores.Any())
        {
            return 0; // categoria padrão
        }

        return scores.OrderByDescending(x => x.Value).First().Key;
    }

    /// <summary>
    /// Learns a new association between a cleaned description and a category ID. If the cleaned description already exists in the database,
    /// </summary>
    /// <param name="descricao"></param>
    /// <param name="categoriaId"></param>
    /// <returns>
    /// Returns a Task representing the asynchronous operation. 
    /// The method updates the database with the new association and also updates the in-memory cache for future identifications.
    /// If the cleaned description already exists, it increments the count of occurrences and updates the category ID; otherwise,
    /// it creates a new entry in the database and cache.
    /// </returns>
    
    public async Task LearnAsync(string descricao, int categoriaId)
    {
        var limpa = Categorizer.CleanText(descricao);

        var existente = await _context.Set<LearningCategory>()
            .FirstOrDefaultAsync(x => x.CleanDescription == limpa);

        if (existente != null)
        {
            existente.Times++;
            existente.CategoryId = categoriaId;
        }
        else
        {
            existente = new LearningCategory
            {
                Description = descricao,
                CleanDescription = limpa,
                CategoryId = categoriaId,
                Times = 1
            };

            _context.Add(existente);
        }

        await _context.SaveChangesAsync();

        _cache.Aprendizados.Add(new AprendizadoCache
        {
            Descricao = limpa,
            CategoriaId = categoriaId,
            Peso = existente.Times
        });
    }
}
