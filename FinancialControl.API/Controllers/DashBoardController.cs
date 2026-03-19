namespace FinancialControl.API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("resumo")]
    public async Task<IActionResult> GetResumo()
    {
        var data = await _context.Transacoes
            .GroupBy(t => t.Categoria)
            .Select(g => new
            {
                categoria = g.Key,
                total = g.Sum(x => x.Valor)
            })
            .ToListAsync();

        return Ok(data);
    }
    [HttpGet("completo")]
    public async Task<IActionResult> Completo()
    {
        var transacoes = await _context.Transacoes.ToListAsync();

        var totalEntrada = transacoes.Where(x => x.Valor > 0).Sum(x => x.Valor);
        var totalSaida = transacoes.Where(x => x.Valor < 0).Sum(x => x.Valor);

        var porCategoria = transacoes
            .GroupBy(x => x.Categoria)
            .Select(g => new
            {
                categoria = g.Key,
                total = g.Sum(x => x.Valor)
            });

        var mensal = transacoes
            .GroupBy(x => new { x.Data.Year, x.Data.Month })
            .Select(g => new
            {
                mes = $"{g.Key.Month}/{g.Key.Year}",
                total = g.Sum(x => x.Valor)
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
