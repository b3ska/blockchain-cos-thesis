using System.Security.Cryptography;

class Block {
  public int index { get; set; }
  public string prevHash { get; set; }
  public long timeStamp { get; set; }
  public string data { get; set; }
  public string hash { get; set; }

  Block (int index, string previousHash, long timestamp, string data, string hash) {
    this.index = index;
    prevHash = previousHash;
    timeStamp = timestamp;
    this.data = data;
    this.hash = hash;
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
    block.hash = block.CalculateHash ();
    return block;
  }

  public string CalculateHash () {
      var sha256 = SHA256.Create();
      var hashBytes = sha256.ComputeHash (
          System.Text.Encoding.UTF8.GetBytes (
              $"{index}{prevHash}{timeStamp}{data}"
          )
      );
      return BitConverter.ToString(hashBytes);
  }
}