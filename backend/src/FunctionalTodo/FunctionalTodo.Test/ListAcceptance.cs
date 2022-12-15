using DeFuncto;
using FunctionalTodo.Models;

namespace FunctionalTodo.Test;

using static Prelude;

public class ListAcceptance
{
    [Fact]
    public async Task Test1()
    {
        using var server = new TestServer();
        var items = await server.Get<List<TodoListItem>>("Todo", None);
        Assert.Empty(items);
    }
}
