using Avalonia;
using System;
using System.Threading;
using tsr_di;

namespace Tonono3;

[ServiceResolver]
internal static partial class Provider;

internal static class Program
{
    internal const string RestartArgument = "--restart";
    private const string SingleInstanceMutexName = "{E11B24F6-0499-4E83-A781-D847BDCD673B}";

    [STAThread]
    public static void Main(string[] args)
    {
        using var mutex = AcquireSingleInstance(args);
        if (mutex is not null)
        {
            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure(() => new App(Provider.Resolve<IApplicationCoordinator>()))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static Mutex? AcquireSingleInstance(string[] args)
    {
        var mutex = new Mutex(false, SingleInstanceMutexName, out var createdNew);
        if (createdNew || Array.IndexOf(args, RestartArgument) >= 0)
        {
            if (mutex.WaitOne(5000))
            {
                return mutex;
            }
        }

        mutex.Dispose();
        return null;
    }
}
