using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using Microsoft.VisualBasic;

class Node {
    public string Name { get; set; }
    public string publicKey { get; set; }
    public string privateKey { get; set; }
    public string adress { get; set; }
    public BlockChain Chain { get; set; }
    public List<Block> PendingBlocks { get; set; }
    public List<Node> Nodes { get; set; }
    public Node(string name, string keywords, string address) {
        var Sha256 = SHA256.Create();
        Name = name;
        publicKey = BitConverter.ToString(
                Sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes($"{keywords}{name}"))
            ).Replace("-", "");
        privateKey = BitConverter.ToString(
                Sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes($"{keywords}{name}"))
            ).Replace("-", "");
        adress = address;
        Chain = new BlockChain();
        PendingBlocks = new List<Block>();
        Nodes = new List<Node>();
    }

    public void ConnectNode(Node node) { // TODO: implement a tree structure for the nodes, higher nodes mined blocks are worth more
        Nodes.Add(node);
    }

    public void SyncChain() {   // TODO: implement validation of the chain, and only sync if the chain is valid
        foreach (var node in Nodes) {
            if (node.Chain.GetBlocks().Count > Chain.GetBlocks().Count) {
                Chain = node.Chain;
            }
        }
    }
    

}