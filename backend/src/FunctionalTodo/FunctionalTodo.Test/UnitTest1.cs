using DeFuncto;

namespace FunctionalTodo.Test;

using static Prelude;

public class UnitTest1
{
    [Fact]
    public async Task Test1()
    {
        using var server = new TestServer();

        var x = await server.Get<List<WeatherForecast>>("WeatherForecast", None);
        int a = 9;
    }
}
