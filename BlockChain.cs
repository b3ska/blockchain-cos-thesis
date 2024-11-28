using System.Security.Cryptography;

class BlockChain  {
  private List<Block> chain;

  public List<Block> pendingBlocks;
  private int totalWork;

  public BlockChain() {
    chain = new List<Block> { Block.GenesisBlock() };
    totalWork = chain[0].nonce;
    pendingBlocks = new List<Block>();
  }

  // constructor for recieved chain from other nodes
  public BlockChain(List<Block> chain, List<Block> pendingBlocks) {
    totalWork = chain.Aggregate(0, (acc, block) => acc + block.nonce);
    this.chain = chain;
    this.pendingBlocks = pendingBlocks;
  }

  public int getChainLen() {
    return chain.Count();
  }

  public int getTotalWork() {
    return totalWork;
  }

  public List<Block> GetBlocks() => chain;

  public List<Block>? GetBlocksByData(string data) => chain.FindAll(block => block.data.Contains(data));

  public Block? GetBlockByHash(string hash) => chain.Find(block => block.hash == hash);

  public Block GetLastBlock(bool isMined) {
    if (pendingBlocks.Any() && !isMined) {
      return chain.Last().timeStamp > pendingBlocks.Last().timeStamp ? chain.Last() : pendingBlocks.Last();
    }
    return chain.Last();
  }

  public string AddBlock(Block block) {
    chain.Add(block);
    totalWork += block.nonce;
    return "Block created successfully";
  }

  public string AddPendingBlock(Block block) {
    if (pendingBlocks == null) {
      pendingBlocks = new List<Block> { block };
      return "Block added to pending blocks";
    }
    pendingBlocks.Add(block);
    return "Block added to pending blocks";
  }

  public void cleanMinedBlocks() {
    foreach (var block in pendingBlocks.ToList()) {
      foreach (var chainBlock in chain.ToList()) {
        if (block.timeStamp == chainBlock.timeStamp) {
          pendingBlocks.Remove(block);
        }
      }
    }
  }

  public bool ContainsBlock(Block block) {
    if (pendingBlocks.Count != 0) {
      foreach (Block b in pendingBlocks) {
        if (b.data == block.data && b.timeStamp == block.timeStamp) 
          return true;
        }
    }
    if (chain.Count != 0) {
      foreach (Block b in chain) {
        if (b.data == block.data && b.timeStamp == block.timeStamp) 
          return true;
        }
    }
    return false;
  }

  public static bool operator == (BlockChain a, BlockChain b) {
    return a.GetBlocks().SequenceEqual(b.GetBlocks()) ? true : false;
  }

  public static bool operator != (BlockChain a, BlockChain b) {
    return a.GetBlocks().SequenceEqual(b.GetBlocks()) ? false : true;
  }

  public bool IsValid() {
    for (int i = 1; i < chain.Count; i++) {
      var currentBlock = chain[i];
      var previousBlock = chain[i - 1];
      if (currentBlock.prevHash != previousBlock.hash) {
        return false;
      }
    }
    return true;
  }

}