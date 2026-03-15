using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TorrentHardLinkHelper.Locate;
using TorrentHardLinkHelper.Torrents;

namespace TorrentHardLinkHelper.Test;

[TestClass]
public class LocateTest
{
    /// <summary>
    ///     文件名字改动
    /// </summary>
    [TestMethod]
    public void TestLocate1()
    {
        var sourceFolder = @"I:\[BDMV][アニメ] ココロコネクト";
        var torrentPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestTorrents", "[U2].13680.torrent");

        var fileInfos = GetFileSystemInfos(sourceFolder);
        var torrent = Torrent.Load(torrentPath);
        var locater = new TorrentFileLocater(torrent, fileInfos);

        var result = locater.Locate();
    }

    [TestMethod]
    public void TestLocate2()
    {
        var sourceFolder = @"I:\[BDMV][アニメ] ココロコネクト";
        var torrentPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestTorrents", "[U2].13680.torrent");

        var fileInfos = GetFileSystemInfos(sourceFolder);
        var torrent = Torrent.Load(torrentPath);

        var locater = new TorrentFileLocater(torrent, fileInfos);
        var result = locater.Locate();

        Assert.IsNotNull(result);
    }

    private IList<FileSystemFileInfo> GetFileSystemInfos(string path)
    {
        var fileInfos =
            Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                .Select(file => new FileInfo(file))
                .Select(f => new FileSystemFileInfo(f))
                .ToList();
        return fileInfos;
    }
}