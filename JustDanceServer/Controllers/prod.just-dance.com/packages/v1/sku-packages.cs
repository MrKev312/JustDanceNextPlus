using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod.just_dance.com.packages.v1;

[ApiController]
[Route("packages/v1")]
public sealed class SkuPackages(IJduPackageService packageService) : ControllerBase
{
    [HttpGet("sku-packages")]
    public IActionResult GetPackages([FromHeader(Name = "X-SkuId")] string? skuId)
        => Ok(packageService.CreateCatalog(skuId));

    [HttpGet("maps/{assetPlatform}/{packageFile}")]
    public IActionResult GetPackage(
        [FromRoute] string assetPlatform,
        [FromRoute] string packageFile)
    {
        if (!packageFile.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            return NotFound();

        string archiveMd5 = packageFile[..^".zip".Length];
        JduPackageArchive? archive = packageService.GetArchive(archiveMd5, assetPlatform);
        return archive is null
            ? NotFound()
            : File(archive.Contents, "application/zip", archive.FileName, enableRangeProcessing: true);
    }

    [HttpHead("maps/{assetPlatform}/{packageFile}")]
    public IActionResult HeadPackage(
        [FromRoute] string assetPlatform,
        [FromRoute] string packageFile)
    {
        if (!packageFile.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            return NotFound();

        string archiveMd5 = packageFile[..^".zip".Length];
        JduPackageArchiveMetadata? archive = packageService.GetArchiveMetadata(archiveMd5, assetPlatform);
        if (archive is null)
            return NotFound();

        Response.ContentType = "application/zip";
        Response.ContentLength = archive.Length;
        Response.Headers.AcceptRanges = "bytes";
        return Ok();
    }
}
