using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using TorrentHardLinkHelper.Torrents;

namespace TorrentHardLinkHelper.Locate;

internal class HashFileLinkPieces
{
    private readonly IList<FileLinkPiece> _fileLinkPieces;
    private readonly int _pieceIndex;
    private readonly Torrent _torrent;
    private IDictionary<int, IList<FileSystemFileInfo>> _cleanedFileInfos;

    private int _result;
    private string _validPattern;

    public HashFileLinkPieces(Torrent torrent, int pieceIndex,
        IList<FileLinkPiece> fileLinkPieces)
    {
        _pieceIndex = pieceIndex;
        _fileLinkPieces = fileLinkPieces;
        _result = -1;
        _torrent = torrent;
    }

    public string Run()
    {
        if (_fileLinkPieces.Any(c => c.FileLink.FsFileInfos.Count == 0)) return null;
        CleanDuplicateFiles();
        Run(0, "");
        return _validPattern;
    }

    private void CleanDuplicateFiles()
    {
        _cleanedFileInfos = new Dictionary<int, IList<FileSystemFileInfo>>();
        for (var i = 0; i < _fileLinkPieces.Count; i++)
        {
            var piece = _fileLinkPieces[i];
            var hashes = new List<string>();
            _cleanedFileInfos.Add(i, new List<FileSystemFileInfo>(piece.FileLink.FsFileInfos.Count));

            foreach (var fileInfo in piece.FileLink.FsFileInfos)
            {
                if (piece.FileLink.TorrentFile.StartPieceIndex == _pieceIndex &&
                    piece.FileLink.TorrentFile.EndPieceIndex == _pieceIndex)
                    if (fileInfo.Located)
                        continue;

                var hash = HashFilePiece(fileInfo.FilePath,
                    piece.StartPos, piece.ReadLength);
                if (hashes.Contains(hash)) continue;
                hashes.Add(hash);
                _cleanedFileInfos[i].Add(fileInfo);
            }
        }
    }

    private void GroupFiles()
    {
    }

    private void Run(int index, string pattern)
    {
        if (_result == 1) return;
        if (index < _fileLinkPieces.Count)
            for (var i = 0; i < _cleanedFileInfos[index].Count; i++)
            {
                var nextPattern = pattern + i + ',';
                var nextIndex = index + 1;
                Run(nextIndex, nextPattern);
            }
        else
            using (var pieceStream = new MemoryStream(_torrent.PieceLength))
            {
                for (var i = 0; i < _fileLinkPieces.Count; i++)
                {
                    var fileIndex = int.Parse(pattern.Split(',')[i]);
                    using (var fileStream =
                           File.OpenRead(_cleanedFileInfos[i][fileIndex].FilePath))
                    {
                        var buffer = new byte[_fileLinkPieces[i].ReadLength];
                        fileStream.Position = (long)_fileLinkPieces[i].StartPos;
                        fileStream.Read(buffer, 0, buffer.Length);
                        pieceStream.Write(buffer, 0, buffer.Length);
                    }
                }

                var sha1 = HashAlgoFactory.Create<SHA1>();
                var hash = sha1.ComputeHash(pieceStream.ToArray());
                if (_torrent.Pieces.IsValid(hash, _pieceIndex))
                {
                    _validPattern = pattern;
                    _result = 1;
                }
            }
    }

    private string HashFilePiece(string path, ulong start, ulong length)
    {
        var sha1 = HashAlgoFactory.Create<SHA1>();
        var fileStream =
            File.OpenRead(path);
        var buffer = new byte[length];
        fileStream.Position = (long)start;
        fileStream.Read(buffer, 0, buffer.Length);
        var hash = sha1.ComputeHash(buffer);
        fileStream.Close();
        return Convert.ToBase64String(hash);
    }

    private class CheckResult
    {
        public string Pattern { get; set; }
        public bool Matched { get; set; }
    }

    private delegate CheckResult CheckPatternFunc(string pattern);
}