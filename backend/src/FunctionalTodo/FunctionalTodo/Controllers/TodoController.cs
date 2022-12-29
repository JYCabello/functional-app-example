using DeFuncto;
using FunctionalTodo.DomainModel;
using FunctionalTodo.Models;
using Microsoft.AspNetCore.Mvc;
using static DeFuncto.Prelude;

namespace FunctionalTodo.Controllers;

[ApiController]
[Route("todo")]
public class TodoController : ControllerBase
{
    private readonly ILogger<TodoController> logger;
    private readonly IDbAccessFunctions dbAccessFunctions;

    public TodoController(ILogger<TodoController> logger, IDbAccessFunctions dbAccessFunctions)
    {
        this.logger = logger;
        this.dbAccessFunctions = dbAccessFunctions;
    }

    [HttpGet(Name = "List")]
    [Route("list")]
    public Task<ActionResult<IEnumerable<TodoListItem>>> Get() =>
        Get(dbAccessFunctions.GetAllFromDb);

    [HttpPost(Name = "Create")]
    [Route("create")]
    public Task<ActionResult> Create(TodoCreation dto) =>
        Create(dbAccessFunctions.CreateTodo, dto);

    [HttpPost(Name = "GetById")]
    [Route("id/{id:int}")]
    public Task<ActionResult<TodoListItem>> GetById(int id) =>
        GetById(dbAccessFunctions.GetTodoById, id);
    
    [HttpPost(Name = "MarkAsComplete")]
    [Route("completed")]
    public Task<ActionResult> MarkAsComplete(TodoListItem dto) =>
        MarkAsCompleted(dbAccessFunctions.MarkAsCompleted, dto);

    private async Task<ActionResult> Create(CreateTodo createTodo, TodoCreation dto)
    {
        var success = await createTodo(dto);
        if (success) return Ok();
        return Conflict();
    }

    private async Task<ActionResult<IEnumerable<TodoListItem>>> Get(GetAllFromDb gafdb)
    {
        var todos = await gafdb();
        return Ok(todos);
    }

    private async Task<ActionResult<TodoListItem>> GetById(GetById gbi, int id)
    {
        var todo = await gbi(id);
        if (todo != null)
        {
            return Ok(todo);
        }

        return NotFound();
    }
    
    private async Task<ActionResult> MarkAsCompleted(MarkTodoAsCompleted mtac, TodoListItem dto)
    {
        await mtac(dto);
        return Ok();
    }
}