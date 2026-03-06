using System.Security.Claims;
using ChatApp.Api.Data;
using ChatApp.Api.Entities;
using ChatApp.Api.Models.Common;
using ChatApp.Api.Models.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Api.Controllers;

[ApiController]
[Route("api/conversations/{conversationId:int}/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMessageRepository _messageRepository;

    public MessagesController(IMessageRepository messageRepository)
    {
        _messageRepository = messageRepository;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<Message>>> Get(
        int conversationId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 50 : pageSize;

        var result = await _messageRepository.GetForConversationAsync(conversationId, search, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Message>> GetById(int conversationId, int id)
    {
        var message = await _messageRepository.GetByIdAsync(id);
        if (message == null || message.ConversationId != conversationId)
        {
            return NotFound();
        }

        return Ok(message);
    }

    [HttpPost]
    public async Task<ActionResult<Message>> Create(int conversationId, MessageCreateRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest("Content is required.");
        }

        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = userId.Value,
            Content = request.Content,
            SentAt = DateTime.UtcNow,
            IsEdited = false,
            IsDeleted = false
        };

        var id = await _messageRepository.CreateAsync(message);
        message.Id = id;

        return CreatedAtAction(nameof(GetById), new { conversationId, id = message.Id }, message);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int conversationId, int id, MessageUpdateRequest request)
    {
        var message = await _messageRepository.GetByIdAsync(id);
        if (message == null || message.ConversationId != conversationId)
        {
            return NotFound();
        }

        message.Content = request.Content;
        message.IsEdited = true;

        await _messageRepository.UpdateAsync(message);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int conversationId, int id)
    {
        var message = await _messageRepository.GetByIdAsync(id);
        if (message == null || message.ConversationId != conversationId)
        {
            return NotFound();
        }

        await _messageRepository.DeleteAsync(id);
        return NoContent();
    }

    private int? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name);
        return int.TryParse(sub, out var id) ? id : null;
    }
}

