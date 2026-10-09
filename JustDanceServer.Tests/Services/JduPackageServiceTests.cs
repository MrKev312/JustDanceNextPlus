using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

using Moq;

using System.IO.Compression;
using System.Security.Cryptography;

using SkuPackagesController = JustDanceServer.Controllers.prod.just_dance.com.packages.v1.SkuPackages;

namespace JustDanceServer.Tests.Services;
public sealed class JduPackageServiceTests
{
	[Fact]
	public void Packages_AreServedDirectlyFromThePrebuiltPlatformZip()
	{
		const string mapsPath = "maps";
		string mapDirectory = Path.Combine(mapsPath, "TestMap");
		string packageDirectory = Path.Combine(mapDirectory, "JDUnlimited", "Assets", "NX", "MainScene");
		using MemoryStream archiveOutput = new();
		using (ZipArchive output = new(archiveOutput, ZipArchiveMode.Create, leaveOpen: true))
		{
			foreach (string member in new[]
			{
				"TestMap_main_scene_nx.ipk",
				"atlascontainer.ckd",
				"dlcdescriptor.ckd",
				"secure_fat.gf",
				"sgscontainer.ckd"
			})
			{
				ZipArchiveEntry entry = output.CreateEntry(member);
				using StreamWriter writer = new(entry.Open());
				writer.Write(member);
			}
		}

		byte[] archiveBytes = archiveOutput.ToArray();
		string archiveMd5 = Convert.ToHexStringLower(MD5.HashData(archiveBytes));
		string archivePath = Path.Combine(packageDirectory, $"{archiveMd5}.zip");
		Mock<IFileSystem> fileSystem = new();
		fileSystem.Setup(system => system.DirectoryExists(mapsPath)).Returns(true);
		fileSystem.Setup(system => system.GetDirectories(mapsPath)).Returns([mapDirectory]);
		fileSystem.Setup(system => system.DirectoryExists(packageDirectory)).Returns(true);
		fileSystem.Setup(system => system.GetFiles(packageDirectory, "*.zip", SearchOption.TopDirectoryOnly))
			.Returns([archivePath]);
		fileSystem.Setup(system => system.GetFileLength(archivePath)).Returns(archiveBytes.Length);
		fileSystem.Setup(system => system.OpenRead(archivePath)).Returns(() => new MemoryStream(archiveBytes));

		JduPackageService service = new(
			Options.Create(new PathSettings { MapsPath = mapsPath }),
			Options.Create(new UrlSettings
			{
				HostUrl = "next-service.local.test",
				JduHostUrl = "jdu-service.local.test",
				CDNUrl = "cdn.local.test"
			}),
			fileSystem.Object,
			NullLogger<JduPackageService>.Instance);

		JduPackageEntry catalogEntry = Assert.Single(service.CreateCatalog("jd2018-nx-all")).Value;
		fileSystem.Verify(system => system.OpenRead(It.IsAny<string>()), Times.Never);

		JduPackageArchiveMetadata metadata = Assert.IsType<JduPackageArchiveMetadata>(
			service.GetArchiveMetadata(archiveMd5, "nx"));
		Assert.Equal(archiveBytes.Length, metadata.Length);
		Assert.Equal($"{archiveMd5}.zip", metadata.FileName);
		fileSystem.Verify(system => system.OpenRead(It.IsAny<string>()), Times.Never);

		JduPackageArchive archive = Assert.IsType<JduPackageArchive>(service.GetArchive(archiveMd5, "nx"));
		Assert.Equal($"https://cdn.local.test/packages/v1/maps/NX/{archiveMd5}.zip", catalogEntry.Url);
		Assert.Equal(archiveMd5, catalogEntry.Md5);
		Assert.True(catalogEntry.Version > 0);
		Assert.Equal($"{archiveMd5}.zip", archive.FileName);
		using Stream zipStream = archive.Contents;
		Assert.True(zipStream.CanSeek);
		Assert.Equal(archiveBytes.Length, zipStream.Length);
		fileSystem.Verify(system => system.GetDirectories(mapsPath), Times.Once);
		using ZipArchive zip = new(zipStream, ZipArchiveMode.Read);
		Assert.Equal(5, zip.Entries.Count);
		Assert.Contains(zip.Entries, entry => entry.Name == "TestMap_main_scene_nx.ipk");
		Assert.Contains(zip.Entries, entry => entry.Name == "secure_fat.gf");
	}

	[Theory]
	[InlineData("jd2017-nx", "NX")]
	[InlineData("jd2018-nx", "NX")]
	[InlineData("jd2018_nx_all", "NX")]
	[InlineData("jd2022-switch", "NX")]
	[InlineData("jd2022-orbis", "ORBIS")]
	[InlineData("jd2022-durango", "DURANGO")]
	public void PackagePlatform_UsesSkuTokensInsteadOfRequiringSurroundingHyphens(
		string skuId,
		string expectedPlatform)
		=> Assert.Equal(expectedPlatform, JduAssetPlatform.FromSkuId(skuId));

	[Fact]
	public void PackageController_HeadReturnsLengthWithoutReadingArchive()
	{
		const string archiveMd5 = "f76b90b2a2f7b0db15f2be53b4db5f42";
		Mock<IJduPackageService> packageService = new();
		packageService
			.Setup(service => service.GetArchiveMetadata(archiveMd5, "NX"))
			.Returns(new JduPackageArchiveMetadata(
				"PokerFaceALT_mapContent",
				$"{archiveMd5}.zip",
				800_000));
		SkuPackagesController controller = new(packageService.Object)
		{
			ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
		};

		IActionResult result = controller.HeadPackage("NX", $"{archiveMd5}.zip");
		Assert.IsType<OkResult>(result);
		Assert.Equal("application/zip", controller.Response.ContentType);
		Assert.Equal(800_000, controller.Response.ContentLength);
		Assert.Equal("bytes", controller.Response.Headers.AcceptRanges);
		packageService.Verify(service => service.GetArchive(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
	}

	[Fact]
	public async Task PackageControllerExecutesByteRangesAndDisposesTheArchiveStream()
	{
		const string md5 = "f76b90b2a2f7b0db15f2be53b4db5f42";
		byte[] bytes = [0, 1, 2, 3, 4, 5, 6];
		MemoryStream archive = new(bytes);
		Mock<IJduPackageService> packages = new();
		packages.Setup(value => value.GetArchive(md5, "NX"))
			.Returns(new JduPackageArchive("test", md5 + ".zip", archive));
		ServiceCollection services = new();
		services.AddLogging();
		services.AddControllers();
		using ServiceProvider provider = services.BuildServiceProvider();
		using MemoryStream output = new();
		DefaultHttpContext http = new() { RequestServices = provider };
		http.Request.Method = "GET";
		http.Request.Headers.Range = "bytes=2-4";
		http.Response.Body = output;
		SkuPackagesController controller = new(packages.Object)
		{
			ControllerContext = new() { HttpContext = http }
		};
		IActionResult result = controller.GetPackage("NX", md5 + ".zip");
		await result.ExecuteResultAsync(new ActionContext(http, new(), new()));
		Assert.Equal(206, http.Response.StatusCode);
		Assert.Equal("bytes 2-4/7", http.Response.Headers.ContentRange);
		Assert.Equal(3, http.Response.ContentLength);
		Assert.Equal(bytes[2..5], output.ToArray());
		Assert.False(archive.CanRead);
	}
}
