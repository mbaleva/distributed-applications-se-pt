namespace ChatApp.Api.Entities;

public class Conversation
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public bool IsGroup { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserId { get; set; }
    public bool IsArchived { get; set; }
}

