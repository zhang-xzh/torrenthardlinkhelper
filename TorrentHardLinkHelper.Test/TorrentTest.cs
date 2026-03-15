using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TorrentHardLinkHelper.Torrents;

namespace TorrentHardLinkHelper.Test;

/// <summary>
///     Summary description for TorrentTest
/// </summary>
[TestClass]
public class TorrentTest
{
    /// <summary>
    ///     Gets or sets the test context which provides
    ///     information about and functionality for the current test run.
    /// </summary>
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void TestLoadTorrent()
    {
        var torrentFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestTorrents", "[U2].14332.torrent");
        var torrent = Torrent.Load(torrentFile);

        Console.WriteLine("{0,15}: {1}", "Name", torrent.Name);
        Console.WriteLine("{0,15}: {1}", "IsPrivate", torrent.IsPrivate);
        Console.WriteLine("{0,15}: {1}", "Pieces", torrent.Pieces.Count);
        Console.WriteLine("{0,15}: {1}", "PieceLength", torrent.PieceLength);
        Console.WriteLine("{0,15}: {1}", "Files", torrent.Files.Count());
        Console.WriteLine("----------Files------------------");
        foreach (var file in torrent.Files)
        {
            Console.WriteLine("{0,15}: {1}", "Path", file.Path);
            Console.WriteLine("{0,15}: {1}", "Length", file.Length);
            Console.WriteLine("{0,15}: {1}", "StartPieceIndex", file.StartPieceIndex);
            Console.WriteLine("{0,15}: {1}", "EndPieceIndex", file.EndPieceIndex);
            Console.WriteLine("{0,15}: {1}", "EndPieceIndex", file.Priority);

            Console.WriteLine("-----------------------------------------");
        }
    }
}