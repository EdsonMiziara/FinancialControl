using FinancialControl.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinancialControl.API.Controllers;

[ApiController]
[Route("api/ofx")]
public class OfxController : ControllerBase
{
    private readonly FileService _fileService;

    public OfxController(FileService fileService)
    {
        _fileService = fileService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Arquivo inválido");

        var tempPath = Path.GetTempFileName();

        using (var stream = new FileStream(tempPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // 🔥 PROCESSA DIRETO (sem Excel)
        int adicionados = await _fileService.ProcessSingleOfx(tempPath);

        return Ok(new { adicionados });
    }
}
