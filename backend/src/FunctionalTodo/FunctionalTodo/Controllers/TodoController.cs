using DeFuncto;
using DeFuncto.Extensions;
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
    public ResultHandler<Unit> Create(TodoCreation dto) =>
        Create2(dbAccessFunctions.CreateTodo, dbAccessFunctions.FindByTitle, dto);

    [HttpPost(Name = "GetById")]
    [Route("id/{id:int}")]
    public Task<ActionResult<TodoListItem>> GetById(int id) =>
        GetById(dbAccessFunctions.GetTodoById, id);
    
    [HttpPost(Name = "MarkAsComplete")]
    [Route("completed")]
    public Task<ActionResult> MarkAsComplete(TodoListItem dto) =>
        MarkAsCompleted(dbAccessFunctions.MarkAsCompleted, dto);

    private ResultHandler<Unit> Create2(CreateTodo createTodo, FindByTitle findByTitle, TodoCreation dto)
    {
        AsyncResult<Unit, AlternateFlow> LiftFind(AsyncOption<TodoListItem> todo) =>
            todo.Match(_ => Error(AlternateFlow.Conflict), () => Result<Unit, AlternateFlow>.Ok(unit));

        AsyncResult<Unit, AlternateFlow> LiftInsert(Task<int> queryResult)
        {
            async Task<Result<Unit, AlternateFlow>> GoLift()
            {
                await queryResult;
                return unit;
            }
            return GoLift();
        }

        return
            from isNewTodo in findByTitle(dto.Title).Apply(LiftFind)
            from inserted in createTodo(dto).Apply(LiftInsert)
            select inserted;

        // Esto te lo dejo como referencia, bórralo cuando seas mayor.
        async Task<Result<Unit, AlternateFlow>> Go()
        {
            Option<TodoListItem> todo = await findByTitle(dto.Title);

            Result<Unit, AlternateFlow> todoResult = todo
                .Match(_ => Error(AlternateFlow.Conflict), () => Result<Unit, AlternateFlow>.Ok(unit));
            
            return await todoResult
                .Match(async _ =>
                    {
                        await createTodo(dto);
                        return Ok<Unit, AlternateFlow>(unit);
                    },
                error => Error<Unit,AlternateFlow>(error).ToTask());
        }
        // como hago que not found no sea un error, en este caso es positivo que no lo encontremos
        // así
        return Go();

    }

    // es correcto separar el async result?
    private static async Task<AsyncResult<string, Errors>> FindByTitleResult(FindByTitle findByTitle, string title)
    {
        var titleOutput = await findByTitle(title);
        return from todo in titleOutput.Result(Errors.NotFound)
            select todo.Title;
    }

    private async Task<ActionResult<IEnumerable<TodoListItem>>> Get(GetAllFromDb gafdb)
    {
        var todos = await gafdb();
        return Ok(todos);
    }

    // Haz que esto sea ResultHandler<TodoListItem>
    private async Task<ActionResult<TodoListItem>> GetById(GetById gbi, int id)
    {
        var todo = await gbi(id);
        if (todo != null)
        {
            return Ok(todo);
        }

        return NotFound();
    }
    
    // Haz que esto sea ResultHandler<Unit> y retorne:
    // Conflict si ya está completo.
    // NotFound si no existe.
    private async Task<ActionResult> MarkAsCompleted(MarkTodoAsCompleted mtac, TodoListItem dto)
    {
        await mtac(dto);
        return Ok();
    }
}
