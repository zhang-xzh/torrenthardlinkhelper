using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ookii.Dialogs.Wpf;
using TorrentHardLinkHelper.HardLink;

namespace TorrentHardLinkHelper.ViewModels;

public class HardLinkToolViewModel : ObservableObject
{
    private string _folderName;
    private string _parentFolder;

    private string _sourceFolder;

    public HardLinkToolViewModel()
    {
        InitCommands();
    }

    public string SourceFolder
    {
        get => _sourceFolder;
        set
        {
            if (SetProperty(ref _sourceFolder, value)) LinkCommand.NotifyCanExecuteChanged();
        }
    }

    public string ParentFolder
    {
        get => _parentFolder;
        set
        {
            if (SetProperty(ref _parentFolder, value)) LinkCommand.NotifyCanExecuteChanged();
        }
    }

    public string FolderName
    {
        get => _folderName;
        set
        {
            if (SetProperty(ref _folderName, value)) LinkCommand.NotifyCanExecuteChanged();
        }
    }

    public RelayCommand SelectSourceFolderCommand { get; private set; }

    public RelayCommand SelectParentFolderCommand { get; private set; }

    public RelayCommand DefaultCommand { get; private set; }

    public RelayCommand LinkCommand { get; private set; }

    public void InitCommands()
    {
        SelectSourceFolderCommand = new RelayCommand(SelectSourceFolder);

        SelectParentFolderCommand = new RelayCommand(SelectParentFolder);

        DefaultCommand = new RelayCommand(Default);

        LinkCommand = new RelayCommand(Link, CanLink);
    }

    private void SelectSourceFolder()
    {
        var dialog = new VistaFolderBrowserDialog();
        dialog.ShowNewFolderButton = true;
        var result = dialog.ShowDialog();
        if (result == true && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
        {
            SourceFolder = dialog.SelectedPath;
            var parent = Directory.GetParent(dialog.SelectedPath);
            if (parent != null) ParentFolder = parent.FullName;
            FolderName = Path.GetFileName(dialog.SelectedPath) + "_Copy";
        }
    }

    private void SelectParentFolder()
    {
        var dialog = new VistaFolderBrowserDialog();
        dialog.ShowNewFolderButton = true;
        dialog.ShowDialog();
        if (dialog.SelectedPath != null) ParentFolder = dialog.SelectedPath;
    }

    private void Default()
    {
        if (string.IsNullOrWhiteSpace(_sourceFolder))
            FolderName = "";
        else
            FolderName = Path.GetFileName(_sourceFolder) + "_HLinked";
    }

    private void Link()
    {
        var helper = new HardLinkHelper();
        helper.HardLink(_sourceFolder, _parentFolder, _folderName, 1024000);
        Process.Start("explorer.exe", Path.Combine(_parentFolder, _folderName));
    }

    private bool CanLink()
    {
        return !string.IsNullOrWhiteSpace(_sourceFolder) &&
               !string.IsNullOrWhiteSpace(_parentFolder) &&
               !string.IsNullOrWhiteSpace(_folderName);
    }
}