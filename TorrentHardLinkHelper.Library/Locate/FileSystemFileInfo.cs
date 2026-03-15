using System.IO;

namespace TorrentHardLinkHelper.Locate;

public class FileSystemFileInfo
{
    public FileSystemFileInfo()
    {
        Located = false;
    }

    public FileSystemFileInfo(FileInfo fileInfo)
    {
        FileName = fileInfo.Name;
        FilePath = fileInfo.FullName;
        Length = fileInfo.Length;
    }

    public string FileName { get; internal set; }

    public string FilePath { get; internal set; }

    public long Length { get; internal set; }

    public bool Located { get; set; }

    public override string ToString()
    {
        return FilePath + ", length: " + Length;
    }
}