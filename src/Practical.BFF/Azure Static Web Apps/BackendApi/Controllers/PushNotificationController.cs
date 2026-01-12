using BackendApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PushNotificationController : ControllerBase
{
    private readonly IOneSignalService _oneSignalService;

    public PushNotificationController(IOneSignalService oneSignalService)
    {
        _oneSignalService = oneSignalService;
    }

    [AllowAnonymous]
    [HttpGet("send")]
    public async Task<ActionResult<PushNotificationResponse>> SendNotification(string externalId, string message)
    {
        var response = await _oneSignalService.SendNotificationToExternalIdAsync(new PushNotificationRequest
        {
            ExternalId = externalId,
            Title = "Notification Title",
            Message = message,
            Url = "http://localhost:3000/products"
        });

        if (response.Success)
        {
            return Ok(response);
        }
        else
        {
            return StatusCode(StatusCodes.Status500InternalServerError, response);
        }
    }
}
