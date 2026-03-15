using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TorrentHardLinkHelper.HardLink;

namespace TorrentHardLinkHelper.Test;

[TestClass]
public class HardLinkTest
{
    private string _testDir;

    // Windows API for creating hard links (same as in HardLinkHelper)
    [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

    [TestInitialize]
    public void TestInitialize()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "HardLinkTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(_testDir)) Directory.Delete(_testDir, true);
    }

    [TestMethod]
    public void TestCreateHardLink_WithWindowsAPI()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDir, "source.txt");
        var hardLinkFile = Path.Combine(_testDir, "hardlink.txt");
        var content = "Test content for hard link";
        File.WriteAllText(sourceFile, content);

        // Act
        var result = CreateHardLink(hardLinkFile, sourceFile, IntPtr.Zero);

        // Assert
        Assert.IsTrue(result, "CreateHardLink should return true");
        Assert.IsTrue(File.Exists(hardLinkFile), "Hard link file should exist");
        Assert.AreEqual(content, File.ReadAllText(hardLinkFile), "Hard link should have same content");

        // Verify they point to the same file (same inode)
        var sourceInfo = new FileInfo(sourceFile);
        var hardLinkInfo = new FileInfo(hardLinkFile);
        Assert.AreEqual(sourceInfo.Length, hardLinkInfo.Length, "File sizes should match");
    }

    [TestMethod]
    public void TestHardLinkHelper_CreateHardLink()
    {
        // Arrange
        var helper = new HardLinkHelper();
        var sourceFolder = Path.Combine(_testDir, "SourceFolder");
        var targetParentFolder = _testDir;
        var folderName = "TargetFolder";
        var sourceFile = Path.Combine(sourceFolder, "test.txt");

        Directory.CreateDirectory(sourceFolder);
        File.WriteAllText(sourceFile, "Test content");

        // Act
        helper.HardLink(sourceFolder, targetParentFolder, folderName, 1024);

        // Assert
        var targetFolder = Path.Combine(targetParentFolder, folderName);
        var targetFile = Path.Combine(targetFolder, "test.txt");

        Assert.IsTrue(Directory.Exists(targetFolder), "Target folder should exist");
        Assert.IsTrue(File.Exists(targetFile), "Target file (hard link) should exist");
        Assert.AreEqual(File.ReadAllText(sourceFile), File.ReadAllText(targetFile), "Content should match");

        // Verify script file was created
        var scriptFile = Path.Combine(targetFolder, "!hard-link.cmd");
        Assert.IsTrue(File.Exists(scriptFile), "Script file should be created");
    }

    [TestMethod]
    public void TestHardLinkHelper_SmallFileCopy()
    {
        // Arrange - file smaller than copyLimitSize should be copied, not hard linked
        var helper = new HardLinkHelper();
        var sourceFolder = Path.Combine(_testDir, "SourceFolder");
        var targetParentFolder = _testDir;
        var folderName = "TargetFolder";
        var sourceFile = Path.Combine(sourceFolder, "small.txt");

        Directory.CreateDirectory(sourceFolder);
        File.WriteAllText(sourceFile, "Small"); // Very small file

        // Act - set copyLimitSize high so small files are copied
        helper.HardLink(sourceFolder, targetParentFolder, folderName, 100);

        // Assert
        var targetFolder = Path.Combine(targetParentFolder, folderName);
        var targetFile = Path.Combine(targetFolder, "small.txt");

        Assert.IsTrue(File.Exists(targetFile), "Target file should exist");
        Assert.AreEqual(File.ReadAllText(sourceFile), File.ReadAllText(targetFile), "Content should match");
    }

    [TestMethod]
    public void TestHardLinkHelper_LargeFileHardLink()
    {
        // Arrange - file larger than copyLimitSize should be hard linked
        var helper = new HardLinkHelper();
        var sourceFolder = Path.Combine(_testDir, "SourceFolder");
        var targetParentFolder = _testDir;
        var folderName = "TargetFolder";
        var sourceFile = Path.Combine(sourceFolder, "large.txt");

        Directory.CreateDirectory(sourceFolder);
        // Create content larger than copyLimitSize
        var content = new string('X', 200);
        File.WriteAllText(sourceFile, content);

        // Act - set copyLimitSize low so large files are hard linked
        helper.HardLink(sourceFolder, targetParentFolder, folderName, 100);

        // Assert
        var targetFolder = Path.Combine(targetParentFolder, folderName);
        var targetFile = Path.Combine(targetFolder, "large.txt");

        Assert.IsTrue(File.Exists(targetFile), "Target file should exist");
        Assert.AreEqual(content, File.ReadAllText(targetFile), "Content should match");
    }

    [TestMethod]
    public void TestCreateHardLink_WithLongPath()
    {
        // Arrange - create a path longer than 260 characters (MAX_PATH)
        // Use temp directory and create nested folders to exceed MAX_PATH
        var longPathPart = new string('A', 50);
        var nestedFolder = Path.Combine(longPathPart, longPathPart, longPathPart, longPathPart);
        var sourceFolder = Path.Combine(_testDir, nestedFolder);
        var sourceFile = Path.Combine(sourceFolder, "source.txt");
        var hardLinkFile = Path.Combine(sourceFolder, "hardlink.txt");

        Directory.CreateDirectory(sourceFolder);

        var content = "Test content for long path hard link";
        File.WriteAllText(sourceFile, content);

        // Verify path is long (over 260 chars)
        Assert.IsTrue(sourceFile.Length > 260, $"Source file path should be long (>260 chars), actual: {sourceFile.Length}");

        // Act - use Windows API with long path prefix
        var longPathSource = sourceFile.StartsWith(@"\\?\") ? sourceFile : @"\\?\" + sourceFile;
        var longPathTarget = hardLinkFile.StartsWith(@"\\?\") ? hardLinkFile : @"\\?\" + hardLinkFile;
        var result = CreateHardLink(longPathTarget, longPathSource, IntPtr.Zero);

        // Assert
        Assert.IsTrue(result, "CreateHardLink should return true for long paths");
        Assert.IsTrue(File.Exists(hardLinkFile), "Hard link file should exist");
        Assert.AreEqual(content, File.ReadAllText(hardLinkFile), "Hard link should have same content");
    }

    [TestMethod]
    public void TestHardLinkHelper_WithLongPath()
    {
        // Arrange - create a path structure that results in long paths
        var longPathPart = new string('B', 40);
        var nestedFolder = Path.Combine(longPathPart, longPathPart, longPathPart);
        var sourceFolder = Path.Combine(_testDir, nestedFolder);
        var targetParentFolder = _testDir;
        var folderName = "Target" + longPathPart;
        var sourceFile = Path.Combine(sourceFolder, "test.txt");

        Directory.CreateDirectory(sourceFolder);
        var content = "Test content for long path";
        File.WriteAllText(sourceFile, content);

        // Act
        var helper = new HardLinkHelper();
        helper.HardLink(sourceFolder, targetParentFolder, folderName, 1024000);

        // Assert
        var targetFolder = Path.Combine(targetParentFolder, folderName);
        var targetFile = Path.Combine(targetFolder, "test.txt");

        Assert.IsTrue(Directory.Exists(targetFolder), "Target folder should exist");
        Assert.IsTrue(File.Exists(targetFile), "Target file should exist");
        Assert.AreEqual(content, File.ReadAllText(targetFile), "Content should match");
    }
}