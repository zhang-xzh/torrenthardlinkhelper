using System.Collections.Generic;
using TorrentHardLinkHelper.Torrents;

namespace TorrentHardLinkHelper.Locate;

public class TorrentFileLink
{
    public TorrentFileLink()
    {
        FsFileInfos = new List<FileSystemFileInfo>();
        LinkedFsFileIndex = -1;
        State = LinkState.None;
    }

    public TorrentFileLink(TorrentFile torrentFile)
        : this()
    {
        TorrentFile = torrentFile;
    }

    public TorrentFile TorrentFile { get; internal set; }

    public IList<FileSystemFileInfo> FsFileInfos { get; internal set; }

    public int LinkedFsFileIndex { get; internal set; }

    public FileSystemFileInfo LinkedFsFileInfo
    {
        get
        {
            if (LinkedFsFileIndex < 0 || LinkedFsFileIndex >= FsFileInfos.Count) return null;
            return FsFileInfos[LinkedFsFileIndex];
        }
    }

    public LinkState State { get; internal set; }

    public override string ToString()
    {
        return TorrentFile.FullPath + ", count: " + FsFileInfos.Count + ", state: " + State;
    }
}