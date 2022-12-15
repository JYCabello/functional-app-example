// See https://aka.ms/new-console-template for more information

using System.Reflection;
using DbUp;

namespace FunctionalTodo.Migrations;
public static class Program
{
    public static int Main(string[] args)
    {
        var connectionString =
            args.FirstOrDefault()
            ?? "Server=localhost,1314;Database=todo;User Id=sa;Password=abcd1234ABCD;TrustServerCertificate=True";

        var upgrader =
            DeployChanges.To
                .SqlDatabase(connectionString)
                .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
                .LogToConsole()
                .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(result.Error);
            Console.ResetColor();
            return -1;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Success!");
        Console.ResetColor();
        return 0;
    }
}
