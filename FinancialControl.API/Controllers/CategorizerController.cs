namespace FinancialControl.API.Controllers;
using FinancialControl.Shared.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/categorization")]
public class CategorizationController : ControllerBase
{
    private readonly CategorizerService _categorizer;

    /// <summary>
    /// Constructor for the CategorizationController, which takes a CategorizerService as a dependency.
    /// </summary>
    /// <param name="categorizer"></param>
    
    public CategorizationController(CategorizerService categorizer)
    {
        _categorizer = categorizer;
    }

    /// <summary>
    /// Categorizes a financial transaction based on its description and value.
    /// It uses the CategorizerService to identify the appropriate category for the transaction and returns it in the response.
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    
    [HttpPost]
    public IActionResult Categorize([FromBody] Request req)
    {
        var categoria = _categorizer.Identify(req.Descricao, req.Valor);

        return Ok(new { categoria });
    }

    /// <summary>
    /// Learns from a given description and category ID, allowing the CategorizerService to improve its categorization accuracy over time.
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    
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
