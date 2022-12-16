using System.Data.SqlClient;
using Dapper;
using FunctionalTodo.Models;

namespace FunctionalTodo.DomainModel;

public interface IDbAccessFunctions
{
    GetAllFromDb GetAllFromDb { get; }
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

    public GetAllFromDb BuildGetAllFromDb(GetConnectionString getConnectionString) =>
        async () =>
        {
            await using var db = new SqlConnection(getConnectionString());
            return await db.QueryAsync<TodoListItem>(
                "SELECT ID, Title, IsCompleted FROM Todo"
            );
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
