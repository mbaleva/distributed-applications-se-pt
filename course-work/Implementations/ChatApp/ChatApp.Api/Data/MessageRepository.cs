using ChatApp.Api.Entities;
using ChatApp.Api.Models.Common;
using Dapper;

namespace ChatApp.Api.Data;

public interface IMessageRepository
{
    Task<PagedResult<Message>> GetForConversationAsync(int conversationId, string? search, int page, int pageSize);
    Task<Message?> GetByIdAsync(int id);
    Task<int> CreateAsync(Message message);
    Task UpdateAsync(Message message);
    Task DeleteAsync(int id);
}

public class MessageRepository : IMessageRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MessageRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PagedResult<Message>> GetForConversationAsync(int conversationId, string? search, int page, int pageSize)
    {
        using var connection = _connectionFactory.CreateConnection();

        var where = new List<string>
        {
            "m.ConversationId = @ConversationId",
            "m.IsDeleted = 0"
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add("m.Content LIKE '%' + @Search + '%'");
        }

        var whereSql = string.Join(" AND ", where);

        var countSql = $"""
SELECT COUNT(*)
FROM Messages m
WHERE {whereSql};
""";

        var querySql = $"""
SELECT m.*
FROM Messages m
WHERE {whereSql}
ORDER BY m.SentAt DESC
OFFSET (@Page - 1) * @PageSize ROWS
FETCH NEXT @PageSize ROWS ONLY;
""";

        var parameters = new
        {
            ConversationId = conversationId,
            Search = search,
            Page = page,
            PageSize = pageSize
        };

        var total = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var items = (await connection.QueryAsync<Message>(querySql, parameters)).ToList();

        return new PagedResult<Message>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Message?> GetByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT * FROM Messages WHERE Id = @Id;";
        return await connection.QuerySingleOrDefaultAsync<Message>(sql, new { Id = id });
    }

    public async Task<int> CreateAsync(Message message)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = """
INSERT INTO Messages (ConversationId, SenderId, Content, SentAt, IsEdited, IsDeleted)
VALUES (@ConversationId, @SenderId, @Content, @SentAt, @IsEdited, @IsDeleted);
SELECT CAST(SCOPE_IDENTITY() as int);
""";
        return await connection.ExecuteScalarAsync<int>(sql, message);
    }

    public async Task UpdateAsync(Message message)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = """
UPDATE Messages
SET Content = @Content,
    IsEdited = @IsEdited
WHERE Id = @Id;
""";
        await connection.ExecuteAsync(sql, message);
    }

    public async Task DeleteAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "UPDATE Messages SET IsDeleted = 1 WHERE Id = @Id;";
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}

