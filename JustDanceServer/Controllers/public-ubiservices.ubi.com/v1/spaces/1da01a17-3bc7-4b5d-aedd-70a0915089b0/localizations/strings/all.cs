using JustDanceServer.Configuration;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using System.Security.Cryptography;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.spaces._1da01a17_3bc7_4b5d_aedd_70a0915089b0.localizations.strings;

[ApiController]
[Route("v1/spaces/{spaceId:jdnext}/localizations/strings/all")]
public class All(ITimingService timingService, IOptions<UrlSettings> urlSettings,
    ILocalizedStringService text, TimeProvider timeProvider) : ControllerBase
{
    [HttpGet(Name = "GetAll")]
    public IActionResult GetAll([FromRoute] Guid spaceId, [FromQuery] string? localeCode = null)
    {
        string locale = LocaleFallback.Normalize(localeCode ?? Request.Headers.AcceptLanguage.FirstOrDefault());
        string digest = Convert.ToHexStringLower(MD5.HashData(LocalizationDocument.Create(text, locale)));
        string cdn = urlSettings.Value.CDNUrl.TrimEnd('/');
        if (!cdn.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            && !cdn.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            cdn = $"https://{cdn}";
        return Ok(new
        {
            GetUrl = $"{cdn}/editorial/v1/All.{locale}.json.gz",
            GetUrlExpiresAt = timingService.TimeString(timeProvider.GetUtcNow().UtcDateTime.AddHours(1)),
            LocaleCode = locale,
            Md5 = digest,
            Revision = (int)(Convert.ToUInt32(digest[..8], 16) & int.MaxValue),
            SpaceRevision = 121
        });
    }
}
