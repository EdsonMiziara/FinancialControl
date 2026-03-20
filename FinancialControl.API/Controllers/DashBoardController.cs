namespace FinancialControl.API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Constructor for the DashboardController, which takes an AppDbContext
    /// as a dependency to access the database and perform queries related to financial transactions for generating dashboard summaries and reports.
    /// </summary>
    /// <param name="context"></param>
    
    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Generates a summary of financial transactions grouped by category, calculating the total amount for each category.
    /// This endpoint is useful for providing an overview of expenses and income categorized by their respective types,
    /// allowing users to quickly understand their financial distribution across different categories.
    /// </summary>
    /// <returns>
    /// Returns an HTTP 200 OK response containing a list of categories and their corresponding total amounts,
    /// which can be used to display a summary of financial transactions on the dashboard.
    /// </returns>
    
    [HttpGet("summary")]
    public async Task<IActionResult> GetResumo()
    {
        var data = await _context.Transacoes
            .GroupBy(t => t.Category)
            .Select(g => new
            {
                categoria = g.Key,
                total = g.Sum(x => x.Value)
            })
            .ToListAsync();

        return Ok(data);
    }

    /// <summary>
    /// Generates a comprehensive report of all financial transactions, including total income,
    /// total expenses, balance, and breakdowns by category and month.
    /// </summary>
    /// <returns>
    /// Returns an HTTP 200 OK response containing a detailed report of financial transactions, which includes:
    /// </returns>
    [HttpGet("alltransactions")]
    public async Task<IActionResult> AllTransactions()
    {
        var transacoes = await _context.Transacoes.ToListAsync();

        var totalEntrada = transacoes.Where(x => x.Value > 0).Sum(x => x.Value);
        var totalSaida = transacoes.Where(x => x.Value < 0).Sum(x => x.Value);

        var porCategoria = transacoes
            .GroupBy(x => x.Category)
            .Select(g => new
            {
                categoria = g.Key,
                total = g.Sum(x => x.Value)
            });

        var mensal = transacoes
            .GroupBy(x => new { x.Date.Year, x.Date.Month })
            .Select(g => new
            {
                mes = $"{g.Key.Month}/{g.Key.Year}",
                total = g.Sum(x => x.Value)
            });

        return Ok(new
        {
            totalEntrada,
            totalSaida,
            saldo = totalEntrada + totalSaida,
            porCategoria,
            mensal
        });
    }
}
