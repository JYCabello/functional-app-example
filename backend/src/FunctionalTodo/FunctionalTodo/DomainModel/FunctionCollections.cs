using System.Data;
using System.Data.SqlClient;
using Dapper;
using FunctionalTodo.Models;

namespace FunctionalTodo.DomainModel;

public interface IDbAccessFunctions
{
    CreateTodo CreateTodo { get; }
    GetAllFromDb GetAllFromDb { get; }
    GetById GetTodoById { get; }
    MarkTodoAsCompleted MarkAsCompleted { get; }
}

public interface ISettingsFunctions
{
    GetConnectionString GetConnectionString { get; }
}

public class DbAccessFunctions : IDbAccessFunctions
{
    private readonly ISettingsFunctions settingsFunctions;

    public DbAccessFunctions(ISettingsFunctions settingsFunctions) =>
        this.settingsFunctions = settingsFunctions;

    public GetAllFromDb GetAllFromDb => BuildGetAllFromDb(settingsFunctions.GetConnectionString);
    public CreateTodo CreateTodo => BuildExecuteQuery(settingsFunctions.GetConnectionString);
    public GetById GetTodoById => BuildGetTodoByIdQuery(settingsFunctions.GetConnectionString);
    public MarkTodoAsCompleted MarkAsCompleted => BuildMarkTodoAsCompletedQuery(settingsFunctions.GetConnectionString);

    public GetAllFromDb BuildGetAllFromDb(GetConnectionString getConnectionString) =>
        async () =>
        {
            await using var db = new SqlConnection(getConnectionString());
            return await db.QueryAsync<TodoListItem>(
                "SELECT ID, Title, IsCompleted FROM Todo"
            );
        };

    public CreateTodo BuildExecuteQuery(GetConnectionString getConnectionString) =>
        async p =>
        {
            await using var db = new SqlConnection(getConnectionString());
            var isThereDuplicatedRecord = await db.QuerySingleAsync<bool>(
                "SELECT CAST(COUNT(*) AS BIT) FROM Todo WHERE Title = @title", p
            );
            if (isThereDuplicatedRecord)
            {
                return false;
            }

            await db.ExecuteAsync("INSERT INTO Todo (Title, IsCompleted) VALUES (@title, 0)", p);
            return true;
        };

    public GetById BuildGetTodoByIdQuery(GetConnectionString getConnectionString) =>
        async id =>
        {
            await using var db = new SqlConnection(getConnectionString());
            var parameters = new DynamicParameters();
            parameters.Add("@ID", id, DbType.String, ParameterDirection.Input);
            TodoListItem todo;
            try
            {
                todo = await db.QuerySingleAsync<TodoListItem>(
                    "SELECT ID, Title, IsCompleted FROM Todo WHERE ID = @ID", parameters);
            }
            catch (Exception ex)
            {
                return null;
            }

            return todo;
        };

    public MarkTodoAsCompleted BuildMarkTodoAsCompletedQuery(GetConnectionString getConnectionString) =>
        async todo =>
        {
            await using var db = new SqlConnection(getConnectionString());
            var parameters = new DynamicParameters();
            parameters.Add("@ID", todo.ID, DbType.String, ParameterDirection.Input);

            await db.ExecuteAsync(
                "UPDATE Todo SET IsCompleted = 'true' WHERE ID = @ID", parameters);

            return todo.ID;
        };
}

public class SettingsFunctions : ISettingsFunctions
{
    private readonly IConfiguration configuration;

    public SettingsFunctions(IConfiguration configuration) =>
        this.configuration = configuration;

    public GetConnectionString GetConnectionString =>
        () => configuration.GetConnectionString("SQL")!;
}