using System.Globalization;
using System.Threading;
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
        // 设置默认语言为中文
        Thread.CurrentThread.CurrentUICulture = new CultureInfo("zh-CN");
        Thread.CurrentThread.CurrentCulture = new CultureInfo("zh-CN");

        base.OnStartup(e);
        StartupArgs = e.Args;
    }
}
