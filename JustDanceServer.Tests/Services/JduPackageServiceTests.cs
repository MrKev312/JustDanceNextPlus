using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;
using JustDanceServer.Utilities;

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
		fileSystem.Setup(system => system.DirectoryExists(packageDirectory)).Returns(true);
		fileSystem.Setup(system => system.GetFiles(packageDirectory, "*.zip", SearchOption.TopDirectoryOnly))
			.Returns([archivePath]);
		fileSystem.Setup(system => system.GetFileLength(archivePath)).Returns(archiveBytes.Length);
		fileSystem.Setup(system => system.OpenRead(archivePath)).Returns(() => new MemoryStream(archiveBytes));

		JduPackageService service = new(
			CreateMapService(mapDirectory, "TestMap"),
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
		fileSystem.Verify(system => system.GetDirectories(It.IsAny<string>()), Times.Never);
		using ZipArchive zip = new(zipStream, ZipArchiveMode.Read);
		Assert.Equal(5, zip.Entries.Count);
		Assert.Contains(zip.Entries, entry => entry.Name == "TestMap_main_scene_nx.ipk");
		Assert.Contains(zip.Entries, entry => entry.Name == "secure_fat.gf");
	}

	[Theory]
	[InlineData("jd2017-nx-all", true, MapVideoCodec.Vp8)]
	[InlineData("jd2018-nx-all", true, MapVideoCodec.Vp9)]
	[InlineData("jd2022-nx-all", true, MapVideoCodec.Vp9)]
	[InlineData("jd2022-nx-all", false, MapVideoCodec.Vp8)]
	[InlineData("jd2022-ps5-all", true, MapVideoCodec.Vp8)]
	[InlineData("jd2022-scarlett-all", true, MapVideoCodec.Vp8)]
	[InlineData("jd2022-orbis-all", true, MapVideoCodec.Vp8)]
	[InlineData("jd2022-durango-all", true, MapVideoCodec.Vp8)]
	[InlineData("jd2018-wiiu-noe", true, MapVideoCodec.Vp8)]
	[InlineData("jd2017-pc-all", true, MapVideoCodec.Vp8)]
	public void PackageSelection_KeepsCatalogAndVideoAuthorizationTogether(
		string skuId,
		bool hasVp9,
		MapVideoCodec expectedCodec)
	{
		Assert.True(JduMediaClientProfiles.TryResolve(skuId, out JduMediaClientProfile? client));
		JduPackageService packages = CreatePackageService(client.AssetPlatform, hasVp8: true, hasVp9);
		JduPackageSelection selection = Assert.IsType<JduPackageSelection>(packages.SelectPackage("TestMap", client));
		JduPackageEntry catalogEntry = packages.CreateCatalog(skuId)["TestMap_mapContent"];
		Assert.Equal(expectedCodec, selection.VideoCodec);
		Assert.Equal(selection.Package, catalogEntry);
		Assert.Equal(new string(expectedCodec == MapVideoCodec.Vp9 ? 'b' : 'a', 32), catalogEntry.Md5);

		ServerMap map = ServerMapTestData.CreateMap(
			Guid.NewGuid(),
			Path.Combine("maps", "TestMap"),
			new JustDanceSongDBEntry { MapName = "TestMap", DanceVersionLocId = new OasisTag(new LocalizedString(1, "Standard")) });
		Mock<IMapService> maps = new();
		maps.Setup(service => service.GetMap("TestMap")).Returns(map);
		JduContentAuthorizationService authorization = new(maps.Object, packages, Options.Create(new UrlSettings { CDNUrl = "cdn.test" }));
		JduContentAuthorizationEntry content = Assert.IsType<JduContentAuthorizationEntry>(authorization.Create("TestMap", client));
		string codec = expectedCodec.ToCodecName();
		Assert.All(content.Urls.Where(entry => entry.Key.EndsWith(".webm", StringComparison.Ordinal)), entry =>
			Assert.Contains($"/Video/{codec}/", entry.Value, StringComparison.Ordinal));
		Assert.Equal(
			content.Urls["jmcs://jd-contents/TestMap/TestMap_LOW.webm"],
			content.Urls["jmcs://jd-contents/TestMap/TestMap_LOW.hd.webm"]);
	}

	[Fact]
	public void PackageSelection_RequiresTheVp8Baseline()
	{
		JduPackageService packages = CreatePackageService("NX", hasVp8: false, hasVp9: true);
		Assert.True(JduMediaClientProfiles.TryResolve("jd2022-nx-all", out JduMediaClientProfile? client));
		Assert.Empty(packages.CreateCatalog("jd2022-nx-all"));
		Assert.Null(packages.SelectPackage("TestMap", client));
		Assert.Null(packages.GetArchive(new string('b', 32), "NX"));

		ServerMap map = ServerMapTestData.CreateMap(
			Guid.NewGuid(),
			Path.Combine("maps", "TestMap"),
			new JustDanceSongDBEntry { MapName = "TestMap", DanceVersionLocId = new OasisTag(new LocalizedString(1, "Standard")) });
		Mock<IMapService> maps = new();
		maps.Setup(service => service.GetMap("TestMap")).Returns(map);
		JduContentAuthorizationService authorization = new(maps.Object, packages, Options.Create(new UrlSettings()));
		Assert.Null(authorization.Create("TestMap", client));
	}

	[Theory]
	[InlineData("jd2017-nx-all")]
	[InlineData("jd2022-nx-all")]
	public void PackageArchives_ResolveBothVariantsFromTheSamePlatformIndex(string firstSkuId)
	{
		JduPackageService packages = CreatePackageService("NX", hasVp8: true, hasVp9: true);
		Assert.Single(packages.CreateCatalog(firstSkuId));
		JduPackageEntry vp8 = packages.CreateCatalog("jd2017-nx-all")["TestMap_mapContent"];
		JduPackageEntry vp9 = packages.CreateCatalog("jd2022-nx-all")["TestMap_mapContent"];
		Assert.NotEqual(vp8.Md5, vp9.Md5);
		Assert.NotEqual(vp8.Version, vp9.Version);
		foreach (JduPackageEntry entry in new[] { vp8, vp9 })
		{
			JduPackageArchiveMetadata metadata = Assert.IsType<JduPackageArchiveMetadata>(packages.GetArchiveMetadata(entry.Md5, "nx"));
			JduPackageArchive archive = Assert.IsType<JduPackageArchive>(packages.GetArchive(entry.Md5, "nx"));
			using Stream stream = archive.Contents;
			Assert.Equal(entry.Md5 + ".zip", archive.FileName);
			Assert.Equal(stream.Length, metadata.Length);
		}
		Assert.Empty(packages.CreateCatalog("invalid"));
		Assert.True(JduMediaClientProfiles.TryResolve("jd2022-nx-all", out JduMediaClientProfile? client));
		Assert.Equal(vp9, packages.SelectPackage("testmap", client)!.Package);
		Assert.Null(packages.SelectPackage("UnknownMap", client));
	}

	[Fact]
	public void PackageSelection_UsesTheCanonicalMapNameInsteadOfTheFolderName()
	{
		JduPackageService packages = CreatePackageService("NX", hasVp8: true, hasVp9: true, "CanonicalMap");
		Assert.True(JduMediaClientProfiles.TryResolve("jd2022-nx-all", out JduMediaClientProfile? client));
		JduPackageEntry catalogEntry = Assert.Single(packages.CreateCatalog("jd2022-nx-all")).Value;
		Assert.Equal(catalogEntry, packages.SelectPackage("CanonicalMap", client)!.Package);
		Assert.Contains("CanonicalMap_mapContent", packages.CreateCatalog("jd2022-nx-all").Keys);
		Assert.Null(packages.SelectPackage("TestMap", client));
	}

	private static JduPackageService CreatePackageService(string platform, bool hasVp8, bool hasVp9, string mapName = "TestMap")
	{
		string mapDirectory = Path.Combine("maps", "TestMap");
		Mock<IFileSystem> fileSystem = new();
		AddArchive("MainScene", new string('a', 32), hasVp8);
		AddArchive("MainSceneVP9", new string('b', 32), hasVp9);
		return new JduPackageService(
			CreateMapService(mapDirectory, mapName),
			Options.Create(new UrlSettings { CDNUrl = "cdn.test" }),
			fileSystem.Object,
			NullLogger<JduPackageService>.Instance);

		void AddArchive(string role, string hash, bool exists)
		{
			string directory = Path.Combine(mapDirectory, "JDUnlimited", "Assets", platform, role);
			string path = Path.Combine(directory, hash + ".zip");
			fileSystem.Setup(system => system.DirectoryExists(directory)).Returns(exists);
			fileSystem.Setup(system => system.GetFiles(directory, "*.zip", SearchOption.TopDirectoryOnly)).Returns([path]);
			fileSystem.Setup(system => system.GetFileLength(path)).Returns(3);
			fileSystem.Setup(system => system.OpenRead(path)).Returns(() => new MemoryStream([1, 2, 3]));
		}
	}

	private static IMapService CreateMapService(string folderPath, string mapName)
	{
		ServerMap map = ServerMapTestData.CreateMap(
			Guid.NewGuid(),
			folderPath,
			new JustDanceSongDBEntry { MapName = mapName, DanceVersionLocId = new OasisTag(new LocalizedString(1, "Standard")) });
		Mock<IMapService> maps = new();
		maps.SetupGet(service => service.Maps).Returns(new OrderedDictionary<Guid, ServerMap> { [map.SongId] = map });
		maps.Setup(service => service.GetMap(mapName)).Returns(map);
		return maps.Object;
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
