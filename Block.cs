using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

struct dataTypes {
}

class Block {
  public int index { get; set; }
  public string prevHash { get; set; }
  public long timeStamp { get; set; }
  public string data { get; set; }
  public string hash { get; set; }
  public string signature { get; set; } = "";
  public string publicKey { get; set; } = "";
  public int difficulty { get; set; } = 0;
  public int nonce { get; set; } = 0;

  Block(int index, string previousHash, long timestamp, string data, string hash) {
    this.index = index;
    prevHash = previousHash;
    timeStamp = timestamp;
    this.data = data;
    this.hash = hash;
  }

  public static Block GenesisBlock() {
    return new Block(0, "prevHash", 0, "Genesis Block", "hash");
  }

  public Block() {
    
  }

  public static Block NewBlock(string data) {
    var block = new Block(
      0,
      "prevHash",
      long.Parse(DateTime.UtcNow.ToString("yyyyMMddHHmmss")),
      data,
      ""
    );
    return block;
  }

public Block MineBlock(string signature) {
    var sha256 = SHA256.Create();

    do {
        nonce++;
        var hashBytes = sha256.ComputeHash(
            Encoding.UTF8.GetBytes(
                $"{index}{prevHash}{timeStamp}{data}{nonce}{signature}"
            )
        );
        hash = BitConverter.ToString(hashBytes).ToLower().Replace("-", "");
    } while (!hash.StartsWith("00d" + new string('0', difficulty)));

    var jsonData = JsonSerializer.Deserialize<Dictionary<string, string>>(data);
    data = "";

    if (jsonData != null && jsonData.ContainsKey("fileContent")) {
        var fileName = jsonData["fileName"];
        var fileContent = jsonData["fileContent"];

        var directory = "files/";
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
    
        var decodedBytes = Convert.FromBase64String(fileContent);
        File.WriteAllBytes(directory + fileName, decodedBytes);
        data = "file: " + fileName;
    }

    data += jsonData != null && jsonData.ContainsKey("blockData") ? jsonData["blockData"] : "";
    sha256.Dispose();
    this.signature = signature;

    Console.WriteLine($"Block mined: {hash}");
    return this;
  }

}

