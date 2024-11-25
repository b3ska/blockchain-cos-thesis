using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

class Node
{
    public string publicKey { get; set; }
    public string? privateKey { get; set; }
    public IPAddress address { get; set; }
    public string status { get; set; } = "Disconnected";
    public BlockChain? chain { get; set; }
    public List<Node>? nodes { get; set; }
    public TcpClient client { get; set; } = new TcpClient();
    public TcpListener? listener { get; set; }

    public Node(string name, string keywords, IPAddress address)
    {
        var sha256 = SHA256.Create();
        privateKey = BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes($"{keywords}{name}"))).Replace("-", "").ToLower();
        publicKey = BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(privateKey))).Replace("-", "").ToLower();
        sha256.Dispose();
        this.address = address;
        status = "Host";
        chain = new BlockChain();
        nodes = new List<Node>();
        listener = new TcpListener(IPAddress.Any, 8080);
        listener.Start();
        _ = AcceptClientsAsync();
    }

    public Node(string publicKey, IPAddress address)
    {
        this.publicKey = publicKey;
        this.address = address;
    }

    public async Task ConnectNode(Node node)
    {
        try
        {
            if (nodes != null && nodes.Any(n => n.address.Equals(node.address)))
            {
                Console.WriteLine($"Already connected to node {node.address}");
                return;
            }
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Invalid operation: {ex.Message}");
            await Task.Delay(3000);
        }

        int retries = 3;
        while (retries > 0 && !node.client.Connected)
        {
            try
            {
                node.client.Connect(node.address, 8080);
                if (node.client.Connected)
                {
                    node.status = "Not verified";
                    nodes.Add(node);
                    await sendPublicKey(node);
                    _ = ListenForPublicKey(node);
                    Console.WriteLine($"connected and sent public key to {node.address}");
                    break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to {node.address}: {ex.Message}");
                node.status = "Disconnected";
                node.client.Close();
                retries--;
            }
        }
    }

    private async Task AcceptClientsAsync()
    {
        while (true)
        {
            try
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting clients: {ex.Message}");
                break;
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        try
        {
            NetworkStream stream = client.GetStream();
            var reader = new StreamReader(stream, Encoding.UTF8);

            IPEndPoint remoteIpEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
            var nodesByAddress = FindNodesByAddress(remoteIpEndPoint.Address);

            if (nodesByAddress == null || nodesByAddress.Count == 0 || nodesByAddress[0].status == "Not verified")
            {
                string message = await reader.ReadLineAsync();
                if (message != null && message.StartsWith("PUBLIC_KEY:"))
                {
                    string publicKey = message.Substring("PUBLIC_KEY:".Length);
                    Node node = new Node(publicKey, remoteIpEndPoint.Address);
                    node.client = client;
                    node.status = "Connected";
                    nodes.Add(node);

                    await sendPublicKey(node);
                    await SendChain(node);


                    Console.WriteLine($"{node.address} | {node.status} - added client");

                    _ = ListenForMessages(node);
                    _ = HeartbeatClient(node);
                }
                else
                {
                    Console.WriteLine("Unauthorized connection attempt TBI");
                    client.Close();
                }
            }
            else
            {
                // Node already connected
                Node node = nodesByAddress[0];
                node.client = client;
                node.status = "Connected";

                await sendPublicKey(node);
                await SendChain(node);

                Console.WriteLine($"{node.address} | {node.status} - updated client");
                _ = ListenForMessages(node);
                _ = HeartbeatClient(node);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling client: {ex.Message}");
        }
    }

    public bool IsConnectionValid(IPAddress address)
    {
        if (FindNodesByAddress(address).Count() > 3) return false;
        else return true;
    }

    public async Task sendPublicKey(Node node)
    {
        string message = $"PUBLIC_KEY:{publicKey}\n";
        byte[] data = Encoding.UTF8.GetBytes(message);
        NetworkStream stream = node.client.GetStream();
        await stream.WriteAsync(data, 0, data.Length);
        await stream.FlushAsync();
    }

    public async Task sendPendingToAll()
    {
        foreach (Node n in nodes)
        {
            await SendPending(n);
        }
    }

    public async Task SendPending(Node node)
    {
        try
        {
            var messageObject = new
            {
                PublicKey = publicKey,
                PendingBlocks = chain.pendingBlocks
            };
            string messageJson = JsonSerializer.Serialize(messageObject);
            string message = $"PENDING_UPDATE:{messageJson}\n";
            byte[] data = Encoding.UTF8.GetBytes(message);

            NetworkStream stream = node.client.GetStream();
            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();

            Console.WriteLine($"Sent pending blocks to {node.address}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send pending blocks to {node.address}: {ex.Message}");
        }
    }

    public async Task SendChainToAll()
    {
        foreach (Node n in nodes)
        {
            await SendChain(n);
        }
    }

    public async Task SendChain(Node node)
    {
        int retries = 3;
        while (retries > 0)
        {
            try
            {
                var messageObject = new
                {
                    PublicKey = publicKey,
                    Chain = chain.GetBlocks()
                };
                string messageJson = JsonSerializer.Serialize(messageObject);
                string message = $"CHAIN_UPDATE:{messageJson}\n";
                byte[] data = Encoding.UTF8.GetBytes(message);

                NetworkStream stream = node.client.GetStream();
                stream.WriteTimeout = 5000;
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();
                break;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send chain to {node.address}: {ex.Message}");
                retries--;
                await Task.Delay(5000);
            }
        }
    }

    public async Task HandlePendingUpdate(string serializedMessage)
    {
        var messageObject = JsonSerializer.Deserialize<PendingUpdateMessage>(serializedMessage);
        try
        {
            if (messageObject != null)
            {
                string senderPublicKey = messageObject.PublicKey;
                List<Block> receivedPendingBlocks = messageObject.PendingBlocks;

                Console.WriteLine($"Received pending update from node with public key: {senderPublicKey}");
                Node sendingNode = FindNodesByPublicKey(senderPublicKey)[0];

                if (sendingNode != null)
                {
                    List<Block> newPendingBlocks = new List<Block>();
                    foreach (Block block in receivedPendingBlocks)
                    {
                        if (chain.ContainsBlock(block))
                        {
                            continue;
                        }
                        else
                        {
                            newPendingBlocks.Add(block);
                        }
                    }
                    chain.pendingBlocks.AddRange(newPendingBlocks);
                    Console.WriteLine($"Updated pending blocks from node {senderPublicKey}");
                    chain.cleanMinedBlocks();

                }
                else
                {
                    Console.WriteLine($"Node with public key {senderPublicKey} not found. Not yet implemented.");
                }
            }
            else
            {
                Console.WriteLine("Failed to deserialize pending update message.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling pending update: {ex.Message}");
        }

    }

    private async Task HandleChainUpdate(string serializedMessage)
    {
        var messageObject = JsonSerializer.Deserialize<ChainUpdateMessage>(serializedMessage);
        try
        {
            if (messageObject != null)
            {
                string senderPublicKey = messageObject.PublicKey;
                List<Block> receivedChainBlocks = messageObject.Chain;

                Console.WriteLine($"Received chain update from node with public key: {senderPublicKey}");

                Node sendingNode = FindNodesByPublicKey(senderPublicKey)[0];

                if (sendingNode != null)
                {
                    BlockChain recievedChain = new BlockChain(receivedChainBlocks, chain.pendingBlocks);
                    if (recievedChain.IsValid())
                    {
                        if (recievedChain.getChainLen() > chain.getChainLen() || recievedChain.getTotalWork() > chain.getTotalWork())
                        {
                            chain = recievedChain;
                            chain.cleanMinedBlocks();
                            Console.WriteLine($"Updated blockchain");
                        }
                        else if (recievedChain.getChainLen() == chain.getChainLen() && recievedChain.getTotalWork() == chain.getTotalWork())
                        {
                            Console.WriteLine("Recieved chain is the same as the current one");
                        }
                        else
                        {
                            Console.WriteLine("Recieved chain is smaller or not valid, keeping previous one, sending valid to the node");
                            await SendChain(sendingNode);
                        }
                    }
                }
                else Console.WriteLine($"Node with public key {senderPublicKey} not found. Adding as a new node.");
            }
            else Console.WriteLine("Failed to deserialize chain update message.");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling chain update: {ex.Message} \n {ex.StackTrace}");
        }

    }

    private async Task HeartbeatClient(Node node)
    {

        var client = node.client;
        while (client.Connected)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] data = Encoding.UTF8.GetBytes("HEARTBEAT\n");
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();
                await Task.Delay(10000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending heartbeat to {node.address}: {ex.Message}");
                client.Close();
                nodes.Remove(node);
                Console.WriteLine($"Node {node.address} disconnected in heartbeat");
                break;
            }
        }
    }

    private async Task ListenForPublicKey(Node node)
    {
        var client = node.client;
        var stream = client.GetStream();
        var reader = new StreamReader(stream, Encoding.UTF8);
        try
        {
            while (true)
            {
                string message = await reader.ReadLineAsync() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(message))
                {
                    if (message.StartsWith("PUBLIC_KEY:"))
                    {
                        string publicKey = message.Substring("PUBLIC_KEY:".Length);
                        if (node.publicKey == publicKey)
                        {
                            node.status = "Connected";
                            Console.WriteLine($"Public key verified for {node.address}");
                        }
                        else Console.WriteLine($"Public key mismatch for {node.address}");
                        break;
                    }
                }
                else
                {
                    await Task.Delay(3000);
                }
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error handling client {node.address}: {ex.Message}");
            node.status = "Disconnected";
        }
        finally
        {
            _ = ListenForMessages(node);
            _ = HeartbeatClient(node);
        }
    }

    public async Task ListenForMessages(Node node)
    {
        var client = node.client;
        var stream = client.GetStream();
        var reader = new StreamReader(stream, Encoding.UTF8);
        try
        {
            while (true)
            {
                string message = await reader.ReadLineAsync() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(message))
                {
                    if (message.StartsWith("PENDING_UPDATE:"))
                    {
                        string serializedMessage = message.Substring("PENDING_UPDATE:".Length);
                        await HandlePendingUpdate(serializedMessage);
                    }
                    else if (message.StartsWith("CHAIN_UPDATE:"))
                    {
                        string serializedMessage = message.Substring("CHAIN_UPDATE:".Length);
                        await HandleChainUpdate(serializedMessage);
                    }
                    else if (message == "HEARTBEAT")
                    {
                        Console.WriteLine($"hb from {node.address}");
                    }
                }
                else
                {
                    await Task.Delay(3000);
                }
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error handling client {node.address}: {ex.Message}");
            node.status = "Disconnected";
        }
        finally
        {
            Console.WriteLine($"Node {node.address} listener finished");
            client.Close();
            nodes.Remove(node);
            Task.Delay(3000);
            ConnectNode(node);
        }
    }

    private List<Node> FindNodesByPublicKey(string publicKey)
    {
        return nodes.FindAll(node => node.publicKey == publicKey);
    }

    public List<Node> FindNodesByAddress(IPAddress address)
    {
        return nodes.Where(node => node.address.Equals(address)).ToList();
    }

    private class ChainUpdateMessage
    {
        public string PublicKey { get; set; }
        public List<Block> Chain { get; set; }
    }

    private class PendingUpdateMessage
    {
        public string PublicKey { get; set; }
        public List<Block> PendingBlocks { get; set; }
    }
}
