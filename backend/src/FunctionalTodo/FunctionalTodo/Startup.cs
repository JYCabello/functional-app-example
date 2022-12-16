using System.Reflection;
using FunctionalTodo.DomainModel;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace FunctionalTodo;

public static class Startup
{
    public static WebApplicationBuilder GetBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder
            .Services
            .AddControllers()
            .PartManager
            .ApplicationParts
            .Add(new AssemblyPart(typeof(Startup).GetTypeInfo().Assembly));
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddSingleton<IDbAccessFunctions, DbAccessFunctions>();

        return builder;
    }

    public static WebApplication BuildApp(WebApplicationBuilder builder)
    {
        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        return app;
    }

    public static WebApplication BuildApp(string[] args) =>
        BuildApp(GetBuilder(args));
}
