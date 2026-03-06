using ChatApp.Api.Data;
using ChatApp.Api.Entities;
using ChatApp.Api.Models.Common;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IDbConnectionFactory _connectionFactory;

    public UsersController(IUserRepository userRepository, IDbConnectionFactory connectionFactory)
    {
        _userRepository = userRepository;
        _connectionFactory = connectionFactory;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<User>>> Get(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 20 : pageSize;

        using var connection = _connectionFactory.CreateConnection();

        var where = new List<string> { "IsActive = 1" };
        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add("(Username LIKE '%' + @Search + '%' OR DisplayName LIKE '%' + @Search + '%')");
        }

        var whereSql = string.Join(" AND ", where);

        var countSql = $"SELECT COUNT(*) FROM Users WHERE {whereSql};";
        var querySql = $"""
SELECT *
FROM Users
WHERE {whereSql}
ORDER BY CreatedAt DESC
OFFSET (@Page - 1) * @PageSize ROWS
FETCH NEXT @PageSize ROWS ONLY;
""";

        var parameters = new { Search = search, Page = page, PageSize = pageSize };

        var total = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var items = (await connection.QueryAsync<User>(querySql, parameters)).ToList();

        var result = new PagedResult<User>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<User>> GetById(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "UPDATE Users SET IsActive = 0 WHERE Id = @Id;";
        await connection.ExecuteAsync(sql, new { Id = id });
        return NoContent();
    }
}

