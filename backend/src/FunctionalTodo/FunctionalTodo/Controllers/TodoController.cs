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

    // Ayuda solo puedo poner un HttpGet, si pongo mas se rompen los tests
    // creo que es problema del TestServer
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
    public ResultHandler<TodoListItem> GetById(int id) =>
        GetById(dbAccessFunctions.GetTodoById, id);

    [HttpPost(Name = "Create")]
    [Route("create")]
    public ResultHandler<int> Create(TodoCreation dto) =>
        Create(dbAccessFunctions.CreateTodo, dbAccessFunctions.FindByTitle, dto);

    [HttpPut(Name = "MarkAsComplete")]
    [Route("completed/{id:int}")]
    public ResultHandler<Unit> MarkAsComplete(int id) =>
        MarkAsComplete(dbAccessFunctions.MarkAsComplete, dbAccessFunctions.FindById,
            dbAccessFunctions.CheckIfCompleted, id);

    [HttpPut(Name = "MarkAsIncomplete")]
    [Route("incomplete/{id:int}")]
    public Task<ActionResult> MarkAsIncomplete(int id) =>
        MarkAsIncomplete(dbAccessFunctions.MarkTodoAsIncomplete, dbAccessFunctions.FindById,
            dbAccessFunctions.CheckIfCompleted, id);

    // En que momento se devuelve la clase ResultHandler?
    private ResultHandler<int> Create(CreateTodo createTodo, FindByTitle findByTitle, TodoCreation dto)
    {
        AsyncResult<Unit, AlternateFlow> LiftFind(AsyncOption<TodoListItem> todo) =>
            todo.Match(_ => Error(AlternateFlow.Conflict), () => Result<Unit, AlternateFlow>.Ok(unit));

        AsyncResult<int, AlternateFlow> LiftInsert(Task<int> queryResult)
        {
            async Task<Result<int, AlternateFlow>> GoLift()
            {
                var id = await queryResult;
                return id;
            }

            return GoLift();
        }

        return
            from isNewTodo in findByTitle(dto.Title).Apply(LiftFind)
            from inserted in createTodo(dto).Apply(LiftInsert)
            select inserted;
        // Esto te lo dejo como referencia, bórralo cuando seas mayor.
        /*async Task<Result<Unit, AlternateFlow>> Go()
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

        return Go();*/
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

    // HECHO
    // Haz que esto sea ResultHandler<TodoListItem>
    private ResultHandler<TodoListItem> GetById(GetById gbi, int id)
    {
        AsyncResult<TodoListItem, AlternateFlow> LiftGet(AsyncOption<TodoListItem> todo) =>
            todo.Match(foundTodo => Result<TodoListItem, AlternateFlow>.Ok(foundTodo),
                () => Error(AlternateFlow.Notfound));

        return from todo in gbi(id).Apply(LiftGet)
            select todo;
    }

    // Haz que esto sea ResultHandler<Unit> y retorne:
    private ResultHandler<Unit> MarkAsComplete(MarkTodoAsComplete mtac, FindById findById,
        CheckIfCompleted checkIfCompleted, int id)
    {
        AsyncResult<Unit, AlternateFlow> LiftFind(AsyncOption<TodoListItem> todo) =>
            todo.Match(_ => Result<Unit, AlternateFlow>.Ok(unit), () => Error(AlternateFlow.Notfound));

        AsyncResult<Unit, AlternateFlow> LiftCheckIfCompleted(AsyncOption<Unit> isItComplete) =>
            isItComplete.Match(_ => Error(AlternateFlow.Conflict), () => Result<Unit, AlternateFlow>.Ok(unit));

        AsyncResult<Unit, AlternateFlow> LiftMarkTodo(Task<Unit> queryResult) {
            async Task<Result<Unit, AlternateFlow>> GoLift()
            {
                await queryResult;
                return unit;
            };
            return GoLift();
        }
        
        return
            from foundTodo in findById(id).Apply(LiftFind)
            from incompleteTodo in checkIfCompleted(id).Apply(LiftCheckIfCompleted)
            from markedTodo in mtac(id).Apply(LiftMarkTodo)
            select markedTodo;
    }

    // Haz que esto sea ResultHandler<Unit> y retorne:
    private async Task<ActionResult> MarkAsIncomplete(MarkTodoAsIncomplete mtai, FindById findById,
        CheckIfCompleted checkIfCompleted, int id)
    {
        var found = await findById(id);
        if (found.IsNone) return NotFound();
        var isAlreadyCompleted = await checkIfCompleted(id);
        if (isAlreadyCompleted.IsSome) return Conflict();
        await mtai(id);
        return Ok();
    }
}