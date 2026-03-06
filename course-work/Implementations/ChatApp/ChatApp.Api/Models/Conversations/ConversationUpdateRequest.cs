namespace ChatApp.Api.Models.Conversations;

public class ConversationUpdateRequest
{
    public string Title { get; set; } = null!;
    public bool IsGroup { get; set; }
    public bool IsArchived { get; set; }
}

