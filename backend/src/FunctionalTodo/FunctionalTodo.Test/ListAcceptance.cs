using DeFuncto;
using Flurl.Http;
using FunctionalTodo.Models;

namespace FunctionalTodo.Test;

using static Prelude;

public class ListAcceptance
{
    [Fact(DisplayName = "Creates a Todo item, but not a duplicate")]
    public async Task Test1()
    {
        await using var server = await TestServer.Create();
        Assert.Empty(await server.Get<List<TodoListItem>>("Todo", None));
        await server.Post("Todo", None, new TodoCreation { Title = "my todo" });
        Assert.Single(await server.Get<List<TodoListItem>>("Todo", None));
        try
        {
            await server.Post("Todo", None, new TodoCreation { Title = "my todo" });
            Assert.Fail("Should not have created a duplicated todo");
        }
        catch (FlurlHttpException ex)
        {
            Assert.Equal(409, ex.StatusCode);
        }
        Assert.Single(await server.Get<List<TodoListItem>>("Todo", None));
    }
}
