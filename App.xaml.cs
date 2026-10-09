using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using Serilog;

namespace UGConverter;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static ILogger Logger { get; private set; }
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        var args = e.Args;
        
        if (args.Contains("-console"))
            Logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Console().WriteTo.File(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", $"{DateTime.Now.Year}-{DateTime.Now.Month}-{DateTime.Now.Day}--{DateTime.Now.Hour}-{DateTime.Now.Minute}-{DateTime.Now.Second}.txt")).CreateLogger();
        else
            Logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.File(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", $"{DateTime.Now.Year}-{DateTime.Now.Month}-{DateTime.Now.Day}--{DateTime.Now.Hour}-{DateTime.Now.Minute}-{DateTime.Now.Second}.txt")).CreateLogger();
        
        Logger.Error($"GIVE ME A DAMN LOG    - lumberjacks");

    }
}