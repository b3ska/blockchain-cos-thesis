using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var blockchain = new BlockChain();

// Serve static files
app.UseStaticFiles();

app.MapGet("/", () => Results.Content(System.IO.File.ReadAllText("wwwroot/index.html"), "text/html"));

app.MapGet("/blocks", () =>
{
    var blocks = blockchain.GetBlocks();
    return Results.Json(blocks);
});

app.MapGet("/search", (string data) =>
{
    var block = blockchain.GetBlockByData(data);
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