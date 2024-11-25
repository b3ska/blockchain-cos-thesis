using System;
using System.Collections.Generic;
using Xunit;

public class BlockChainTest
{
  [Fact]
  public void TestBlockChainInitialization()
  {
    BlockChain blockChain = new BlockChain();
    Assert.Equal(1, blockChain.getChainLen());
    Assert.Equal(0, blockChain.getTotalWork());
  }

  [Fact]
  public void TestAddBlock()
  {
    BlockChain blockChain = new BlockChain();
    Block block = new Block { data = "Test Data", nonce = 1, hash = "hash1", prevHash = "prevHash1", timeStamp = long.Parse(DateTime.UtcNow.ToString("yyyyMMddHHmmss")) };
    string result = blockChain.AddBlock(block);
    Assert.Equal("Block created successfully", result);
    Assert.Equal(2, blockChain.getChainLen());
    Assert.Equal(1, blockChain.getTotalWork());
  }

  [Fact]
  public void TestAddPendingBlock()
  {
    BlockChain blockChain = new BlockChain();
    Block block = new Block { data = "Pending Data", nonce = 1, hash = "hash2", prevHash = "prevHash2", timeStamp = long.Parse(DateTime.UtcNow.ToString("yyyyMMddHHmmss")) };
    string result = blockChain.AddPendingBlock(block);
    Assert.Equal("Block added to pending blocks", result);
    Assert.Single(blockChain.pendingBlocks);
  }

  [Fact]
  public void TestGetBlockByData()
  {
    BlockChain blockChain = new BlockChain();
    Block block = new Block { data = "Test Data", nonce = 1, hash = "hash1", prevHash = "prevHash1", timeStamp = long.Parse(DateTime.UtcNow.ToString("yyyyMMddHHmmss")) };
    blockChain.AddBlock(block);
    Block? result = blockChain.GetBlockByData("Test Data");
    Assert.NotNull(result);
    Assert.Equal("Test Data", result.data);
  }

  [Fact]
  public void TestGetBlockByHash()
  {
    BlockChain blockChain = new BlockChain();
    Block block = new Block { data = "Test Data", nonce = 1, hash = "hash1", prevHash = "prevHash1", timeStamp = long.Parse(DateTime.UtcNow.ToString("yyyyMMddHHmmss")) };
    blockChain.AddBlock(block);
    Block? result = blockChain.GetBlockByHash("hash1");
    Assert.NotNull(result);
    Assert.Equal("hash1", result.hash);
  }

  [Fact]
  public void TestContainsBlock()
  {
    BlockChain blockChain = new BlockChain();
    Block block = new Block { data = "Test Data", nonce = 1, hash = "hash1", prevHash = "prevHash1", timeStamp = long.Parse(DateTime.UtcNow.ToString("yyyyMMddHHmmss")) };
    blockChain.AddBlock(block);
    bool result = blockChain.ContainsBlock(block);
    Assert.True(result);
  }

  [Fact]
  public void TestIsValid()
  {
    BlockChain blockChain = new BlockChain();
    Block block = new Block { data = "Test Data", nonce = 1, hash = "hash1", prevHash = blockChain.GetBlocks().Last().hash, timeStamp = long.Parse(DateTime.UtcNow.ToString("yyyyMMddHHmmss")) };
    blockChain.AddBlock(block);
    bool result = blockChain.IsValid();
    Assert.True(result);
  }

  [Fact]
  public void TestEqualityOperator()
  {
    BlockChain blockChain1 = new BlockChain();
    BlockChain blockChain2 = new BlockChain();
    Assert.True(blockChain1 == blockChain2);
  }

  [Fact]
  public void TestInequalityOperator()
  {
    BlockChain blockChain1 = new BlockChain();
    Block block = new Block { data = "Test Data", nonce = 1, hash = "hash1", prevHash = "prevHash1", timeStamp = long.Parse(DateTime.UtcNow.ToString("yyyyMMddHHmmss")) };
    blockChain1.AddBlock(block);
    BlockChain blockChain2 = new BlockChain();
    Assert.True(blockChain1 != blockChain2);
  }

  [Fact]
  public void TestCleanMinedBlocks()
  {
    BlockChain blockChain = new BlockChain();
    Block block = new Block { data = "Test Data", nonce = 1, hash = "hash1", prevHash = "prevHash1", timeStamp = long.Parse(DateTime.UtcNow.ToString("yyyyMMddHHmmss")) };
    blockChain.AddPendingBlock(block);
    blockChain.AddBlock(block);
    blockChain.cleanMinedBlocks();
    Assert.Empty(blockChain.pendingBlocks);
  }

  [Fact]
  public void TestGetLastBlock()
  {
    BlockChain blockChain = new BlockChain();
    Block block = new Block { data = "Test Data", nonce = 1, hash = "hash1", prevHash = "prevHash1", timeStamp = long.Parse(DateTime.UtcNow.ToString("yyyyMMddHHmmss")) };
    blockChain.AddPendingBlock(block);
    Block lastBlock = blockChain.GetLastBlock(false);
    Assert.Equal(block, lastBlock);
  }
}
