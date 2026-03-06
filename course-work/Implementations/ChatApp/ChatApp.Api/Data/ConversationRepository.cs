using ChatApp.Api.Entities;
using ChatApp.Api.Models.Common;
using Dapper;

namespace ChatApp.Api.Data;

public interface IConversationRepository
{
    Task<PagedResult<Conversation>> GetForUserAsync(int userId, string? title, bool? isGroup, int page, int pageSize);
    Task<Conversation?> GetByIdAsync(int id);
    Task<int> CreateAsync(Conversation conversation);
    Task UpdateAsync(Conversation conversation);
    Task DeleteAsync(int id);
}

public class ConversationRepository : IConversationRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ConversationRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PagedResult<Conversation>> GetForUserAsync(int userId, string? title, bool? isGroup, int page, int pageSize)
    {
        using var connection = _connectionFactory.CreateConnection();

        var where = new List<string>
        {
            "c.IsArchived = 0",
            "cp.UserId = @UserId"
        };

        if (!string.IsNullOrWhiteSpace(title))
        {
            where.Add("c.Title LIKE '%' + @Title + '%'");
        }

        if (isGroup.HasValue)
        {
            where.Add("c.IsGroup = @IsGroup");
        }

        var whereSql = string.Join(" AND ", where);

        var countSql = $"""
SELECT COUNT(*)
FROM Conversations c
JOIN ConversationParticipants cp ON cp.ConversationId = c.Id
WHERE {whereSql};
""";

        var querySql = $"""
SELECT c.*
FROM Conversations c
JOIN ConversationParticipants cp ON cp.ConversationId = c.Id
WHERE {whereSql}
ORDER BY c.CreatedAt DESC
OFFSET (@Page - 1) * @PageSize ROWS
FETCH NEXT @PageSize ROWS ONLY;
""";

        var parameters = new
        {
            UserId = userId,
            Title = title,
            IsGroup = isGroup,
            Page = page,
            PageSize = pageSize
        };

        var total = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var items = (await connection.QueryAsync<Conversation>(querySql, parameters)).ToList();

        return new PagedResult<Conversation>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Conversation?> GetByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT * FROM Conversations WHERE Id = @Id;";
        return await connection.QuerySingleOrDefaultAsync<Conversation>(sql, new { Id = id });
    }

    public async Task<int> CreateAsync(Conversation conversation)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = """
INSERT INTO Conversations (Title, IsGroup, CreatedAt, CreatedByUserId, IsArchived)
VALUES (@Title, @IsGroup, @CreatedAt, @CreatedByUserId, @IsArchived);
SELECT CAST(SCOPE_IDENTITY() as int);
""";
        return await connection.ExecuteScalarAsync<int>(sql, conversation);
    }

    public async Task UpdateAsync(Conversation conversation)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = """
UPDATE Conversations
SET Title = @Title,
    IsGroup = @IsGroup,
    IsArchived = @IsArchived
WHERE Id = @Id;
""";
        await connection.ExecuteAsync(sql, conversation);
    }

    public async Task DeleteAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "UPDATE Conversations SET IsArchived = 1 WHERE Id = @Id;";
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}

