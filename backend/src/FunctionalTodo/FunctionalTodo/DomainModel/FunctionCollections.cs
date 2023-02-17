using System.Data;
using System.Data.SqlClient;
using Dapper;
using DeFuncto;
using FunctionalTodo.Models;

namespace FunctionalTodo.DomainModel;

public interface IDbAccessFunctions
{
    CreateTodo CreateTodo { get; }
    GetAllFromDb GetAllFromDb { get; }
    GetAllIncompleteFromDb GetAllIncompleteFromDb { get; }
    GetAllCompleteFromDb GetAllCompleteFromDb { get; }
    GetById GetTodoById { get; }
    MarkTodoAsComplete MarkAsComplete { get; }
    MarkTodoAsIncomplete MarkTodoAsIncomplete { get; }
    CheckIfCompleted CheckIfCompleted { get; }
    FindByTitle FindByTitle { get; }
    FindById FindById { get; }
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

    public GetAllIncompleteFromDb GetAllIncompleteFromDb =>
        BuildGetAllIncompleteFromDb(settingsFunctions.GetConnectionString);

    public GetAllCompleteFromDb GetAllCompleteFromDb =>
        BuildGetAllCompleteFromDb(settingsFunctions.GetConnectionString);

    public CreateTodo CreateTodo => BuildExecuteQuery(settingsFunctions.GetConnectionString);
    public GetById GetTodoById => BuildGetTodoByIdQuery(settingsFunctions.GetConnectionString);
    public MarkTodoAsComplete MarkAsComplete => BuildMarkTodoAsCompleteQuery(settingsFunctions.GetConnectionString);

    public MarkTodoAsIncomplete MarkTodoAsIncomplete =>
        BuildMarkTodoAsIncompleteQuery(settingsFunctions.GetConnectionString);

    public CheckIfCompleted CheckIfCompleted => BuildCheckIfCompleted(settingsFunctions.GetConnectionString);
    public FindById FindById => BuildFindByIdQuery(settingsFunctions.GetConnectionString);
    public FindByTitle FindByTitle => BuildFindByTitleQuery(settingsFunctions.GetConnectionString);

    public GetAllFromDb BuildGetAllFromDb(GetConnectionString getConnectionString) =>
        async () =>
        {
            await using var db = new SqlConnection(getConnectionString());
            return await db.QueryAsync<TodoListItem>(
                "SELECT ID, Title, IsCompleted FROM Todo"
            );
        };

    public GetAllIncompleteFromDb BuildGetAllIncompleteFromDb(GetConnectionString getConnectionString) =>
        async () =>
        {
            await using var db = new SqlConnection(getConnectionString());
            return await db.QueryAsync<TodoListItem>(
                "SELECT ID, Title, IsCompleted FROM Todo WHERE IsCompleted='false'"
            );
        };

    public GetAllCompleteFromDb BuildGetAllCompleteFromDb(GetConnectionString getConnectionString) =>
        async () =>
        {
            await using var db = new SqlConnection(getConnectionString());
            return await db.QueryAsync<TodoListItem>(
                "SELECT ID, Title, IsCompleted FROM Todo WHERE IsCompleted='true'"
            );
        };

    public CreateTodo BuildExecuteQuery(GetConnectionString getConnectionString) =>
        async t =>
        {
            await using var db = new SqlConnection(getConnectionString());
            return await db.ExecuteAsync(
                "INSERT INTO Todo (Title, IsCompleted) VALUES (@Title, 0); SELECT SCOPE_IDENTITY();",
                t);
        };

    public FindByTitle BuildFindByTitleQuery(GetConnectionString getConnectionString) =>
        t =>
        {
            async Task<Option<TodoListItem>> Go()
            {
                await using var db = new SqlConnection(getConnectionString());
                var todo = await db
                    .QueryFirstOrDefaultAsync<TodoListItem>(
                        "SELECT ID, Title, IsCompleted FROM Todo WHERE Title=@Title",
                        new { Title = t });
                return Optional(todo);
            }

            return Go();
        };

    public FindById BuildFindByIdQuery(GetConnectionString getConnectionString) =>
        id =>
        {
            async Task<Option<TodoListItem>> Go()
            {
                await using var db = new SqlConnection(getConnectionString());
                var todo = await db
                    .QueryFirstOrDefaultAsync<TodoListItem>(
                        "SELECT ID, Title, IsCompleted FROM Todo WHERE ID = @ID",
                        new { ID = id });
                return Optional(todo);
            }

            return Go();
        };

    // No deberia ser exactamente igual al de arriba (y por tanto inutil)? Que utilidad tiene un Get comparado con un Find
    // si usamos Optional en vez de try/catch? O hay situaciones en las que usar try/catch?
    public GetById BuildGetTodoByIdQuery(GetConnectionString getConnectionString) =>
        id =>
        {
            async Task<Option<TodoListItem>> Go()
            {
                await using var db = new SqlConnection(getConnectionString());
                TodoListItem todo = null;
                try
                {
                    todo = await db.QuerySingleAsync<TodoListItem>(
                        "SELECT ID, Title, IsCompleted FROM Todo WHERE ID = @ID", new { ID = id });
                }
                catch (Exception)
                {
                    // No se que hacer aquí ayuda
                }

                return Optional(todo);
            }

            return Go();
        };

    public MarkTodoAsComplete BuildMarkTodoAsCompleteQuery(GetConnectionString getConnectionString) =>
        async id =>
        {
            await using var db = new SqlConnection(getConnectionString());

            await db.ExecuteAsync(
                "UPDATE Todo SET IsCompleted = 'true' WHERE ID = @ID", new { ID = id });

            return unit;
        };

    public MarkTodoAsIncomplete BuildMarkTodoAsIncompleteQuery(GetConnectionString getConnectionString) =>
        async id =>
        {
            await using var db = new SqlConnection(getConnectionString());

            await db.ExecuteAsync(
                "UPDATE Todo SET IsCompleted = 'false' WHERE ID = @ID", new { ID = id });

            return id;
        };

    public CheckIfCompleted BuildCheckIfCompleted(GetConnectionString getConnectionString) =>
        id =>
        {
            async Task<Option<Unit>> Go()
            {
                await using var db = new SqlConnection(getConnectionString());
                var result = await db.QuerySingleAsync<bool>(
                    "SELECT CAST(IsCompleted AS BIT) FROM Todo WHERE ID = @ID", new { ID = id });

                return result ? unit : None;
            }

            return Go();
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