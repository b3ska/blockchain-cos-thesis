using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using System.Net.Sockets;
using TcpClient = System.Net.Sockets.TcpClient;
using TcpListener = System.Net.Sockets.TcpListener;

using var httpClient = new HttpClient(); // only for automatic public IP detection
var publicIp = await httpClient.GetStringAsync("https://api.ipify.org");

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Load known nodes from JSON
string json = System.IO.File.ReadAllText("known_nodes.json");
Dictionary<string, string> knownNodes = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();

Node host = new Node("Artem Kalmakov", "banana computer graph", publicIp);
var blockchain = host.Chain;

app.UseStaticFiles();

Console.WriteLine(publicIp + " is your public IP address that will be used to host the node");

// Connect to each known node's IP
foreach (var nodeEntry in knownNodes)
{
    string publicKey = nodeEntry.Key;
    string nodeIp = nodeEntry.Value;

    Console.WriteLine($"Connecting to node with public key: {publicKey} at IP: {nodeIp}");

    // Connect to the node via its IP
    using (TcpClient client = new TcpClient())
    {
        try
        {
            await client.ConnectAsync(nodeIp, 8080); 
            Console.WriteLine($"Connected to {nodeIp}");

            NetworkStream stream = client.GetStream();
            byte[] data = System.Text.Encoding.UTF8.GetBytes($"SYNC_REQUEST:{host.publicKey}");
            await stream.WriteAsync(data, 0, data.Length);

            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string response = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine($"Received from {nodeIp}: {response}");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to connect to {nodeIp}: {ex.Message}");
        }
    }
}

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
    host.PendingBlocks.Add(Block.NewBlock(blockchain.GetLastBlock(), data));
    return Results.Text("Block added to queue of pending blocks");
});

app.MapPost("/mine", async (HttpContext httpContext) =>
{
    using var reader = new StreamReader(httpContext.Request.Body);
    var data = await reader.ReadToEndAsync();
    Console.WriteLine(data);
    foreach (var block in host.PendingBlocks) {
        block.prevHash = blockchain.GetLastBlock().hash;
        block.MineBlock(host.privateKey, host.publicKey);
        blockchain.AddBlock(block);
        host.PendingBlocks.Remove(block);
        return Results.Text($"Block {block} was mined and added to the blockchain");
    }
    return Results.Text("No blocks to mine");
});

app.MapGet("/pendingBlocks", () =>
{
    var pendingBlocks = host.PendingBlocks; // Assuming host has a property PendingBlocks
    return Results.Json(pendingBlocks);
});




app.Run();
