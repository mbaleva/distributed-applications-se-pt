using System.Security.Claims;
using ChatApp.Api.Data;
using ChatApp.Api.Entities;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ChatHub(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task JoinConversation(int conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GetConversationGroup(conversationId));
    }

    public async Task LeaveConversation(int conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetConversationGroup(conversationId));
    }

    public async Task SendMessage(int conversationId, string content)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            throw new HubException("Unauthorized");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new HubException("Content is required.");
        }

        using var connection = _connectionFactory.CreateConnection();

        const string insertSql = """
INSERT INTO Messages (ConversationId, SenderId, Content, SentAt, IsEdited, IsDeleted)
VALUES (@ConversationId, @SenderId, @Content, @SentAt, 0, 0);
SELECT CAST(SCOPE_IDENTITY() as int);
""";

        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = userId.Value,
            Content = content,
            SentAt = DateTime.UtcNow,
            IsEdited = false,
            IsDeleted = false
        };

        var id = await connection.ExecuteScalarAsync<int>(insertSql, message);
        message.Id = id;

        await Clients.Group(GetConversationGroup(conversationId))
            .SendAsync("ReceiveMessage", new
            {
                message.Id,
                message.ConversationId,
                message.SenderId,
                message.Content,
                message.SentAt,
                message.IsEdited
            });
    }

    private static string GetConversationGroup(int conversationId) => $"conversation-{conversationId}";

    private int? GetUserId()
    {
        var sub = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? Context.User?.FindFirstValue(ClaimTypes.Name);
        return int.TryParse(sub, out var id) ? id : null;
    }
}

