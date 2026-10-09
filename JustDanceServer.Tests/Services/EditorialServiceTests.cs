using JustDanceServer.Configuration;
using JustDanceServer.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using System.Text;

using Moq;

namespace JustDanceServer.Tests.Services;

public sealed class EditorialServiceTests
{
	[Fact]
	public async Task LoadsMultipleMessagesAndFiltersByProductYearPlatformAndTime()
	{
		const string path = "editorial";
		const string file = "editorial/messages.json";
		Mock<IFileSystem> fileSystem = new();
		fileSystem.Setup(value => value.DirectoryExists(path)).Returns(true);
		fileSystem.Setup(value => value.GetFiles(path, "*.json", SearchOption.TopDirectoryOnly))
			.Returns([file]);
		fileSystem.Setup(value => value.OpenRead(file)).Returns(() => new MemoryStream(Encoding.UTF8.GetBytes(
			"""
			[
			  {
			    "id": "11111111-1111-1111-1111-111111111111",
			    "title": "All JDU",
			    "body": "Body",
			    "publicationDate": "2026-01-01T00:00:00Z",
			    "products": [ "JDUnlimited" ]
			  },
			  {
			    "id": "22222222-2222-2222-2222-222222222222",
			    "title": "2018 Switch",
			    "body": "Body",
			    "publicationDate": "2026-01-01T00:00:00Z",
			    "products": [ "JDUnlimited" ],
			    "gameYears": [ 2018 ],
			    "platforms": [ "switch" ]
			  },
			  {
			    "id": "33333333-3333-3333-3333-333333333333",
			    "title": "JD+",
			    "body": "Body",
			    "publicationDate": "2026-01-01T00:00:00Z",
			    "products": [ "JDPlus" ]
			  },
			  {
			    "id": "44444444-4444-4444-4444-444444444444",
			    "title": "Expired",
			    "body": "Body",
			    "publicationDate": "2026-01-01T00:00:00Z",
			    "expirationDate": "2026-02-01T00:00:00Z"
			  }
			]
			""")));
		TestTimeProvider clock = new(new DateTimeOffset(2026, 8, 15, 12, 0, 0, TimeSpan.Zero));
		EditorialService service = new(
			Options.Create(new PathSettings { EditorialPath = path }),
			new JsonSettingsService(Mock.Of<IServiceProvider>()),
			fileSystem.Object,
			clock,
			NullLogger<EditorialService>.Instance,
			Mock.Of<ILocalizedStringService>());

		await service.LoadData();

		Assert.Equal(
			["All JDU", "2018 Switch"],
			service.GetEntries(new EditorialAudience(EditorialProduct.JDUnlimited, 2018, "switch"))
				.Select(value => value.Title));
		Assert.Equal(
			["All JDU"],
			service.GetEntries(new EditorialAudience(EditorialProduct.JDUnlimited, 2017, "wiiu"))
				.Select(value => value.Title));
		Assert.Equal(
			["JD+"],
			service.GetEntries(new EditorialAudience(EditorialProduct.JDPlus, Platform: "switch"))
				.Select(value => value.Title));
	}

	private sealed class TestTimeProvider(DateTimeOffset utcNow) : TimeProvider
	{
		public override DateTimeOffset GetUtcNow() => utcNow;
	}
}
