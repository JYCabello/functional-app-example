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
        var id = await server.Post<int>("todo/create", None, new TodoCreation { Title = "my todo" });
        var list = await server.Get<List<TodoListItem>>("todo/list", None);
        Assert.Collection(list, todo => Assert.Equal(todo.ID, id));
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

        var todo = await server.Get<TodoListItem>("todo/id/1", None);
        Assert.Equal(1, todo.ID);
        Assert.Single(await server.Get<List<TodoListItem>>("Todo", None));
    }

    [Fact(DisplayName = "Gets a non found error when getting a todo by an non-existing id")]
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

    [Fact(DisplayName = "Marks a todo item as completed but not when it was already completed")]
    public async Task Test4()
    {
        await using var server = await TestServer.Create();
        Assert.Empty(await server.Get<List<TodoListItem>>("todo/list", None));

        var todoBody = new TodoCreation { Title = "my todo" };
        var todoId = await server.Post<int>("todo/create", None, todoBody);

        var todoList = await server.Get<List<TodoListItem>>("todo/list", None);
        Assert.Single(todoList);
        Assert.False(todoList[0].IsCompleted);

        await server.Put($"todo/completed/{todoId}", None, None);
        var todoListAfterMarkingComplete = await server.Get<List<TodoListItem>>("todo/list", None);
        Assert.Single(todoListAfterMarkingComplete);
        Assert.True(todoListAfterMarkingComplete[0].IsCompleted);

        try
        {
            await server.Put($"todo/completed/{todoId}", None, None);
            Assert.Fail("Should not have updated completed todo");
        }
        catch (FlurlHttpException ex)
        {
            Assert.Equal(409, ex.StatusCode);
        }
    }

    [Fact(DisplayName = "Can't mark a todo item as completed or incomplete if it doesn't exist")]
    public async Task Test5()
    {
        await using var server = await TestServer.Create();
        Assert.Empty(await server.Get<List<TodoListItem>>("todo/list", None));

        try
        {
            await server.Put("todo/completed/3", None, None);
            Assert.Fail("Should not have found todo");
        }
        catch (FlurlHttpException ex)
        {
            Assert.Equal(404, ex.StatusCode);
        }

        try
        {
            await server.Put("todo/incomplete/3", None, None);
            Assert.Fail("Should not have found todo");
        }
        catch (FlurlHttpException ex)
        {
            Assert.Equal(404, ex.StatusCode);
        }
    }

    [Fact(DisplayName = "Marks a todo item as incomplete but not when it was already incomplete")]
    public async Task Test6()
    {
        await using var server = await TestServer.Create();
        Assert.Empty(await server.Get<List<TodoListItem>>("todo/list", None));

        var todoBody = new TodoCreation { Title = "my todo" };
        var todoId = await server.Post<int>("todo/create", None, todoBody);
        var todoList = await server.Get<List<TodoListItem>>("todo/list", None);
        Assert.False(todoList[0].IsCompleted);

        try
        {
            await server.Put($"todo/incomplete/{todoId}", None, None);
            Assert.Fail("Should not have updated incomplete todo");
        }
        catch (FlurlHttpException ex)
        {
            Assert.Equal(409, ex.StatusCode);
        }

        await server.Put($"todo/completed/{todoId}", None, None);
        todoList = await server.Get<List<TodoListItem>>("todo/list", None);
        Assert.Single(todoList);
        Assert.True(todoList[0].IsCompleted);

        await server.Put($"todo/incomplete/{todoId}", None, None);
        todoList = await server.Get<List<TodoListItem>>("todo/list", None);
        Assert.Single(todoList);
        Assert.False(todoList[0].IsCompleted);
    }

    [Fact(DisplayName = "Get list of all incomplete todos")]
    public async Task Test7()
    {
        await using var server = await TestServer.Create();
        Assert.Empty(await server.Get<List<TodoListItem>>("todo/list", None));
        await server.Post("todo/create", None, new TodoCreation { Title = "my todo 1" });
        await server.Post("todo/create", None, new TodoCreation { Title = "my todo 2" });
        await server.Post("todo/create", None, new TodoCreation { Title = "my todo 3" });
        await server.Post("todo/create", None, new TodoCreation { Title = "my todo 4" });
        await server.Put("todo/completed/2", None, None);
        var todoList = await server.Get<List<TodoListItem>>("todo/list", None);
        Assert.Equal(4, todoList.Count);

        todoList = await server.Get<List<TodoListItem>>("todo/list-incomplete", None);
        Assert.Equal(3, todoList.Count);
        Assert.All(todoList, todo => todo.IsCompleted.Equals(false));
    }

    [Fact(DisplayName = "Get list of all complete todos")]
    public async Task Test8()
    {
        await using var server = await TestServer.Create();
        Assert.Empty(await server.Get<List<TodoListItem>>("todo/list", None));
        await server.Post("todo/create", None, new TodoCreation { Title = "my todo 1" });
        await server.Post("todo/create", None, new TodoCreation { Title = "my todo 2" });
        await server.Post("todo/create", None, new TodoCreation { Title = "my todo 3" });
        await server.Post("todo/create", None, new TodoCreation { Title = "my todo 4" });
        await server.Put("todo/completed/2", None, None);
        await server.Put("todo/completed/3", None, None);
        var todoList = await server.Get<List<TodoListItem>>("todo/list", None);
        Assert.Equal(4, todoList.Count);

        todoList = await server.Get<List<TodoListItem>>("todo/list-complete", None);
        Assert.Equal(2, todoList.Count);
        Assert.All(todoList, todo => todo.IsCompleted.Equals(true));
    }
}