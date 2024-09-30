using System.Security.Cryptography;

class BlockChain  {
  private List<Block> chain;

  public BlockChain () {
    chain = [Block.GenesisBlock ()];
  }

  public List<Block> GetBlocks () {
    return chain;
  }

  public Block? GetBlockByData (string data) {
      chain.Last().data.Contains(data);
      return chain.Find (block => block.data.Contains (data));
  }

  public Block? GetBlockByHash (string hash) {
      return chain.Find (block => block.hash == hash);
  }

  public string AddBlock (string data) {
    chain.Add (Block.NewBlock (chain.Last (), data)); 
    return "Block created successfully";
  }

  public Block GetLastBlock () {
    return chain.Last ();
  }

  public bool IsValid() {
    for (int i = 1; i < chain.Count; i++) {
      var currentBlock = chain[i];
      var previousBlock = chain[i - 1];

      if (currentBlock.hash != CalculateHash(currentBlock)) {
        return false;
      }

      if (currentBlock.prevHash != previousBlock.hash) {
        return false;
      }
    }
    return true;
  }

  private string CalculateHash(Block block) {
    using (var sha256 = SHA256.Create()) {
      var hashBytes = sha256.ComputeHash(
        System.Text.Encoding.UTF8.GetBytes(
          $"{block.index}{block.prevHash}{block.timeStamp}{block.data}{block.nonce}{block.signature}{block.publicKey}"
        )
      );
      return BitConverter.ToString(hashBytes).ToLower().Replace("-", "");
    }
  }
}