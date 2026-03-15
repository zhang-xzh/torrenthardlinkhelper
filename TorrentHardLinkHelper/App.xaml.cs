using System.Windows;

namespace TorrentHardLinkHelper;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static string[] StartupArgs { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        StartupArgs = e.Args;
    }
}