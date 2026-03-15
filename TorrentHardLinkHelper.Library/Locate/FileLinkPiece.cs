namespace TorrentHardLinkHelper.Locate;

public class FileLinkPiece
{
    public TorrentFileLink FileLink { get; set; }
    public ulong StartPos { get; set; }
    public ulong ReadLength { get; set; }

    public override string ToString()
    {
        return FileLink + ", startpos: " + StartPos + ", length: " + ReadLength;
    }
}