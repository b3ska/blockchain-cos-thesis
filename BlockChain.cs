class BlockChain {
  List<Block> chain;

  public BlockChain () {
    chain = [Block.GenesisBlock ()];
  }

  public List<Block> GetBlocks () {
    return chain;
  }

  public Block? GetBlockByData (string data) {
      return chain.Find (block => block.data == data);
  }

  public Block? GetBlockByHash (string hash) {
      return chain.Find (block => block.hash == hash);
  }

  public string AddBlock (string data) {
    chain.Add (Block.NewBlock (chain.Last (), data)); 
    return "Block created successfully";
  }

  public bool IsValid () {
    for (int i = 1; i < chain.Count; i++) {
      var currentBlock = chain[i];
      var previousBlock = chain[i - 1];

      if (currentBlock.hash != currentBlock.CalculateHash ()) {
        return false;
      }

      if (currentBlock.prevHash != previousBlock.hash) {
        return false;
      }
    }

    return true;
  
  }
}