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

  public BlockChain(List<Block> chain) {
    this.chain = chain; 
  }

  public int getChainLen() {
    return this.chain.Count();
  }

  public List<Block> GetBlocks() => chain;

  public Block? GetBlockByData(string data) => chain.Find(block => block.data.Contains(data));

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
    pendingBlocks.Add(block);
    return "Block added to pending blocks";
  }

  public bool IsValid() {
    for (int i = 1; i < chain.Count; i++) {
      var currentBlock = chain[i];
      var previousBlock = chain[i - 1];
      if (currentBlock.hash != CalculateHash(currentBlock) || currentBlock.prevHash != previousBlock.hash) {
        return false;
      }
    }
    return true;
  }

  private string CalculateHash(Block block) {
    using (var sha256 = SHA256.Create()) {
      var hashBytes = sha256.ComputeHash(
        System.Text.Encoding.UTF8.GetBytes($"{block.index}{block.prevHash}{block.timeStamp}{block.data}{block.nonce}{block.signature}{block.publicKey}")
      );
      return BitConverter.ToString(hashBytes).ToLower().Replace("-", "");
    }
  }

}