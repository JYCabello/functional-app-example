using DeFuncto;
using FunctionalTodo.Models;

namespace FunctionalTodo.Test;

using static Prelude;

public class ListAcceptance
{
    [Fact]
    public async Task Test1()
    {
        await using var server = await TestServer.Create();
        var items = await server.Get<List<TodoListItem>>("Todo", None);
        Assert.Empty(items);
    }
}
