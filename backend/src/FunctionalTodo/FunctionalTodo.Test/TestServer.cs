﻿using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using DeFuncto;
using DeFuncto.Extensions;
using Docker.DotNet;
using Docker.DotNet.Models;
using Flurl;
using Flurl.Http;
using Flurl.Http.Configuration;
using FunctionalTodo.DomainModel;
using FunctionalTodo.Migrations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
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

    public static async Task DeleteContainer(string id)
    {
        using var client = GetClient();
        await client.Containers.StopContainerAsync(id, new ContainerStopParameters());
        await client.Containers.RemoveContainerAsync(id, new ContainerRemoveParameters());
    }

    public static CreateContainerParameters SqlServerParams(int port)
    {
        var name = $"fp-api-sample-irene{port}";
        var portBinding = new PortBinding
        {
            HostPort = $"{port}/tcp",
            HostIP = "0.0.0.0"
        };
        var bindings = new Dictionary<string, IList<PortBinding>>
        {
            { "1433/tcp", new List<PortBinding> { portBinding } }
        };
        var hostConfig = new HostConfig
        {
            PortBindings = bindings
        };

        var environmentVars = new List<string>
        {
            "SA_PASSWORD=abcd1234ABCD",
            "ACCEPT_EULA=Y"
        };
        return new CreateContainerParameters
        {
            Name = name,
            Image = "mcr.microsoft.com/mssql/server:2022-latest",
            Env = environmentVars,
            HostConfig = hostConfig
        };
    }
}

public class TestSettingsFunctions : ISettingsFunctions
{
    private readonly int port;

    public TestSettingsFunctions(int port) =>
        this.port = port;

    public GetConnectionString GetConnectionString =>
        () => $"Server=localhost,{port};Database=todo;User Id=sa;Password=abcd1234ABCD;TrustServerCertificate=True";
}

public class TestServer : IAsyncDisposable, IDisposable
{
    private static readonly SemaphoreSlim Sm = new(1);

    private static readonly Random Rn = new();

    private readonly WebApplication host;
    private readonly string sqlContainerID;
    private readonly string url;

    private TestServer(string sqlContainerID, int sqlPort)
    {
        this.sqlContainerID = sqlContainerID;
        var port = GetPort();
        var builder = Startup.GetBuilder(Array.Empty<string>());
        builder.WebHost.UseUrls($"https://localhost:{port}");
        builder.Services.AddSingleton<ISettingsFunctions>(_ => new TestSettingsFunctions(sqlPort));
        host = Startup.BuildApp(builder);
        host.Start();
        url = $"https://localhost:{port}/";
    }

    private static int RandomPort => Rn.Next(30_000) + 10_000;

    public async ValueTask DisposeAsync()
    {
        (host as IDisposable).Dispose();
        await DockerUtilities.DeleteContainer(sqlContainerID);
    }

    public void Dispose()
    {
        (host as IDisposable).Dispose();
        DockerUtilities.DeleteContainer(sqlContainerID).RunSynchronously();
    }

    public static async Task<TestServer> Create()
    {
        var sqlPort = GetPort();
        var createParams = DockerUtilities.SqlServerParams(sqlPort);
        var id = await DockerUtilities.StartContainer(createParams);
        var connectionString =
            $"Server=localhost,{sqlPort};Database=todo;User Id=sa;Password=abcd1234ABCD;TrustServerCertificate=True";
        await TryMigrate(0);
        return new TestServer(id, sqlPort);

        async Task TryMigrate(int count)
        {
            try
            {
                await using var connection =
                    new SqlConnection($"Server=localhost,{sqlPort};User Id=sa;Password=abcd1234ABCD;TrustServerCertificate=True");
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "CREATE DATABASE todo";
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception)
            {
                if (count > 45)
                    throw;
                await Task.Delay(500);
                await TryMigrate(count + 1);
            }

            if (Program.Main(new[] { connectionString }) != 0)
            {
                if (count > 45)
                    throw new Exception("Could not migrate");
                await Task.Delay(250);
                await TryMigrate(count + 1);
            }
        }
    }

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
    
    public Task<Unit> Put(string path, Option<Dictionary<string, string>> queryParams, object body) =>
        BaseReq(path, queryParams).PutJsonAsync(body).Map(_ => unit);

    public Task<T> Put<T>(string path, Option<Dictionary<string, string>> queryParams, object body) =>
        BaseReq(path, queryParams).PutJsonAsync(body).Map(r => r.GetJsonAsync<T>());

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
