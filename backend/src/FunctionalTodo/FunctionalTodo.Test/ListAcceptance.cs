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
        Assert.Empty(await server.Get<List<TodoListItem>>("todo/list", None));
        await server.Post("todo/create", None, new TodoCreation { Title = "my todo" });
        Assert.Single(await server.Get<List<TodoListItem>>("todo/list", None));
        try
        {
            await server.Post("todo/create", None, new TodoCreation { Title = "my todo" });
            Assert.Fail("Should not have created a duplicated todo");
        }
        catch (FlurlHttpException ex)
        {
            Assert.Equal(409, ex.StatusCode);
        }

        Assert.Single(await server.Get<List<TodoListItem>>("Todo", None));
    }

    [Fact(DisplayName = "Gets a todo by its id")]
    public async Task Test2()
    {
        await using var server = await TestServer.Create();
        Assert.Empty(await server.Get<List<TodoListItem>>("todo/list", None));
        await server.Post("todo/create", None, new TodoCreation { Title = "my todo" });
        Assert.Single(await server.Get<List<TodoListItem>>("todo/list", None));
        try
        {
            var todo = await server.Get<TodoListItem>("todo/id/1", None);
            Assert.Equal(1, todo.ID);
        }
        catch (FlurlHttpException ex)
        {
            Assert.Fail("Couldn't find a todo by that id");
        }

        Assert.Single(await server.Get<List<TodoListItem>>("Todo", None));
    }

    [Fact(DisplayName = "Gets a non found error when getting a todo by an non-existing id", Skip = "Todo")]
    public async Task Test3()
    {
        await using var server = await TestServer.Create();
        Assert.Empty(await server.Get<List<TodoListItem>>("todo/list", None));
        try
        {
            await server.Get<TodoListItem>("todo/id/3", None);
            Assert.Fail("Should not have found a todo by that id");
        }
        catch (FlurlHttpException ex)
        {
            Assert.Equal(404, ex.StatusCode);
        }
    }
}