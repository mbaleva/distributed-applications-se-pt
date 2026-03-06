using System.Data;
using Microsoft.Data.SqlClient;


namespace ChatApp.Api.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetValue<String>("ConnectionString");
    }

    public IDbConnection CreateConnection()
    {
        var conn = new SqlConnection(_connectionString);

        return conn;
    }
}

