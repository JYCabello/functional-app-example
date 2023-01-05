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
        await server.Post("todo/create", None, todoBody);

        var todoList = await server.Get<List<TodoListItem>>("todo/list", None);
        var todoId = todoList[0].ID;
        var todo = todoList[0];
        Assert.Single(todoList);
        Assert.False(todo.IsCompleted);

        try
        {
            await server.Put($"todo/completed/{todoId}", None, None);
        }
        catch (FlurlHttpException ex)
        {
            Assert.Fail("Couldn't mark todo as complete");
        }

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
        await server.Post("todo/create", None, todoBody);
        var todoList = await server.Get<List<TodoListItem>>("todo/list", None);
        var todoId = todoList[0].ID;
        await server.Put($"todo/completed/{todoId}", None, None);
        
        todoList = await server.Get<List<TodoListItem>>("todo/list", None);
        var todo = todoList[0];
        Assert.Single(todoList);
        Assert.True(todo.IsCompleted);

        try
        {
            await server.Put($"todo/incomplete/{todoId}", None, None);
        }
        catch (FlurlHttpException ex)
        {
            Assert.Fail("Couldn't mark todo as complete");
        }

        var todoListAfterMarkingIncomplete = await server.Get<List<TodoListItem>>("todo/list", None);
        Assert.Single(todoListAfterMarkingIncomplete);
        Assert.False(todoListAfterMarkingIncomplete[0].IsCompleted);
        
        try
        {
            await server.Put($"todo/incomplete/{todoId}", None, None);
            Assert.Fail("Should not have updated incomplete todo");
        }
        catch (FlurlHttpException ex)
        {
            Assert.Equal(409, ex.StatusCode);
        }
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
        try
        {
            todoList = await server.Get<List<TodoListItem>>("todo/list-incomplete", None);
        }
        catch (FlurlHttpException ex)
        {
            Assert.Fail("Couldn't get list");
        }

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
        try
        {
            todoList = await server.Get<List<TodoListItem>>("todo/list-complete", None);
        }
        catch (FlurlHttpException ex)
        {
            Assert.Fail("Couldn't get list");
        }

        Assert.Equal(2, todoList.Count);
        Assert.All(todoList, todo => todo.IsCompleted.Equals(true));
    }
}