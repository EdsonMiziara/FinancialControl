namespace FinancialControl.API.Controllers;
using FinancialControl.Shared.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/categorization")]
public class CategorizationController : ControllerBase
{
    private readonly CategorizerService _categorizer;

    public CategorizationController(CategorizerService categorizer)
    {
        _categorizer = categorizer;
    }

    [HttpPost]
    public IActionResult Categorize([FromBody] Request req)
    {
        var categoria = _categorizer.Identify(req.Descricao, req.Valor);

        return Ok(new { categoria });
    }

    [HttpPost("learn")]
    public async Task<IActionResult> Learn([FromBody] LearnRequest req)
    {
        await _categorizer.LearnAsync(req.Descricao, req.CategoriaId);
        return Ok();
    }
}

public class Request
{
    public string Descricao { get; set; }
    public decimal Valor { get; set; }
}

public class LearnRequest
{
    public string Descricao { get; set; }
    public int CategoriaId { get; set; }
}
