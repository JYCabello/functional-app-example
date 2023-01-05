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

    // Ayuda solo puedo poner un HttpGet, si pongo mas se rompen
    [HttpGet(Name = "List")]
    [Route("list")]
    public Task<ActionResult<IEnumerable<TodoListItem>>> Get() =>
        Get(dbAccessFunctions.GetAllFromDb);

    [HttpPost(Name = "ListIncomplete")]
    [Route("list-incomplete")]
    public Task<ActionResult<IEnumerable<TodoListItem>>> GetIncomplete() =>
        GetIncomplete(dbAccessFunctions.GetAllIncompleteFromDb);

    [HttpPost(Name = "ListComplete")]
    [Route("list-complete")]
    public Task<ActionResult<IEnumerable<TodoListItem>>> GetComplete() =>
        GetComplete(dbAccessFunctions.GetAllCompleteFromDb);

    [HttpPost(Name = "GetById")]
    [Route("id/{id:int}")]
    public Task<ActionResult<TodoListItem>> GetById(int id) =>
        GetById(dbAccessFunctions.GetTodoById, id);

    [HttpPost(Name = "Create")]
    [Route("create")]
    public ResultHandler<Unit> Create(TodoCreation dto) =>
        Create(dbAccessFunctions.CreateTodo, dbAccessFunctions.FindByTitle, dto);

    [HttpPut(Name = "MarkAsComplete")]
    [Route("completed/{id:int}")]
    public Task<ActionResult> MarkAsComplete(int id) =>
        MarkAsCompleted(dbAccessFunctions.MarkAsCompleted, dbAccessFunctions.FindById,
            dbAccessFunctions.CheckIfCompleted, id);

    [HttpPut(Name = "MarkAsIncomplete")]
    [Route("incomplete/{id:int}")]
    public Task<ActionResult> MarkAsIncomplete(int id) =>
        MarkAsIncomplete(dbAccessFunctions.MarkTodoAsIncomplete, dbAccessFunctions.FindById,
            dbAccessFunctions.CheckIfCompleted, id);

    // hacer que devuelva el id; cambiar tests de mark complete / incomplete por el todoId, y de create para comprovarlo
    private ResultHandler<Unit> Create(CreateTodo createTodo, FindByTitle findByTitle, TodoCreation dto)
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
                    error => Error<Unit, AlternateFlow>(error).ToTask());
        }

        // como hago que not found no sea un error, en este caso es positivo que no lo encontremos
        // así
        return Go();
    }

    private async Task<ActionResult<IEnumerable<TodoListItem>>> Get(GetAllFromDb gafdb)
    {
        var todos = await gafdb();
        return Ok(todos);
    }

    private async Task<ActionResult<IEnumerable<TodoListItem>>> GetIncomplete(GetAllIncompleteFromDb gaifdb)
    {
        var todos = await gaifdb();
        return Ok(todos);
    }
    
    private async Task<ActionResult<IEnumerable<TodoListItem>>> GetComplete(GetAllCompleteFromDb gacfdb)
    {
        var todos = await gacfdb();
        return Ok(todos);
    }

    // Haz que esto sea ResultHandler<TodoListItem>
    // no debe ser nullable, debe lanzar error en repositorio
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
    private async Task<ActionResult> MarkAsCompleted(MarkTodoAsCompleted mtac, FindById findById,
        CheckIfCompleted checkIfCompleted, int id)
    {
        var found = await findById(id);
        if (found.IsNone) return NotFound();
        var isAlreadyCompleted = await checkIfCompleted(id);
        if (isAlreadyCompleted) return Conflict();
        await mtac(id);
        return Ok();
    }

    // Haz que esto sea ResultHandler<Unit> y retorne:
    private async Task<ActionResult> MarkAsIncomplete(MarkTodoAsIncomplete mtai, FindById findById,
        CheckIfCompleted checkIfCompleted, int id)
    {
        var found = await findById(id);
        if (found.IsNone) return NotFound();
        var isAlreadyCompleted = await checkIfCompleted(id);
        if (!isAlreadyCompleted) return Conflict();
        await mtai(id);
        return Ok();
    }
}