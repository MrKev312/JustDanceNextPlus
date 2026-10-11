using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod_next.just_dance.com.editorial.v1;

[ApiController]
[Route("editorial/v1/All.{locale}.json.gz")]
public class Localization(ILocalizedStringService localizedStringService) : ControllerBase
{
    [HttpGet]
    public IActionResult GetLocalization([FromRoute] string locale = "en-US")
        => File(LocalizationDocument.Create(localizedStringService, locale), "application/x-gzip");
}
