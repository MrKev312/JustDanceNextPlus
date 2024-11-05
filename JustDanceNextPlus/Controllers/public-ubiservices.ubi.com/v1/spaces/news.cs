using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v1.spaces;

[ApiController]
[Route("v1/spaces/news")]
public class News : Controller
{
	[HttpGet(Name = "GetNews")]
	public IActionResult GetNews()
	{
		string response = """
			{
				"news": [{
					"spaceId": "1da01a17-3bc7-4b5d-aedd-70a0915089b0",
					"newsId": "ignt.900002",
					"type": "normal",
					"placement": "WhatsNew Popup",
					"priority": 1,
					"displayTime": 0,
					"publicationDate": "2024-04-09T09:00:00",
					"expirationDate": null,
					"locale": "en-US",
					"title": "Welcome to the Just Dance Custom Server!",
					"contentType": "plaintext",
					"body": "Connected to Just Dance Custom Server!\n\nEnjoy!\n\t- MrKev312",
					"mediaURL": "",
					"mediaType": "None",
					"profileId": null,
					"obj": {
						"promotionalContent": true
					},
					"summary": "",
					"links": [],
					"tags": []
				}, {
					"spaceId": "1da01a17-3bc7-4b5d-aedd-70a0915089b0",
					"newsId": "ignt.900001",
					"type": "Maintenance",
					"placement": "Notification center",
					"priority": 1,
					"displayTime": 0,
					"publicationDate": "2024-03-31T09:00:00",
					"expirationDate": null,
					"locale": "en-US",
					"title": "Just Dance Custom Server is now available!",
					"contentType": "plaintext",
					"body": "The Just Dance Custom Server is now available for everyone to use! You can now play with your friends and family on the Just Dance Custom Server. Enjoy!",
					"mediaURL": "",
					"mediaType": "None",
					"profileId": null,
					"obj": {
						"promotionalContent": true
					},
					"summary": "",
					"links": [],
					"tags": []
				}, {
					"spaceId": "1da01a17-3bc7-4b5d-aedd-70a0915089b0",
					"newsId": "ignt.34464",
					"type": "patch_note",
					"placement": "Notification center",
					"priority": 2,
					"displayTime": 0,
					"publicationDate": "2022-11-21T07:30:00",
					"expirationDate": null,
					"locale": "en-US",
					"title": "Welcome to the party!",
					"contentType": "plaintext",
					"body": "Welcome to the party!\n\nWe are happy to announce that the Just Dance Custom Server is now available for everyone to use! You can now play on a Just Dance Custom Server.\n\nEnjoy!\n\t- MrKev312",
					"mediaURL": "",
					"mediaType": "None",
					"profileId": null,
					"obj": {
						"promotionalContent": false
					},
					"summary": "",
					"links": [],
					"tags": []
				}]
			}
			""";

		return Content(response, "application/json");
	}
}
