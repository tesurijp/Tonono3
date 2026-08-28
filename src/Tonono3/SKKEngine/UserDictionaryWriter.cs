using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tonono3.AutoDefined;
using tsr_di;

namespace Tonono3.SKKEngine;

internal sealed class UserDictionaryWriter(string FilePath, SerializeUserDictionaryFunc serializeDictionary,  WriteLogFunc WriteLog) : IUserDictionaryWriter
{
    private sealed record class DicBuffer(long Version, DictionarySnapshot Dictionary);
    private readonly static Lock lockObj = new();
    private static long dicVersion;
    private DicBuffer? buffer;

    public void Enqueue(DictionarySnapshot dictionary)
    {
        lock (lockObj)
        {
            buffer = new(++dicVersion, dictionary);
        }
    }

    private async Task SaveAsync()
    {
        if (buffer is not null)
        {
            var path = Path.GetTempFileName();
            var folder = Path.GetDirectoryName(path)!;
            Directory.CreateDirectory(folder);
            var lines = serializeDictionary(buffer.Dictionary);
            await File.WriteAllLinesAsync(path, lines, Encoding.UTF8).ConfigureAwait(false);
            lock (lockObj)
            {
                if (buffer.Version == dicVersion)
                {
                    File.Move(path, FilePath, overwrite: true);
                }
            }
        }
    }

    private void RunForget()
    {
        try
        {
            Task.Run(SaveAsync);
            WriteLog("Update Dictionary");
        }
        catch (Exception ex)
        {
            WriteLog($"Failed to save user dictionary: {ex.Message}");
        }
    }

    public void Dispose() => RunForget();
}

[ServiceClass(Lifetime = Lifetime.Singleton)]
public sealed class UserDictionaryWriterFactory(SerializeUserDictionaryFunc serializeDictionary, WriteLogFunc writeLog)
{
    [ServiceFunction(ServiceName = "CreateUserDictionaryWriterFunc")]
    public IUserDictionaryWriter Create(string path) => new UserDictionaryWriter(path, serializeDictionary, writeLog);
}
