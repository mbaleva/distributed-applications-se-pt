namespace ChatApp.Api.Entities;

public class Message
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string Content { get; set; } = null!;
    public DateTime SentAt { get; set; }
    public bool IsEdited { get; set; }
    public bool IsDeleted { get; set; }
}

