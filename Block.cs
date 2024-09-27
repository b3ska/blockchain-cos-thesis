using System.Security.Cryptography;

class Block {
  public int index { get; set; }
  public string prevHash { get; set; }
  public long timeStamp { get; set; }
  public string data { get; set; }
  public string hash { get; set; }
  public string signature { get; set; }
  public string publicKey { get; set; }
  public int nonce { get; set; }

  Block (int index, string previousHash, long timestamp, 
  string data, string hash) {
    this.index = index;
    prevHash = previousHash;
    timeStamp = timestamp;
    this.data = data;
    this.hash = hash;
    this.signature = "";
    this.publicKey = "";
  }

  public static Block GenesisBlock () {
    return new Block (0, "0", 0, "Genesis Block", "");
  }

  public static Block NewBlock (Block previousBlock, string data) {
    var block = new Block (
      previousBlock.index + 1,
      previousBlock.hash,
      DateTimeOffset.UtcNow.ToUnixTimeSeconds (),
      data,
      ""
    );
    return block;
  }

    public string MineBlock (string signature, string publicKey) {
    var sha256 = SHA256.Create();

    do {
      nonce++;
      var hashBytes = sha256.ComputeHash (
        System.Text.Encoding.UTF8.GetBytes (
          $"{index}{prevHash}{timeStamp}{data}{nonce}{signature}{publicKey}"
        )       
      );
      hash = BitConverter.ToString(hashBytes).ToLower().Replace("-", "");
    }
    while (!hash.StartsWith("00d0"));

    sha256.Dispose();
    this.signature = signature;
    this.publicKey = publicKey;
    Console.WriteLine($"Block mined: {hash}");
    return hash;
  }
}
