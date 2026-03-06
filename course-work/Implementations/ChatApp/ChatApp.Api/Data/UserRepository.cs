using ChatApp.Api.Entities;
using Dapper;

namespace ChatApp.Api.Data;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByUsernameAsync(string username);
    Task<int> CreateAsync(User user);
}

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT * FROM [Users] WHERE [Id] = @Id AND [IsActive] = 1;";
        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Id = id });
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT * FROM [Users] WHERE [Username] = @Username AND [IsActive] = 1;";
        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Username = username });
    }

    public async Task<int> CreateAsync(User user)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = """
INSERT INTO [Users] (Username, PasswordHash, Email, DisplayName, CreatedAt, IsActive)
VALUES (@Username, @PasswordHash, @Email, @DisplayName, @CreatedAt, @IsActive);
SELECT CAST(SCOPE_IDENTITY() as int);
""";
        var id = await connection.ExecuteScalarAsync<int>(sql, user);
        return id;
    }
}

