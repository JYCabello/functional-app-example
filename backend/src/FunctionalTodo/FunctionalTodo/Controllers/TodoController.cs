using Microsoft.AspNetCore.Mvc;

namespace FunctionalTodo.Controllers;

[ApiController]
[Route("[controller]")]
public class TodoController : ControllerBase
{
    private readonly ILogger<TodoController> logger;

    public TodoController(ILogger<TodoController> logger) =>
        this.logger = logger;

    [HttpGet(Name = "List")]
    public IEnumerable<WeatherForecast> Get() =>
        throw new NotImplementedException("todo");
}
