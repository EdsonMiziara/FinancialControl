using FinancialControl.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinancialControl.API.Controllers;

[ApiController]
[Route("api/ofx")]
public class OfxController : ControllerBase
{
    private readonly FileService _fileService;

    /// <summary>
    /// Constructor for the OfxController, which takes a FileService as a dependency to handle file processing related to OFX files.
    /// </summary>
    public OfxController(FileService fileService)
    {
        _fileService = fileService;
    }

    /// <summary>
    /// Uploads an OFX file, processes it, and returns the number of transactions added to the system.
    /// </summary>
    /// <param name="file"></param>
    /// <returns>
    /// Returns an HTTP 200 OK response containing the number of transactions added to the system after processing the uploaded OFX file.
    /// If the file is invalid, it returns a Bad Request response with an appropriate error message.
    /// </returns>
    
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

        int adicionados = await _fileService.ProcessSingleOfx(tempPath);

        return Ok(new { adicionados });
    }
}
