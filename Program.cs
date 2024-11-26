using System.Text.Json;
using System.Net;
using Microsoft.Extensions.FileProviders;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc.Routing;

// using var httpClient = new HttpClient(); // only for automatic public IP detection
// var publicIp = await httpClient.GetStringAsync("https://api.ipify.org");
// Console.WriteLine(publicIp + " is your public IP address that will be used to host the node");

var publicIp = Environment.GetEnvironmentVariable("NODE_IP") ?? "192.168.0.57";
var port = Environment.GetEnvironmentVariable("NODE_PORT") ?? "8000";
var nodeName = Environment.GetEnvironmentVariable("NODE_NAME") ?? "DefaultNode";
var nodeKeywords = Environment.GetEnvironmentVariable("NODE_KEYWORDS") ?? "default keywords";

Console.WriteLine($"{publicIp}:{port} is your IP address and port that will be used to host the node");
Console.WriteLine($"Node Name: {nodeName}");
Console.WriteLine($"Node Keywords: {nodeKeywords}");

var fileDirectory = "files/";

if (!Directory.Exists(fileDirectory)) Directory.CreateDirectory(fileDirectory);

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Load known nodes from JSON
string json = File.ReadAllText("known_nodes.json");
Dictionary<string, string> knownNodes = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();

Node host = new Node(nodeName, nodeKeywords, IPAddress.Parse(publicIp), int.Parse(port)); 
Console.WriteLine($"Public Key: {host.publicKey}");

if (knownNodes.ContainsKey(host.publicKey)) {
    if (knownNodes[host.publicKey] == publicIp+":"+port) Console.WriteLine("This node is already known and has the same IP");
    else {
        knownNodes[host.publicKey] = publicIp+":"+port;
        Console.WriteLine("This node is already known but has a different IP, updating the known nodes list");
        json = JsonSerializer.Serialize(knownNodes);
        File.WriteAllText("known_nodes.json", json);
    }
} else {
    knownNodes.Add(host.publicKey, publicIp+":"+port);
    json = JsonSerializer.Serialize(knownNodes);
    File.WriteAllText("known_nodes.json", json);
    Console.WriteLine("This node is new and has been added to the known nodes list");
}

app.UseStaticFiles();

foreach (var nodeEntry in knownNodes) {
    string publicKey = nodeEntry.Key;
    string nodeAddress = nodeEntry.Value;
    string nodePort = nodeAddress.Split(":")[1];
    string nodeIp = nodeAddress.Split(":")[0];
    if (nodeIp+":"+nodePort == publicIp+":"+port) continue;

    // connecting to the node
    var node = new Node(publicKey, IPAddress.Parse(nodeIp), int.Parse(nodePort));
    await host.ConnectNode(node);
    
}

app.MapGet("/", () => Results.Content(File.ReadAllText("wwwroot/index.html"), "text/html"));

app.MapGet("/blocks", () => {
    var blocks = host.chain.GetBlocks();
    return Results.Json(blocks);
});

app.MapGet("/searchData", (string data) => {
    var block = host.chain.GetBlockByData(data);
    if (block == null)
    {
        return Results.NotFound();
    }
    return Results.Json(block);
});

app.MapGet("/searchHash", (string hash) => {
    var block = host.chain.GetBlockByHash(hash);
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
            host.chain.AddPendingBlock(Block.NewBlock(data));
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
        host.chain.AddPendingBlock(Block.NewBlock(data));
    }

    Console.WriteLine($"Block Data received");
    host.sendPendingToAll();
    return Results.Text("Block added to queue of pending blocks");
});


app.MapPost("/mine", async (HttpContext httpContext) => {
    using var reader = new StreamReader(httpContext.Request.Body);
    var data = await reader.ReadToEndAsync();
    var results = "";
    
    var lastMinedBlock = host.chain.GetLastBlock(true);
    foreach (var block in host.chain.pendingBlocks.ToList()) {
        if (lastMinedBlock != null) {
            block.prevHash = lastMinedBlock.hash;
            block.index = lastMinedBlock.index + 1;
        } else {
            Console.WriteLine("No blocks in the chain");
        }
        Block minedBlock = block.MineBlock(host.publicKey);
        host.chain.AddBlock(minedBlock);
        lastMinedBlock = host.chain.GetLastBlock(true);
        host.chain.pendingBlocks.Remove(block);
        results += $"Block mined: {minedBlock.hash}\n";
    }
    if (results.Any()) {
        await host.SendChainToAll();
        return Results.Text(results);
    }
    else return Results.Text("No blocks to mine");
});


app.MapGet("/pendingBlocks", () => {
    var pendingBlocks = host.chain.pendingBlocks;
    return Results.Json(pendingBlocks);
});

app.UseStaticFiles(new StaticFileOptions {
    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "files")),
    RequestPath = "/files",
    ServeUnknownFileTypes = true, // This will serve files with unknown MIME types
    DefaultContentType = "application/octet-stream" // Default MIME type if none is provided
});


app.Run($"http://{publicIp}:{port}");
