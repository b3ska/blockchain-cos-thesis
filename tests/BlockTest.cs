using System;
using Xunit;

public class BlockTest
{
  [Fact]
  public void TestGenesisBlock()
  {
    var genesisBlock = Block.GenesisBlock();
    Assert.Equal(0, genesisBlock.index);
    Assert.Equal("prevHash", genesisBlock.prevHash);
    Assert.Equal(0, genesisBlock.timeStamp);
    Assert.Equal("Genesis Block", genesisBlock.data);
    Assert.Equal("hash", genesisBlock.hash);
  }

  [Fact]
  public void TestNewBlock()
  {
    var data = "Test Data";
    var newBlock = Block.NewBlock(data);
    Assert.Equal(0, newBlock.index);
    Assert.Equal("prevHash", newBlock.prevHash);
    Assert.Equal(data, newBlock.data);
    Assert.Equal("", newBlock.hash);
  }

  [Fact]
  public void TestMineBlock()
  {
    var data = "{\"fileName\":\"test.txt\",\"fileContent\":\"dGVzdCBjb250ZW50\",\"blockData\":\"Test Block Data\"}";
    var block = Block.NewBlock(data);
    block.difficulty = 1;
    var minedBlock = block.MineBlock("signature");

    Assert.StartsWith("00d", minedBlock.hash);
    Assert.Equal("file: test.txtTest Block Data", minedBlock.data);
    Assert.Equal("signature", minedBlock.signature);
  }
}