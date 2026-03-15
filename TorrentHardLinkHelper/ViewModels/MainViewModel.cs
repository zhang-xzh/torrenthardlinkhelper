using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ookii.Dialogs.Wpf;
using TorrentHardLinkHelper.HardLink;
using TorrentHardLinkHelper.Locate;
using TorrentHardLinkHelper.Models;
using TorrentHardLinkHelper.Properties;
using TorrentHardLinkHelper.Torrents;
using TorrentHardLinkHelper.Views;

namespace TorrentHardLinkHelper.ViewModels;

public class MainViewModel : ObservableObject
{
    private static readonly IList<string> _outputNameTypes = new[] { Resources.OutputNameType_TorrentTitle, Resources.OutputNameType_TorrentName, Resources.OutputNameType_Custom };
    private Style _collapseAllStyle;
    private int _copyLimitSize;
    private int _curProcess;

    private Style _expandAllStyle;
    private IList<EntityModel> _fileSystemEntityModel;
    private IList<FileSystemFileInfo> _fileSystemFileInfos;
    private bool _isOutputNameReadonly;

    private LocateResult _locateResult;
    private int _maxProcess;
    private string _outputBaseFolder;
    private string _outputName;
    private string _outputNameType;

    private string _sourceFolder;
    private string _status;
    private Torrent _torrent;
    private IList<EntityModel> _torrentEntityModel;

    private string _torrentFile;
    private int _unlocatedCount = -1;

    public MainViewModel()
    {
        InitCommands();
        InitStyles();
        IsOutputNameReadonly = true;
        CopyLimitSize = 1024;
        UpdateStatusFormat(Resources.Status_Ready);
    }

    private void InitCommands()
    {
        SelectTorrentFileCommand = new RelayCommand(SelectTorrentFile);

        SelectSourceFolderCommand = new RelayCommand(SelectSourceFolder);

        SelectOutputBaseFolderCommand = new RelayCommand(SelectOutputBaseFolder);

        AnalyseCommand = new RelayCommand(Analyse, CanAnalyse);

        LinkCommand = new RelayCommand(Link, CanLink);

        LinkLinuxCommand = new RelayCommand(LinkLinux, CanLink);

        HardlinkLinuxCommand = new RelayCommand(HardlinkLinux, CanLink);

        MoveLinuxCommand = new RelayCommand(MoveLinux, CanLink);

        OutputNameTypeChangedCommand =
            new RelayCommand<SelectionChangedEventArgs>(args => ChangeOutputFolderNmae(args.AddedItems[0].ToString()));

        ExpandAllCommand = new RelayCommand<TreeView>(tv => { tv.ItemContainerStyle = _expandAllStyle; });
        CollapseAllCommand = new RelayCommand<TreeView>(tv => { tv.ItemContainerStyle = _collapseAllStyle; });

        HardlinkToolCommand = new RelayCommand(() =>
        {
            var tool = new HardLinkTool();
            tool.ShowDialog();
        });
    }

    private void InitStyles()
    {
        _expandAllStyle = new Style(typeof(TreeViewItem));
        _collapseAllStyle = new Style(typeof(TreeViewItem));

        _expandAllStyle.Setters.Add(new Setter(TreeViewItem.IsExpandedProperty, true));
        _collapseAllStyle.Setters.Add(new Setter(TreeViewItem.IsExpandedProperty, false));
    }

    private void UpdateStatusFormat(string format, params object[] args)
    {
        Status = string.Format(format, args);
    }

    private void SelectTorrentFile()
    {
        var dialog = new VistaOpenFileDialog();
        dialog.Title = Resources.DialogTitle_SelectTorrent;
        dialog.Filter = Resources.Dialog_Filter_TorrentFiles;
        dialog.Multiselect = false;
        dialog.CheckFileExists = true;
        dialog.ShowDialog();
        if (dialog.FileName != null)
        {
            TorrentFile = dialog.FileName;
            OpenTorrent();
        }
    }

    private void SelectSourceFolder()
    {
        var dialog = new VistaFolderBrowserDialog();
        dialog.ShowNewFolderButton = true;
        dialog.ShowDialog();
        if (dialog.SelectedPath != null)
        {
            SourceFolder = dialog.SelectedPath;
            FileSystemEntityModel = new[] { EntityModel.Load(SourceFolder) };
        }
    }

    private void SelectOutputBaseFolder()
    {
        var dialog = new VistaFolderBrowserDialog();
        dialog.ShowNewFolderButton = true;
        dialog.ShowDialog();
        if (dialog.SelectedPath != null) OutputBaseFolder = dialog.SelectedPath;
    }

    private bool CanAnalyse()
    {
        return !string.IsNullOrEmpty(_torrentFile) && !string.IsNullOrEmpty(_sourceFolder);
    }

    private bool CanLink()
    {
        return !string.IsNullOrEmpty(_outputBaseFolder) && !string.IsNullOrEmpty(_outputName) &&
               _locateResult != null;
    }

    private void OpenTorrent()
    {
        if (string.IsNullOrEmpty(_torrentFile)) return;
        try
        {
            _torrent = Torrent.Load(_torrentFile);
            ChangeOutputFolderNmae(_outputNameType);
            TorrentEntityModel = new[] { EntityModel.Load(_torrent) };
        }
        catch (Exception ex)
        {
            UpdateStatusFormat(Resources.Status_LoadTorrentFailed, ex.Message);
        }
    }

    public void LoadTorrentFile(string filePath)
    {
        TorrentFile = filePath;
        OpenTorrent();
    }

    public void LoadSourceFolder(string folderPath)
    {
        SourceFolder = folderPath;
        FileSystemEntityModel = new[] { EntityModel.Load(SourceFolder) };
    }

    public void LoadOutputBaseFolder(string folderPath)
    {
        OutputBaseFolder = folderPath;
    }

    private void ChangeOutputFolderNmae(string nameType)
    {
        if (nameType == "Custom")
            IsOutputNameReadonly = false;
        else
            IsOutputNameReadonly = true;
        if (_torrent == null)
        {
            OutputName = "";
            return;
        }

        switch (nameType)
        {
            case "Torrent Name":
                OutputName = Path.GetFileNameWithoutExtension(_torrentFile);
                IsOutputNameReadonly = true;
                break;
            case "Torrent Title":
                OutputName = _torrent.Name;
                IsOutputNameReadonly = true;
                break;
        }
    }

    private void Analyse()
    {
        UpdateStatusFormat(Resources.Status_Locating);
        var func = new Func<LocateResult>(Locate);
        func.BeginInvoke(AnalyseFinish, func);
    }

    private void AnalyseFinish(IAsyncResult ar)
    {
        var func = ar.AsyncState as Func<LocateResult>;
        try
        {
            var result = func.EndInvoke(ar);

            UpdateStatusFormat(Resources.Status_LocateSuccess,
                result.LocatedCount,
                result.LocatedCount + result.UnlocatedCount,
                result.TorrentFileLinks.Where(c => c.State == LinkState.Located)
                    .Where(c => c.LinkedFsFileInfo != null)
                    .Select(c => c.LinkedFsFileInfo.FilePath)
                    .Distinct()
                    .Count(), _fileSystemFileInfos.Count);
            _locateResult = result;
            _unlocatedCount = result.UnlocatedCount;

            EntityModel.Update(_fileSystemEntityModel[0], result.TorrentFileLinks);
            OnPropertyChanged(nameof(FileSystemEntityModel));

            TorrentEntityModel = new[] { EntityModel.Load(_torrent.Name, result) };

            Application.Current.Dispatcher.Invoke(() =>
            {
                LinkCommand.NotifyCanExecuteChanged();
                LinkLinuxCommand.NotifyCanExecuteChanged();
                HardlinkLinuxCommand.NotifyCanExecuteChanged();
                MoveLinuxCommand.NotifyCanExecuteChanged();
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString());
        }
    }

    private LocateResult Locate()
    {
        _fileSystemFileInfos = FileSystemFileSearcher.SearchFolder(_sourceFolder);
        var locater = new TorrentFileLocater(_torrent, _fileSystemFileInfos,
            () => CurPorcess = _curProcess + 1);
        MaxProcess = _torrent.Files.Length;
        CurPorcess = 0;
        var result = locater.Locate();
        return result;
    }

    private void Link()
    {
        if (Path.GetPathRoot(_outputBaseFolder) != Path.GetPathRoot(_sourceFolder))
        {
            UpdateStatusFormat(Resources.Status_LinkFailedDifferentDrive);
            return;
        }

        if (_unlocatedCount != 0)
        {
            var result = MessageBox.Show(string.Format(Resources.DialogMessage_HardLinkAnyway, _unlocatedCount), Resources.DialogTitle_Confirm, MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
            if (result != MessageBoxResult.OK) return;
        }

        UpdateStatusFormat(Resources.Status_Linking);
        var helper = new HardLinkHelper();
        helper.HardLink(_locateResult.TorrentFileLinks, _copyLimitSize, _outputName,
            _outputBaseFolder);
        var targetTorrentFile = Path.Combine(Path.Combine(_outputBaseFolder, _outputName), Path.GetFileName(_torrentFile));
        helper.Copy(_torrentFile, targetTorrentFile);
        UpdateStatusFormat(Resources.Status_Done);
        Process.Start(Resources.Process_Explorer, Path.Combine(_outputBaseFolder, _outputName));
    }

    private void LinkLinux()
    {
        if (_unlocatedCount != 0)
        {
            var result = MessageBox.Show(string.Format(Resources.DialogMessage_GenerateScriptAnyway, _unlocatedCount), Resources.DialogTitle_Confirm, MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
            if (result != MessageBoxResult.OK) return;
        }

        UpdateStatusFormat(Resources.Status_GeneratingLinuxSymlinkScript);
        var helper = new HardLinkHelper();
        helper.GenerateLinuxSymlinkScript(_locateResult.TorrentFileLinks, _outputName,
            _outputBaseFolder, _sourceFolder);
        UpdateStatusFormat(Resources.Status_ScriptSavedSymlink, _outputName);
    }

    private void HardlinkLinux()
    {
        if (_unlocatedCount != 0)
        {
            var result = MessageBox.Show(string.Format(Resources.DialogMessage_GenerateScriptAnyway, _unlocatedCount), Resources.DialogTitle_Confirm, MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
            if (result != MessageBoxResult.OK) return;
        }

        UpdateStatusFormat(Resources.Status_GeneratingLinuxHardlinkScript);
        var helper = new HardLinkHelper();
        helper.GenerateLinuxHardlinkScript(_locateResult.TorrentFileLinks, _outputName,
            _outputBaseFolder, _sourceFolder);
        UpdateStatusFormat(Resources.Status_ScriptSavedHardlink, _outputName);
    }

    private void MoveLinux()
    {
        if (_unlocatedCount != 0)
        {
            var result = MessageBox.Show(string.Format(Resources.DialogMessage_GenerateScriptAnyway, _unlocatedCount), Resources.DialogTitle_Confirm, MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
            if (result != MessageBoxResult.OK) return;
        }

        UpdateStatusFormat(Resources.Status_GeneratingLinuxMoveScript);
        var helper = new HardLinkHelper();
        helper.GenerateLinuxMoveScript(_locateResult.TorrentFileLinks, _outputName,
            _outputBaseFolder, _sourceFolder);
        UpdateStatusFormat(Resources.Status_ScriptSavedMove, _outputName);
    }

    #region Properties

    public string TorrentFile
    {
        get => _torrentFile;
        set
        {
            if (SetProperty(ref _torrentFile, value)) AnalyseCommand.NotifyCanExecuteChanged();
        }
    }

    public string SourceFolder
    {
        get => _sourceFolder;
        set
        {
            if (SetProperty(ref _sourceFolder, value)) AnalyseCommand.NotifyCanExecuteChanged();
        }
    }

    public string OutputBaseFolder
    {
        get => _outputBaseFolder;
        set
        {
            if (SetProperty(ref _outputBaseFolder, value))
            {
                LinkCommand.NotifyCanExecuteChanged();
                LinkLinuxCommand.NotifyCanExecuteChanged();
                HardlinkLinuxCommand.NotifyCanExecuteChanged();
                MoveLinuxCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string OutputName
    {
        get => _outputName;
        set
        {
            if (SetProperty(ref _outputName, value))
            {
                LinkCommand.NotifyCanExecuteChanged();
                LinkLinuxCommand.NotifyCanExecuteChanged();
                HardlinkLinuxCommand.NotifyCanExecuteChanged();
                MoveLinuxCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string OutputNameType
    {
        get => _outputNameType;
        set => SetProperty(ref _outputNameType, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public IList<string> OutputNameTypes => _outputNameTypes;

    public bool IsOutputNameReadonly
    {
        get => _isOutputNameReadonly;
        set => SetProperty(ref _isOutputNameReadonly, value);
    }

    public IList<EntityModel> FileSystemEntityModel
    {
        get => _fileSystemEntityModel;
        set => SetProperty(ref _fileSystemEntityModel, value);
    }

    public IList<EntityModel> TorrentEntityModel
    {
        get => _torrentEntityModel;
        set => SetProperty(ref _torrentEntityModel, value);
    }

    public int CopyLimitSize
    {
        get => _copyLimitSize;
        set => SetProperty(ref _copyLimitSize, value);
    }

    public int MaxProcess
    {
        get => _maxProcess;
        set => SetProperty(ref _maxProcess, value);
    }

    public int CurPorcess
    {
        get => _curProcess;
        set => SetProperty(ref _curProcess, value);
    }

    #endregion

    #region Commands

    public RelayCommand SelectTorrentFileCommand { get; private set; }

    public RelayCommand SelectSourceFolderCommand { get; private set; }

    public RelayCommand SelectOutputBaseFolderCommand { get; private set; }

    public RelayCommand AnalyseCommand { get; private set; }

    public RelayCommand LinkCommand { get; private set; }

    public RelayCommand LinkLinuxCommand { get; private set; }

    public RelayCommand HardlinkLinuxCommand { get; private set; }

    public RelayCommand MoveLinuxCommand { get; private set; }

    public RelayCommand<SelectionChangedEventArgs> OutputNameTypeChangedCommand { get; private set; }

    public RelayCommand<TreeView> ExpandAllCommand { get; private set; }

    public RelayCommand<TreeView> CollapseAllCommand { get; private set; }

    public RelayCommand HardlinkToolCommand { get; private set; }

    #endregion
}