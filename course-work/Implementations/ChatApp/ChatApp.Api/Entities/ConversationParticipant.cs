namespace ChatApp.Api.Entities;

public class ConversationParticipant
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public int UserId { get; set; }
    public DateTime JoinedAt { get; set; }
    public string Role { get; set; } = null!;
    public bool IsMuted { get; set; }
}

