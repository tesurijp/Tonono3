using System.Runtime.CompilerServices;

namespace Tonono3.Tests;

internal static class UnsafeAccessors
{
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ScheduleReload")]
    internal static extern void ScheduleReload(ConfigWatcher watcher);
}
