using System;
using System.Collections.Generic;
using System.Linq;
using TorrentHardLinkHelper.Torrents;

namespace TorrentHardLinkHelper.Locate;

public class TorrentFileLocater
{
    private readonly Action _fileLocating;
    private readonly IList<FileSystemFileInfo> _fsfiFileInfos;
    private readonly Dictionary<int, bool> _pieceCheckedReusltsDictionary;
    private readonly Torrent _torrent;
    private readonly IList<TorrentFileLink> _torrentFileLinks;

    public TorrentFileLocater()
    {
        _torrentFileLinks = new List<TorrentFileLink>();
    }

    public TorrentFileLocater(Torrent torrent)
        : this()
    {
        _torrent = torrent;
        _pieceCheckedReusltsDictionary = new Dictionary<int, bool>(_torrent.Pieces.Count);
    }

    public TorrentFileLocater(Torrent torrent, IList<FileSystemFileInfo> fsfiFileInfos)
        : this(torrent)
    {
        _fsfiFileInfos = fsfiFileInfos;
    }

    public TorrentFileLocater(Torrent torrent, IList<FileSystemFileInfo> fsfiFileInfos, Action fileLocating)
        : this(torrent, fsfiFileInfos)
    {
        _fileLocating = fileLocating;
    }


    public LocateResult Locate()
    {
        if (_torrent == null) throw new ArgumentException("Torrent cannot be null.");
        if (_fsfiFileInfos == null || _fsfiFileInfos.Count == 0) throw new ArgumentException("FsFileInfos cannot be null or zero");

        FindTorrentFileLinks();
        ConfirmFileSystemFiles();
        return new LocateResult(_torrentFileLinks);
    }

    private void FindTorrentFileLinks()
    {
        foreach (var torrentFile in _torrent.Files)
        {
            var fileLink = new TorrentFileLink(torrentFile);
            foreach (var fsFileInfo in _fsfiFileInfos)
                if (fsFileInfo.Length == torrentFile.Length)
                    fileLink.FsFileInfos.Add(fsFileInfo);

            if (fileLink.FsFileInfos.Count > 1)
            {
                var torrentFilePathParts = torrentFile.Path.Split('\\').ToList();
                torrentFilePathParts.Insert(0, _torrent.Name);
                for (var i = 0; i < torrentFilePathParts.Count; i++)
                {
                    var links = new List<FileSystemFileInfo>();
                    foreach (var fileInfo in fileLink.FsFileInfos)
                    {
                        var filePathPaths = fileInfo.FilePath.Split('\\');
                        if (filePathPaths.Length > i + 1 &&
                            filePathPaths[filePathPaths.Length - i - 1].ToUpperInvariant() ==
                            torrentFilePathParts[torrentFilePathParts.Count - i - 1].ToUpperInvariant())
                            links.Add(fileInfo);
                    }

                    if (links.Count == 0) break;
                    if (links.Count >= 1)
                    {
                        fileLink.FsFileInfos = links;
                        if (links.Count == 1) break;
                    }
                }
            }

            if (fileLink.FsFileInfos.Count == 1)
            {
                fileLink.State = LinkState.Located;
                fileLink.LinkedFsFileIndex = 0;
            }
            else if (fileLink.FsFileInfos.Count > 1)
            {
                fileLink.State = LinkState.NeedConfirm;
            }
            else
            {
                fileLink.State = LinkState.Fail;
            }

            _torrentFileLinks.Add(fileLink);
        }
    }

    private void ConfirmFileSystemFiles()
    {
        foreach (var fileLink in _torrentFileLinks)
        {
            if (_fileLocating != null) _fileLocating.Invoke();
            if (fileLink.State == LinkState.Located) continue;
            if (fileLink.State == LinkState.Fail)
            {
                if (fileLink.TorrentFile.EndPieceIndex - fileLink.TorrentFile.StartPieceIndex > 2)
                    fileLink.State = CheckPiece(fileLink.TorrentFile.StartPieceIndex + 1)
                        ? LinkState.Located
                        : LinkState.Fail;
                continue;
            }

            for (var i = fileLink.TorrentFile.StartPieceIndex; i <= fileLink.TorrentFile.EndPieceIndex; i++)
            {
                if (!CheckPiece(i)) break;
                if (fileLink.State == LinkState.Located) break;
            }
        }
    }

    private bool CheckPiece(int pieceIndex)
    {
        bool result;
        if (_pieceCheckedReusltsDictionary.TryGetValue(pieceIndex, out result)) return result;
        var startPos = (ulong)pieceIndex * (ulong)_torrent.PieceLength;
        ulong pos = 0;
        ulong writenLength = 0;
        var filePieces = new List<FileLinkPiece>();
        foreach (var fileLink in _torrentFileLinks)
        {
            if (pos + (ulong)fileLink.TorrentFile.Length >= startPos)
            {
                var readPos = startPos - pos;
                var readLength = (ulong)fileLink.TorrentFile.Length - readPos;
                if (writenLength + readLength > (ulong)_torrent.PieceLength) readLength = (ulong)_torrent.PieceLength - writenLength;

                var filePiece = new FileLinkPiece
                {
                    FileLink = fileLink,
                    ReadLength = readLength,
                    StartPos = readPos
                };
                filePieces.Add(filePiece);

                writenLength += readLength;
                startPos += readLength;
                if (writenLength == (ulong)_torrent.PieceLength) break;
            }

            pos += (ulong)fileLink.TorrentFile.Length;
        }

        var hash = new HashFileLinkPieces(_torrent, pieceIndex, filePieces);
        var pattern = hash.Run();
        if (string.IsNullOrEmpty(pattern))
        {
            foreach (var piece in filePieces)
                if (piece.FileLink.State == LinkState.NeedConfirm)
                    piece.FileLink.State = LinkState.Fail;

            _pieceCheckedReusltsDictionary.Add(pieceIndex, false);
            return false;
        }

        for (var i = 0; i < filePieces.Count; i++)
            if (filePieces[i].FileLink.State == LinkState.NeedConfirm)
            {
                filePieces[i].FileLink.LinkedFsFileIndex = int.Parse(pattern.Split(',')[i]);
                filePieces[i].FileLink.LinkedFsFileInfo.Located = true;
                filePieces[i].FileLink.State = LinkState.Located;
            }

        _pieceCheckedReusltsDictionary.Add(pieceIndex, true);
        return true;
    }
}