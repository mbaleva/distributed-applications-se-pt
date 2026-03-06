namespace ChatApp.Api.Models.Conversations;

public class ConversationCreateRequest
{
    public string Title { get; set; } = null!;
    public bool IsGroup { get; set; }
    public List<int> ParticipantUserIds { get; set; } = new();
}

