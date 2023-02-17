using System.Net;
using DeFuncto;
using DeFuncto.Extensions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace FunctionalTodo;

public enum AlternateFlow
{
    Conflict,
    Notfound
}

public class ResultHandler<TOk> : IActionResult
{
    private readonly AsyncResult<TOk, AlternateFlow> asyncResult;

    public ResultHandler(AsyncResult<TOk, AlternateFlow> asyncResultAsync) =>
        asyncResult = asyncResultAsync;

    public Task ExecuteResultAsync(ActionContext context) =>
        asyncResult
            .Match(ok => Handle(ok, context), err => Handle(err, context))
            .Map(result => result.ExecuteResultAsync(context));

    public static implicit operator ResultHandler<TOk>(Result<TOk, AlternateFlow> either) =>
        new(either.Async());

    public static implicit operator ResultHandler<TOk>(Task<Result<TOk, AlternateFlow>> either) =>
        new(either.Async());

    public static implicit operator ResultHandler<TOk>(AsyncResult<TOk, AlternateFlow> either) =>
        new(either);

    private static ActionResult ErrorResult(string message, HttpStatusCode code) => new ContentResult
    {
        Content = JsonConvert.SerializeObject(
            new ErrorDetails(message, (int)HttpStatusCode.InternalServerError)
        ),
        ContentType = "application/json",
        StatusCode = (int)code
    };

    private static ActionResult ExceptionResult(string message, Exception? exception, ActionContext context) =>
        new ContentResult
        {
            Content = JsonConvert.SerializeObject(
                new ErrorDetails(message, (int)HttpStatusCode.InternalServerError)
            ),
            ContentType = "application/json",
            StatusCode = (int)HttpStatusCode.InternalServerError
        };

    private static IActionResult Handle(TOk right, ActionContext context) =>
        right switch
        {
            Unit _ => new NoContentResult(),
            _ => new OkObjectResult(right)
        };

    private static IActionResult Handle(AlternateFlow alternateFlow, ActionContext context) =>
        alternateFlow switch
        {
            AlternateFlow.Conflict => ErrorResult("Conflict", HttpStatusCode.Conflict),
            AlternateFlow.Notfound => ErrorResult("Not Found", HttpStatusCode.NotFound),
            _ => ErrorResult("Error desconocido.", HttpStatusCode.InternalServerError)
        };
}

internal record ErrorDetails(string Message, int StatusCode);
