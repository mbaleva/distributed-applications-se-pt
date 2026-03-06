using System.Security.Claims;
using ChatApp.Api.Data;
using ChatApp.Api.Entities;
using ChatApp.Api.Models.Common;
using ChatApp.Api.Models.Conversations;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConversationsController : ControllerBase
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IDbConnectionFactory _connectionFactory;

    public ConversationsController(IConversationRepository conversationRepository, IDbConnectionFactory connectionFactory)
    {
        _conversationRepository = conversationRepository;
        _connectionFactory = connectionFactory;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<Conversation>>> GetMyConversations(
        [FromQuery] string? title,
        [FromQuery] bool? isGroup,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 20 : pageSize;

        var result = await _conversationRepository.GetForUserAsync(userId.Value, title, isGroup, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Conversation>> GetById(int id)
    {
        var conversation = await _conversationRepository.GetByIdAsync(id);
        if (conversation == null)
        {
            return NotFound();
        }

        return Ok(conversation);
    }

    [HttpPost]
    public async Task<ActionResult<Conversation>> Create(ConversationCreateRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest("Title is required.");
        }

        var conversation = new Conversation
        {
            Title = request.Title.Trim(),
            IsGroup = request.IsGroup,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId.Value,
            IsArchived = false
        };

        var id = await _conversationRepository.CreateAsync(conversation);
        conversation.Id = id;

        // add participants (creator + provided list)
        using var connection = _connectionFactory.CreateConnection();

        var participantUserIds = new HashSet<int>(request.ParticipantUserIds ?? new List<int>())
        {
            userId.Value
        };

        const string insertParticipantSql = """
INSERT INTO ConversationParticipants (ConversationId, UserId, JoinedAt, Role, IsMuted)
VALUES (@ConversationId, @UserId, @JoinedAt, @Role, 0);
""";

        var now = DateTime.UtcNow;
        foreach (var pid in participantUserIds)
        {
            await connection.ExecuteAsync(insertParticipantSql, new
            {
                ConversationId = conversation.Id,
                UserId = pid,
                JoinedAt = now,
                Role = pid == userId.Value ? "Owner" : "Member"
            });
        }

        return CreatedAtAction(nameof(GetById), new { id = conversation.Id }, conversation);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ConversationUpdateRequest request)
    {
        var existing = await _conversationRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.Title = request.Title;
        existing.IsGroup = request.IsGroup;
        existing.IsArchived = request.IsArchived;

        await _conversationRepository.UpdateAsync(existing);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _conversationRepository.DeleteAsync(id);
        return NoContent();
    }

    private int? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name);
        return int.TryParse(sub, out var id) ? id : null;
    }
}

