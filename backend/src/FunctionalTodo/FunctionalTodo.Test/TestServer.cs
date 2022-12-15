using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using DeFuncto;
using DeFuncto.Extensions;
using Docker.DotNet;
using Docker.DotNet.Models;
using Flurl;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using static DeFuncto.Prelude;
using NullValueHandling = Newtonsoft.Json.NullValueHandling;


namespace FunctionalTodo.Test;

public static class DockerUtilities
{
    private static DockerClient GetClient()
    {
        var defaultWindowsDockerEngineUri = new Uri("npipe://./pipe/docker_engine");
        var defaultLinuxDockerEngineUri =
            Environment.GetEnvironmentVariable("DOCKER_HOST") switch
            {
                null => new Uri("unix:///var/run/docker.sock"),
                var value => new Uri(value)
            };
        var engineUri =
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? defaultWindowsDockerEngineUri
                : defaultLinuxDockerEngineUri;

        return new DockerClientConfiguration(engineUri).CreateClient();
    }

    public static async Task<string> StartContainer(CreateContainerParameters cp)
    {
        using var client = GetClient();
        await client.Images.CreateImageAsync(
            new ImagesCreateParameters
            {
                FromImage = cp.Image.Split(":")[0],
                Tag = cp.Image.Split(":")[1]
            },
            null,
            new Progress<JSONMessage>());

        var containers =
            await client.Containers.ListContainersAsync(
                new ContainersListParameters { All = true }
            );

        var container =
            containers.SingleOrNone(c => c.Names.Any(n => n == $"/{cp.Name}"));

        var id = await
            container
                .Match(
                    clr => clr.ID.Apply(Task.FromResult),
                    async () =>
                    {
                        var response = await client.Containers.CreateContainerAsync(cp);
                        return response.ID;
                    });
        
        await client.Containers.StartContainerAsync(id, new ContainerStartParameters());

        return id;
    }
}

public class TestServer : IDisposable
{
    private static readonly SemaphoreSlim Sm = new(1);

    private static readonly Random Rn = new();

    private readonly WebApplication host;
    private readonly string url;

    public TestServer()
    {
        var port = GetPort();
        var builder = Startup.GetBuilder(Array.Empty<string>());
        builder.WebHost.UseUrls($"https://localhost:{port}");
        host = Startup.BuildApp(builder);
        host.Start();
        url = $"https://localhost:{port}/";
    }

    private static int RandomPort => Rn.Next(30_000) + 10_000;

    public void Dispose() =>
        (host as IDisposable).Dispose();

    private static TcpConnectionInformation[] GetConnectionInfo() =>
        IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();

    private static int GetPort()
    {
        Sm.Wait();
        var port = Go(RandomPort);
        Sm.Release();
        return port;

        static int Go(int portNumber) =>
            GetConnectionInfo().Any(ci => ci.LocalEndPoint.Port == portNumber)
                ? Go(RandomPort)
                : portNumber;
    }

    private IFlurlRequest BaseReq(string path, Option<Dictionary<string, string>> queryParams)
    {
        // To make sure both serializers work.
        if (Rn.Next(10) % 2 == 0)
            FlurlHttp.Configure(settings =>
                settings.JsonSerializer = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    ObjectCreationHandling = ObjectCreationHandling.Replace
                }.Apply(jss => new NewtonsoftJsonSerializer(jss))
            );

        return queryParams
            .DefaultValue(new Dictionary<string, string>())
            .ToList()
            .Aggregate(
                url.AppendPathSegment(path).ConfigureRequest(_ => { }),
                (acc, kvp) => acc.SetQueryParam(kvp.Key, kvp.Value)
            );
    }

    public Task<T> Get<T>(string path, Option<Dictionary<string, string>> queryParams) =>
        BaseReq(path, queryParams).GetJsonAsync<T>();

    public Task<string> Get(string path, Option<Dictionary<string, string>> queryParams) =>
        BaseReq(path, queryParams).GetStringAsync();

    public Task<Unit> Post(string path, Option<Dictionary<string, string>> queryParams, object body) =>
        BaseReq(path, queryParams).PostJsonAsync(body).Map(_ => unit);

    public Task<T> Post<T>(string path, Option<Dictionary<string, string>> queryParams, object body) =>
        BaseReq(path, queryParams).PostJsonAsync(body).Map(r => r.GetJsonAsync<T>());

    public Task<Unit> PostFile(
        string path,
        Option<Dictionary<string, string>> queryParams,
        Stream stream,
        string fileName
    ) =>
        BaseReq(path, queryParams)
            .PostMultipartAsync(mp => mp.AddFile("image", stream, fileName))
            .Map(_ => unit);
}
