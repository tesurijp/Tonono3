using System;
using tsr_di;

namespace Tonono3;

public static class DebugLogger
{
    [ServiceFunction]
    public static void WriteLog(string message)
    {
#if DEBUG
        Console.Error.WriteLine($"[Tonono3] {message}");
#endif
    }
}
