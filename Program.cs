using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using System.Net.Sockets;
using TcpClient = System.Net.Sockets.TcpClient;
using TcpListener = System.Net.Sockets.TcpListener;
using System.Net;
using Microsoft.Extensions.FileProviders;
using System.Security.Cryptography;

using var httpClient = new HttpClient(); // only for automatic public IP detection
var publicIp = await httpClient.GetStringAsync("https://api.ipify.org");
Console.WriteLine(publicIp + " is your public IP address that will be used to host the node");

var fileDirectory = "files/";

if (!Directory.Exists(fileDirectory)) Directory.CreateDirectory(fileDirectory);

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Load known nodes from JSON
string json = File.ReadAllText("known_nodes.json");
Dictionary<string, string> knownNodes = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();

Node host = new Node("Artem Kalmakov", "banana computer graph", IPAddress.Parse(publicIp));
var blockchain = host.chain;

if (knownNodes.ContainsKey(host.publicKey)) {
    if (knownNodes[host.publicKey] == publicIp) Console.WriteLine("This node is already known and has the same IP");
    else {
        knownNodes[host.publicKey] = publicIp;
        Console.WriteLine("This node is already known but has a different IP, updating the known nodes list");
    }
} else {
    knownNodes.Add(host.publicKey, publicIp);
    json = JsonSerializer.Serialize(knownNodes);
    File.WriteAllText("known_nodes.json", json);
    Console.WriteLine("This node is new and has been added to the known nodes list");
}

app.UseStaticFiles();

foreach (var nodeEntry in knownNodes) {
    string publicKey = nodeEntry.Key;
    string nodeIp = nodeEntry.Value;
    if (nodeIp == publicIp) continue;

    // recieve chain from node
    var node = new Node(publicKey, IPAddress.Parse(nodeIp));
    await node.ConnectNode();
    Console.WriteLine($"Synchronising chain with node with public key: {publicKey} at IP: {nodeIp}");
    
}

app.MapGet("/", () => Results.Content(File.ReadAllText("wwwroot/index.html"), "text/html"));

app.MapGet("/blocks", () => {
    var blocks = blockchain.GetBlocks();
    return Results.Json(blocks);
});

app.MapGet("/searchData", (string data) => {
    var block = blockchain.GetBlockByData(data);
    if (block == null)
    {
        return Results.NotFound();
    }
    return Results.Json(block);
});

app.MapGet("/searchHash", (string hash) => {
    var block = blockchain.GetBlockByHash(hash);
    if (block == null)
    {
        return Results.NotFound();
    }
    return Results.Json(block);
});

app.MapPost("/create", async (HttpContext httpContext) => {
    var form = await httpContext.Request.ReadFormAsync(); // Read the form data

    var blockData = form["blockData"].ToString();

    var fileInput = form.Files["fileInput"];
    var fileContent = "";

    string data;
    if (string.IsNullOrEmpty(blockData)) {
        if (fileInput != null) {
            using (var memoryStream = new MemoryStream()) {
                await fileInput.CopyToAsync(memoryStream);
                fileContent = Convert.ToBase64String(memoryStream.ToArray());
            }
            var jsonData = new {
                fileName = form["fileName"].ToString(),
                fileContent
            };
            data = JsonSerializer.Serialize(jsonData);
            blockchain.AddPendingBlock(Block.NewBlock(blockchain.GetLastBlock(false), data));
        }
    } else {
        if (fileInput != null) {
            using (var memoryStream = new MemoryStream()) {
                await fileInput.CopyToAsync(memoryStream);
                fileContent = Convert.ToBase64String(memoryStream.ToArray());
            }
            var jsonData = new {
                fileName = form["fileName"].ToString(),
                fileContent,
                blockData
            };
            data = JsonSerializer.Serialize(jsonData);
        }
        else {
            var jsonData = new {
                blockData
            };
            data = JsonSerializer.Serialize(jsonData);
        }
        blockchain.AddPendingBlock(Block.NewBlock(blockchain.GetLastBlock(false), data));
    }

    Console.WriteLine($"Block Data received");
    return Results.Text("Block added to queue of pending blocks");
});


app.MapPost("/mine", async (HttpContext httpContext) => {
    using var reader = new StreamReader(httpContext.Request.Body);
    var data = await reader.ReadToEndAsync();
    Console.WriteLine(data);
    foreach (var block in blockchain.pendingBlocks) {
        block.prevHash = blockchain.GetLastBlock(true).hash;
        block.MineBlock(host.publicKey);
        blockchain.AddBlock(block);
        blockchain.pendingBlocks.Remove(block);
        return Results.Text($"Block {block} was mined and added to the blockchain");
    }
    return Results.Text("No blocks to mine");
});

app.MapGet("/pendingBlocks", () => {
    var pendingBlocks = blockchain.pendingBlocks;
    return Results.Json(pendingBlocks);
});

app.UseStaticFiles(new StaticFileOptions {
    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "files")),
    RequestPath = "/files",
    ServeUnknownFileTypes = true, // This will serve files with unknown MIME types
    DefaultContentType = "application/octet-stream" // Default MIME type if none is provided
});


app.Run();
