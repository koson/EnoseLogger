using EnoseLogger.Services;
using Microsoft.AspNetCore.Mvc;

namespace EnoseLogger.Controllers;

[Route("[controller]")]
[ApiController]
public class DownloadController : ControllerBase
{
    private readonly SessionManager _sessions;
    private readonly ILogger<DownloadController> _logger;
    
    public DownloadController(SessionManager sessions, ILogger<DownloadController> logger)
    {
        _sessions = sessions;
        _logger = logger;
    }
    
    [HttpGet("{sessionId}")]
    public IActionResult GetCsv(string sessionId)
    {
        try
        {
            var csvPath = _sessions.GetCsvPath(sessionId);
            
            if (csvPath == null || !System.IO.File.Exists(csvPath))
            {
                _logger.LogWarning("CSV file not found for session: {SessionId}", sessionId);
                return NotFound($"Session {sessionId} not found");
            }
            
            var fileName = $"enose_{sessionId}.csv";
            var fileBytes = System.IO.File.ReadAllBytes(csvPath);
            
            _logger.LogInformation("📥 Downloading CSV: {FileName}", fileName);
            
            return File(fileBytes, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading CSV for session: {SessionId}", sessionId);
            return StatusCode(500, "Internal server error");
        }
    }
}
