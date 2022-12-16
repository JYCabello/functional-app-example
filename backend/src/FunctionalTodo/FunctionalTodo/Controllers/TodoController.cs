using DeFuncto;
using FunctionalTodo.DomainModel;
using FunctionalTodo.Models;
using Microsoft.AspNetCore.Mvc;
using static DeFuncto.Prelude;

namespace FunctionalTodo.Controllers;

[ApiController]
[Route("[controller]")]
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
    public Task<ActionResult<IEnumerable<TodoListItem>>> Get() =>
        Get(dbAccessFunctions.GetAllFromDb);

    public async Task<ActionResult<IEnumerable<TodoListItem>>> Get(GetAllFromDb gafdb)
    {
        var todos = await gafdb();
        return Ok(todos);
    }
}
