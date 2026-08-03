using System;
using tsr_di;

namespace Tonono3;

public static class DebugLogger
{
    [ServiceFunction(ServiceName = "WriteLog")]
    public static void Log(string message)
    {
#if DEBUG
        Console.Error.WriteLine($"[Tonono3] {message}");
#endif
    }
}
