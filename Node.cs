using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

class Node
{
    public string publicKey { get; set; }
    public string privateKey { get; set; }
    public IPAddress address { get; set; }
    public string status { get; set; }
    public BlockChain chain { get; set; }
    public List<Node> nodes {get; set;}

    public Node(string name, string keywords, IPAddress address) {
        var sha256 = SHA256.Create();
        privateKey = BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes($"{keywords}{name}"))).Replace("-", "").ToLower();
        publicKey = BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(privateKey))).Replace("-", "").ToLower();
        this.address = address;
        _ = RecievePublicKey();
        chain = new BlockChain();

    }

    public Node (string publicKey, IPAddress address) {
        this.publicKey = publicKey;
        this.address = address;
    }

    public async Task ConnectNode(Node node) {
        int retries = 3;
        while (retries > 0) {
            using TcpClient client = new();
            try {
                await client.ConnectAsync(node.address, 8080);
                status = "Connected";
                nodes.Add(node);

                Console.WriteLine($"{node.address} | {status}");
                _ = SendChain(node.address.ToString());
                break;
            }
            catch (Exception ex) {
                Console.WriteLine($"Failed to connect to {address}: {ex.Message}");
                status = "Disconnected";
                nodes.Remove(node);
                retries--;
                await Task.Delay(2000);
            }
        }
    }

    public async Task sendPublicKey(IPAddress recipient) {
        using TcpClient client = new();
        await client.ConnectAsync(recipient, 8080);
        NetworkStream stream = client.GetStream();
        byte[] data = Encoding.UTF8.GetBytes($"PUBLIC_KEY:{publicKey}");
        await stream.WriteAsync(data, 0, data.Length);
    }

    public async Task RecievePublicKey() {
        TcpListener listener = new TcpListener(IPAddress.Any, 8080);
        listener.Start();
        Console.WriteLine("Node is listening for incoming public keys...");

        while (true) {
            TcpClient client = await listener.AcceptTcpClientAsync();
                using NetworkStream stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                string message = await reader.ReadToEndAsync();

                if (message.StartsWith("PUBLIC_KEY:")) {
                    string serializedMessage = message.Substring("PUBLIC_KEY:".Length);
                    if(FindNodeByPublicKey(serializedMessage) == null) {
                        var remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                        if (remoteEndPoint != null) {
                            Node newNode = new Node(serializedMessage, remoteEndPoint.Address);
                            nodes.Add(newNode);
                            await sendPublicKey(newNode.address);
                        }
                        else Console.WriteLine("Failed to get remote endpoint address.");
                    }
                }
        }
    }

    public async Task SendChainToAll() {
        foreach (Node n in nodes) {
            await SendChain(n.address.ToString());
        }
    }

    public async Task SendChain(string nodeIp) {
        int retries = 3;
        while (retries > 0) {
            using TcpClient client = new();
            try {
                await client.ConnectAsync(nodeIp, 8080);
                Console.WriteLine($"Connected to {nodeIp}");

                var messageObject = new {
                    PublicKey = publicKey,
                    Chain = chain.GetBlocks()
                };
                string messageJson = JsonSerializer.Serialize(messageObject);
                byte[] data = Encoding.UTF8.GetBytes($"CHAIN_UPDATE:{messageJson}");
                NetworkStream stream = client.GetStream();
                await stream.WriteAsync(data, 0, data.Length);
                break;
            }
            catch (Exception ex) {
                Console.WriteLine($"Failed to connect to {nodeIp}: {ex.Message}");
                retries--;
                await Task.Delay(2000);
            }
        }
    }


    public async Task ReceiveChain() {
        TcpListener listener = new TcpListener(IPAddress.Any, 8080);
        listener.Start();
        Console.WriteLine("Node is listening for incoming chain updates...");

        while (true) {
            TcpClient client = await listener.AcceptTcpClientAsync();
            try {
                using NetworkStream stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                string message = await reader.ReadToEndAsync();

                if (message.StartsWith("CHAIN_UPDATE:")) {
                    string serializedMessage = message.Substring("CHAIN_UPDATE:".Length);
                    HandleChainUpdate(serializedMessage);
                }
                else Console.WriteLine("Received unknown message format.");

            }   
            catch (Exception ex) {Console.WriteLine($"Error handling client: {ex.Message}");}
            finally {client.Close();}
            }
    }

    private async void HandleChainUpdate(string serializedMessage)
    {
        var messageObject = JsonSerializer.Deserialize<ChainUpdateMessage>(serializedMessage);

        if (messageObject != null) {
            string senderPublicKey = messageObject.PublicKey;
            List<Block> receivedChainBlocks = messageObject.Chain;

            Console.WriteLine($"Received chain update from node with public key: {senderPublicKey}");

            Node sendingNode = FindNodeByPublicKey(senderPublicKey);

            if (sendingNode != null) {
                BlockChain recievedChain = new BlockChain(receivedChainBlocks);
                if (recievedChain.getChainLen() > chain.getChainLen() && recievedChain.IsValid()) {
                    chain = recievedChain;

                    Console.WriteLine($"Updated blockchain of node {senderPublicKey}");
                }
                else {
                    Console.WriteLine("Recieved chain is smaller or not valid, keeping previous one, sending valid to the node");
                    await SendChain(sendingNode.address.ToString());
                }
            }
            else {
                Console.WriteLine($"Node with public key {senderPublicKey} not found. Adding as a new node.");
                
                Node newNode = new Node(senderPublicKey, sendingNode.address);
                newNode.chain = new BlockChain(receivedChainBlocks);

                nodes.Add(newNode);
            }
        }
        else {
            Console.WriteLine("Failed to deserialize chain update message.");
        }
    }

    private Node FindNodeByPublicKey(string publicKey) {
        return nodes.Find(node => node.publicKey == publicKey);
    }

    private class ChainUpdateMessage {
        public string PublicKey { get; set; }
        public List<Block> Chain { get; set; }
        public List<Block> pendingBlocks { get; set; }
    }
}
