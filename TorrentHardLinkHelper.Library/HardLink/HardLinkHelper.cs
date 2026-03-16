using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using TorrentHardLinkHelper.Locate;

namespace TorrentHardLinkHelper.HardLink;

public class HardLinkHelper
{
    private StringBuilder _builder;
    private List<string> _createdFolders;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

    public void HardLink(string sourceFolder, string targetParentFolder, string folderName, int copyLimitSize)
    {
        _builder = new StringBuilder();
        _builder.AppendLine("chcp 65001");
        _builder.AppendLine("::==============================================::");
        _builder.AppendLine(":: Torrent Hard-Link Helper - HardLink Script");
        _builder.AppendLine("::");
        _builder.AppendLine(":: Created at " + DateTime.Now);
        _builder.AppendLine("::==============================================::");
        _builder.AppendLine("::.");

        var rootFolder = Path.Combine(targetParentFolder, folderName);
        if (!Directory.Exists(rootFolder)) CreateFolder(rootFolder);
        if (!Directory.Exists(targetParentFolder)) CreateFolder(targetParentFolder);
        SearchFolder(sourceFolder, rootFolder, copyLimitSize);
        var utf8Bom = new UTF8Encoding(true);
        File.WriteAllText(Path.Combine(rootFolder, "!hard-link.cmd"), _builder.ToString(), utf8Bom);
    }

    private void SearchFolder(string folder, string targetParentFolder, int copyLimitSize)
    {
        _createdFolders ??= [];
        if (_createdFolders.Contains(folder)) return;
        foreach (var file in Directory.GetFiles(folder))
        {
            var targetFile = Path.Combine(targetParentFolder, Path.GetFileName(file));
            var fileInfo = new FileInfo(file);
            if (fileInfo.Length >= copyLimitSize)
                CreateHarkLink(file, targetFile);
            else
                Copy(file, targetFile);
        }

        foreach (var subFolder in Directory.GetDirectories(folder))
        {
            var targetSubFolder = Path.Combine(targetParentFolder, Path.GetFileName(subFolder));
            if (!Directory.Exists(targetSubFolder))
            {
                CreateFolder(targetSubFolder);
                _createdFolders.Add(targetSubFolder);
            }

            SearchFolder(subFolder, targetSubFolder, copyLimitSize);
        }
    }

    public void HardLink(IList<TorrentFileLink> links, int copyLimitSize, string folderName, string baseFolde)
    {
        var rootFolder = Path.Combine(baseFolde, folderName);

        _builder = new StringBuilder();
        _builder.AppendLine("chcp 65001");
        _builder.AppendLine("::==============================================::");
        _builder.AppendLine(":: Torrent Hard-Link Helper - HardLink Script");
        _builder.AppendLine("::");
        _builder.AppendLine(":: Created at " + DateTime.Now);
        _builder.AppendLine("::==============================================::");
        _builder.AppendLine("::.");
        if (!Directory.Exists(rootFolder)) CreateFolder(rootFolder);
        foreach (var link in links)
        {
            if (link.LinkedFsFileInfo == null) continue;
            var pathParts = link.TorrentFile.Path.Split('\\');
            for (var i = 0; i < pathParts.Length - 1; i++)
            {
                var targetPathParts = new string[i + 2];
                targetPathParts[0] = rootFolder;
                Array.Copy(pathParts, 0, targetPathParts, 1, i + 1);
                var targetPath = Path.Combine(targetPathParts);
                if (!Directory.Exists(targetPath)) CreateFolder(targetPath);
            }

            var targetFile = Path.Combine(rootFolder, link.TorrentFile.Path);

            if (link.TorrentFile.Length >= copyLimitSize)
                CreateHarkLink(link.LinkedFsFileInfo.FilePath, targetFile);
            else
                Copy(link.LinkedFsFileInfo.FilePath, targetFile);
        }

        var utf8WithBom = new UTF8Encoding(true);
        File.WriteAllText(Path.Combine(rootFolder, "!hard-link.cmd"), _builder.ToString(), utf8WithBom);
    }

    private void CreateHarkLink(string source, string target)
    {
        _builder.AppendLine($"fsutil hardlink create \"{target}\" \"{source}\"");

        // Use Windows API instead of command line
        if (!CreateHardLink(target, source, IntPtr.Zero))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(),
                $"Failed to create hard link from '{source}' to '{target}'");
        }
    }

    public void Copy(string source, string target)
    {
        _builder.AppendLine($"copy /y \"{source}\" \"{target}\"");

        // Use .NET File.Copy instead of command line
        File.Copy(source, target, overwrite: true);
    }

    private void CreateFolder(string path)
    {
        _builder.AppendLine($"mkdir  \"{path}\"");
        Directory.CreateDirectory(path);
    }

    public void GenerateLinuxSymlinkScript(IList<TorrentFileLink> links, string folderName, string baseFolder, string sourceFolder)
    {
        _builder = new StringBuilder();
        _builder.AppendLine("#!/bin/bash");
        _builder.AppendLine("#==============================================");
        _builder.AppendLine("# Torrent Hard-Link Helper - Linux Symlink Script");
        _builder.AppendLine("#");
        _builder.AppendLine("# Created at " + DateTime.Now);
        _builder.AppendLine("#==============================================");
        _builder.AppendLine("");
        _builder.AppendLine("# Change to the script directory");
        _builder.AppendLine("cd \"$(dirname \"$0\")\"");
        _builder.AppendLine("");

        // Create base folder
        var linuxFolderName = ToLinuxPath(folderName);
        _builder.AppendLine($"mkdir -p \"{linuxFolderName}\"");
        _builder.AppendLine("");

        // Track directories we've already added mkdir commands for
        var createdLinuxDirs = new HashSet<string>();

        foreach (var link in links)
        {
            if (link.LinkedFsFileInfo == null) continue;

            // Create subdirectories inside base folder
            var pathParts = link.TorrentFile.Path.Split('\\');
            if (pathParts.Length > 1)
            {
                var linuxDirPath = linuxFolderName + "/" + ToLinuxPath(string.Join("\\", pathParts, 0, pathParts.Length - 1));

                if (!createdLinuxDirs.Contains(linuxDirPath))
                {
                    _builder.AppendLine($"mkdir -p \"{linuxDirPath}\"");
                    createdLinuxDirs.Add(linuxDirPath);
                }
            }

            // Calculate relative path from target location to source file
            var linuxTargetPath = linuxFolderName + "/" + ToLinuxPath(link.TorrentFile.Path);
            var relativePath = GetRelativeSourcePath(link.TorrentFile.Path, link.LinkedFsFileInfo.FilePath, sourceFolder);

            _builder.AppendLine($"ln -s \"{relativePath}\" \"{linuxTargetPath}\"");
        }

        var utf8Nobom = new UTF8Encoding(false);
        var scriptContent = _builder.ToString().Replace("\r\n", "\n");
        File.WriteAllText(Path.Combine(baseFolder, folderName + "_symlink.sh"), scriptContent, utf8Nobom);
    }

    public void GenerateLinuxMoveScript(IList<TorrentFileLink> links, string folderName, string baseFolder, string sourceFolder)
    {
        _builder = new StringBuilder();
        _builder.AppendLine("#!/bin/bash");
        _builder.AppendLine("#==============================================");
        _builder.AppendLine("# Torrent Hard-Link Helper - Linux Move Script");
        _builder.AppendLine("#");
        _builder.AppendLine("# Created at " + DateTime.Now);
        _builder.AppendLine("#==============================================");
        _builder.AppendLine("");
        _builder.AppendLine("# Change to the script directory");
        _builder.AppendLine("cd \"$(dirname \"$0\")\"");
        _builder.AppendLine("");

        // Create base folder
        var linuxFolderName = ToLinuxPath(folderName);
        _builder.AppendLine($"mkdir -p \"{linuxFolderName}\"");
        _builder.AppendLine("");

        // Track directories we've already added mkdir commands for
        var createdLinuxDirs = new HashSet<string>();

        foreach (var link in links)
        {
            if (link.LinkedFsFileInfo == null) continue;

            // Create subdirectories inside base folder
            var pathParts = link.TorrentFile.Path.Split('\\');
            if (pathParts.Length > 1)
            {
                var linuxDirPath = linuxFolderName + "/" + ToLinuxPath(string.Join("\\", pathParts, 0, pathParts.Length - 1));

                if (!createdLinuxDirs.Contains(linuxDirPath))
                {
                    _builder.AppendLine($"mkdir -p \"{linuxDirPath}\"");
                    createdLinuxDirs.Add(linuxDirPath);
                }
            }

            // Get source path relative to source folder
            var sourceRelative = link.LinkedFsFileInfo.FilePath.Substring(sourceFolder.Length).TrimStart('\\');
            var sourceFolderName = Path.GetFileName(sourceFolder);
            var linuxSourcePath = sourceFolderName + "/" + ToLinuxPath(sourceRelative);
            var linuxTargetPath = linuxFolderName + "/" + ToLinuxPath(link.TorrentFile.Path);

            _builder.AppendLine($"mv \"{linuxSourcePath}\" \"{linuxTargetPath}\"");
        }

        var utf8Nobom = new UTF8Encoding(false);
        var scriptContent = _builder.ToString().Replace("\r\n", "\n");
        File.WriteAllText(Path.Combine(baseFolder, folderName + "_move.sh"), scriptContent, utf8Nobom);
    }

    public void GenerateLinuxHardlinkScript(IList<TorrentFileLink> links, string folderName, string baseFolder, string sourceFolder)
    {
        _builder = new StringBuilder();
        _builder.AppendLine("#!/bin/bash");
        _builder.AppendLine("#==============================================");
        _builder.AppendLine("# Torrent Hard-Link Helper - Linux Hard Link Script");
        _builder.AppendLine("#");
        _builder.AppendLine("# Created at " + DateTime.Now);
        _builder.AppendLine("#==============================================");
        _builder.AppendLine("");
        _builder.AppendLine("# Change to the script directory");
        _builder.AppendLine("cd \"$(dirname \"$0\")\"");
        _builder.AppendLine("");

        // Create base folder
        var linuxFolderName = ToLinuxPath(folderName);
        _builder.AppendLine($"mkdir -p \"{linuxFolderName}\"");
        _builder.AppendLine("");

        // Track directories we've already added mkdir commands for
        var createdLinuxDirs = new HashSet<string>();

        foreach (var link in links)
        {
            if (link.LinkedFsFileInfo == null) continue;

            // Create subdirectories inside base folder
            var pathParts = link.TorrentFile.Path.Split('\\');
            if (pathParts.Length > 1)
            {
                var linuxDirPath = linuxFolderName + "/" + ToLinuxPath(string.Join("\\", pathParts, 0, pathParts.Length - 1));

                if (!createdLinuxDirs.Contains(linuxDirPath))
                {
                    _builder.AppendLine($"mkdir -p \"{linuxDirPath}\"");
                    createdLinuxDirs.Add(linuxDirPath);
                }
            }

            // Get source path relative to source folder
            var sourceRelative = link.LinkedFsFileInfo.FilePath[sourceFolder.Length..].TrimStart('\\');
            var sourceFolderName = Path.GetFileName(sourceFolder);
            var linuxSourcePath = sourceFolderName + "/" + ToLinuxPath(sourceRelative);
            var linuxTargetPath = linuxFolderName + "/" + ToLinuxPath(link.TorrentFile.Path);

            _builder.AppendLine($"ln \"{linuxSourcePath}\" \"{linuxTargetPath}\"");
        }

        var utf8Nobom = new UTF8Encoding(false);
        var scriptContent = _builder.ToString().Replace("\r\n", "\n");
        File.WriteAllText(Path.Combine(baseFolder, folderName + "_hardlink.sh"), scriptContent, utf8Nobom);
    }

    private string GetRelativeSourcePath(string torrentFilePath, string sourceFile, string sourceFolder)
    {
        // Calculate how many directories deep the target file is (add 1 for the base folder)
        var pathParts = torrentFilePath.Split('\\');
        var depth = pathParts.Length; // includes base folder level

        // Build the relative path prefix (../ for each level)
        var relativePath = new StringBuilder();
        for (var i = 0; i < depth; i++) relativePath.Append("../");

        // Get the source file path relative to source folder
        var sourceRelative = sourceFile.Substring(sourceFolder.Length).TrimStart('\\');
        var linuxSourceRelative = ToLinuxPath(sourceRelative);

        // Combine with source folder name
        var sourceFolderName = Path.GetFileName(sourceFolder);
        return relativePath + sourceFolderName + "/" + linuxSourceRelative;
    }

    private string ToLinuxPath(string windowsPath)
    {
        return windowsPath.Replace('\\', '/');
    }
}