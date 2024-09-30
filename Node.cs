using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using Microsoft.VisualBasic;

class Node {
    public string Name { get; set; }
    public string publicKey { get; set; }
    public string privateKey { get; set; }
    public string address { get; set; }
    public string Status { get; set; } // e.g., "Connected", "Disconnected", etc.

    public BlockChain Chain { get; set; }
    public List<Block> PendingBlocks { get; set; }
    public Node ParentNode { get; set; }
    public List<Node> ChildNodes { get; set; }

    public Node(string name, string keywords, string address) {
        var Sha256 = SHA256.Create();
        Name = name;
        publicKey = BitConverter.ToString(
            Sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes($"{keywords}{name}"))
        ).Replace("-", "");
        privateKey = BitConverter.ToString(
            Sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes($"{keywords}{name}"))
        ).Replace("-", "");
        this.address = address;
        Chain = new BlockChain();
        PendingBlocks = new List<Block>();
        ChildNodes = new List<Node>();
        ParentNode = null;
    }

    // Count how many blocks this node has mined based on the block's signature (public key)
    public int GetMinedBlockCount() {
        return Chain.GetBlocks().Count(block => block.signature == this.publicKey);
    }

    // Adds a new node either as a peer or a child depending on its mined block count
    public void ConnectNode(Node node) {
        int currentNodeMinedCount = GetMinedBlockCount();
        int newNodeMinedCount = node.GetMinedBlockCount();

        if (newNodeMinedCount > currentNodeMinedCount) {
            // New node has mined more blocks, it becomes a parent
            node.ChildNodes.Add(this);  // Current node becomes a child of the new node
            if (this.ParentNode != null) {
                // Reassign the parent if this node had one
                this.ParentNode.ChildNodes.Remove(this);
                this.ParentNode = node;
            }
        } else if (newNodeMinedCount == currentNodeMinedCount) {
            // New node has mined the same amount of blocks, make it a peer
            if (this.ParentNode != null) {
                this.ParentNode.ChildNodes.Add(node);  // Both nodes share the same parent
                node.ParentNode = this.ParentNode;
            } else {
                // Special case: if there is no parent, treat this as a root peer node
                this.ChildNodes.Add(node);
                node.ParentNode = this;
            }
        } else {
            // New node has mined fewer blocks, it becomes a child of the current node
            ChildNodes.Add(node);
            node.ParentNode = this;
        }
    }

    // Sync chain with the highest node in the tree
public void SyncChain() {
    // Start syncing with the highest node in the hierarchy
    Node highestNode = GetHighestNodeInHierarchy();
    
    // Check if the highest node's chain has more blocks than the current node's chain
    if (highestNode != null && highestNode.Chain.GetBlocks().Count > Chain.GetBlocks().Count) {
        Chain = highestNode.Chain;  // Sync to the highest chain found in the hierarchy
    }
}

// Helper method to traverse the hierarchy and find the highest node in the tree structure
private Node GetHighestNodeInHierarchy() {
    // We start from the current node and will compare it with its parent and peers
    Node highestNode = this;
    
    // Traverse up through the parent hierarchy
    Node currentNode = this.ParentNode;
    while (currentNode != null) {
        // If the parent node has more blocks mined, it becomes the new highest node
        if (currentNode.Chain.GetBlocks().Count > highestNode.Chain.GetBlocks().Count) {
            highestNode = currentNode;
        }
        
        // Compare the parent node's peers as well (nodes at the same level)
        foreach (var peer in currentNode.ChildNodes) {
            if (peer != this && peer.Chain.GetBlocks().Count > highestNode.Chain.GetBlocks().Count) {
                highestNode = peer;
            }
        }
        
        // Move up the hierarchy to the parent node
        currentNode = currentNode.ParentNode;
    }

    return highestNode;  // Return the highest node found
}

    // Recursive function to get the highest node in the tree
    private Node GetHighestNode() {
        Node highest = this;
        foreach (var child in ChildNodes) {
            var highestChild = child.GetHighestNode();
            if (highestChild.GetMinedBlockCount() > highest.GetMinedBlockCount()) {
                highest = highestChild;
            }
        }
        return highest;
    }
}
