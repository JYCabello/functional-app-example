using System.Data.SqlClient;
using Dapper;
using FunctionalTodo.Models;

namespace FunctionalTodo.DomainModel;

public interface IDbAccessFunctions
{
    GetAllFromDb GetAllFromDb { get; }
}

public class DbAccessFunctions : IDbAccessFunctions
{
    private readonly string connectionString;

    public DbAccessFunctions(IConfiguration configuration) =>
        connectionString = configuration.GetConnectionString("SQL")!;

    public GetAllFromDb GetAllFromDb =>
        async () =>
        {
            await using var db = new SqlConnection(connectionString);
            return await db.QueryAsync<TodoListItem>(
                "SELECT ID, Title, IsCompleted FROM Todo"
            );
        };
}
