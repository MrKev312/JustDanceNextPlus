using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v2;

[ApiController]
[Route("v2/websocket")]
public sealed class Websocket(
    ISessionManager sessionManager,
    IUbiServicesNotificationHub notifications) : ControllerBase
{
    [HttpGet]
    public async Task GetWebsocket(CancellationToken cancellationToken)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        if (!RequestSessionResolver.TryResolve(Request, sessionManager, out Session? session))
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        using WebSocket socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        using UbiServicesNotificationSubscription subscription = notifications.Subscribe(
            session.PlayerId,
            ParseNotificationTypes(Request.Query["NotificationTypes"].ToString()),
            ParseSpaceIds(Request.Query["SpaceIds"].ToString()));

        Guid connectionId = Guid.NewGuid();
        await SendAsync(socket, new UbiServicesNotification(
            "US_CONNECTION_ESTABLISHED",
            new JsonObject
            {
                ["connectionId"] = connectionId,
                ["contactUrl"] = GetWebSocketUrl(Request)
            },
            DateTime.UtcNow), cancellationToken);

        using CancellationTokenSource connectionCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task sender = SendNotificationsAsync(
            socket,
            subscription.Notifications,
            connectionCancellation.Token);
        Task receiver = ReceiveUntilClosedAsync(socket, connectionCancellation.Token);
        await Task.WhenAny(sender, receiver);
        await connectionCancellation.CancelAsync();
        await IgnoreConnectionTerminationAsync(sender, receiver);
    }

    private static IEnumerable<string> ParseNotificationTypes(string value)
        => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static IEnumerable<Guid> ParseSpaceIds(string value)
        => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => Guid.TryParse(value, out Guid id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty);

    private static string GetWebSocketUrl(HttpRequest request)
        => $"{(request.IsHttps ? "wss" : "ws")}://{request.Host}{request.PathBase}{request.Path}{request.QueryString}";

    private static async Task SendNotificationsAsync(
        WebSocket socket,
        System.Threading.Channels.ChannelReader<UbiServicesNotification> notifications,
        CancellationToken cancellationToken)
    {
        await foreach (UbiServicesNotification notification in
            notifications.ReadAllAsync(cancellationToken))
        {
            await SendAsync(socket, notification, cancellationToken);
        }
    }

    private static Task SendAsync(
        WebSocket socket,
        UbiServicesNotification notification,
        CancellationToken cancellationToken)
    {
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            notification.NotificationType,
            notification.Content,
            notification.DatePosted
        }, JsonSerializerOptions.Web);
        return socket.SendAsync(
            payload,
            WebSocketMessageType.Text,
            true,
            cancellationToken);
    }

    private static async Task ReceiveUntilClosedAsync(
        WebSocket socket,
        CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[1024];
        while (socket.State == WebSocketState.Open)
        {
            ValueWebSocketReceiveResult result = await socket.ReceiveAsync(
                buffer.AsMemory(),
                cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseOutputAsync(
                    WebSocketCloseStatus.NormalClosure,
                    string.Empty,
                    CancellationToken.None);
                return;
            }
        }
    }

    private static async Task IgnoreConnectionTerminationAsync(params Task[] tasks)
    {
        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception exception) when (exception is
            OperationCanceledException or WebSocketException)
        {
        }
    }
}
