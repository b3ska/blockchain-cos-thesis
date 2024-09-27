using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using System.Net.Sockets;
using TcpClient = System.Net.Sockets.TcpClient;
using TcpListener = System.Net.Sockets.TcpListener;\

using var httpClient = new HttpClient(); // only for automatic public IP detection
var publicIp = await httpClient.GetStringAsync("https://api.ipify.org");

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
// TODO: ask user to fill the data, write json

Node host = new Node("Artem Kalmakov", "banana computer graph", publicIp);
var blockchain = host.Chain;

app.UseStaticFiles();

Console.WriteLine(publicIp + " is your public IP address that will be used to host the node");

app.MapGet("/", () => Results.Content(System.IO.File.ReadAllText("wwwroot/index.html"), "text/html"));

app.MapGet("/blocks", () =>
{
    var blocks = blockchain.GetBlocks();
    return Results.Json(blocks);
});

app.MapGet("/searchData", (string data) =>
{
    var block = blockchain.GetBlockByData(data);
    if (block == null)
    {
        return Results.NotFound();
    }
    return Results.Json(block);
});

app.MapGet("/searchHash", (string hash) =>
{
    var block = blockchain.GetBlockByHash(hash);
    if (block == null)
    {
        return Results.NotFound();
    }
    return Results.Json(block);
});

app.MapPost("/create", async (HttpContext httpContext) =>
{
    using var reader = new StreamReader(httpContext.Request.Body);
    var data = await reader.ReadToEndAsync();
    Console.WriteLine(data);
    blockchain.AddBlock(data);
    return Results.Text("Block created successfully");
});

app.Run();