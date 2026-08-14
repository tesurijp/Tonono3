using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Tonono3.AutoDefined;
using tsr_di;

namespace Tonono3.SKKEngine;

internal sealed class UserDictionaryWriter : IUserDictionaryWriter
{
    private readonly string path;
    private readonly WriteLogFunc writeLog;
    private readonly Channel<ImmutableDictionary<string, ImmutableArray<string>>> channel;
    private readonly Task writerTask;
    private bool disposed;

    internal UserDictionaryWriter(string path, WriteLogFunc writeLog)
    {
        this.path = path;
        this.writeLog = writeLog;
        channel = Channel.CreateUnbounded<ImmutableDictionary<string, ImmutableArray<string>>>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
        writerTask = RunAsync();
    }

    public void Enqueue(ImmutableDictionary<string, ImmutableArray<string>> dictionary)
    {
        if (!disposed)
        {
            channel.Writer.TryWrite(dictionary);
        }
    }

    private async Task RunAsync()
    {
        await foreach (var first in channel.Reader.ReadAllAsync())
        {
            var latest = first;
            while (channel.Reader.TryRead(out var newer))
            {
                latest = newer;
            }
            await SaveAsync(latest).ConfigureAwait(false);
        }
    }

    private async Task SaveAsync(ImmutableDictionary<string, ImmutableArray<string>> dictionary)
    {
        var folder = Path.GetDirectoryName(path);
        var tempPath = path + ".tmp";
        try
        {
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }
            var lines = dictionary.Select(pair => $"{pair.Key} /{string.Join("/", pair.Value)}/");
            await File.WriteAllLinesAsync(tempPath, lines, Encoding.UTF8).ConfigureAwait(false);
            File.Move(tempPath, path, overwrite: true);
        }
        catch (Exception ex)
        {
            writeLog($"Failed to save user dictionary: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }
        disposed = true;
        channel.Writer.TryComplete();
        if (!writerTask.Wait(TimeSpan.FromSeconds(5)))
        {
            writeLog("Timed out while flushing user dictionary.");
        }
    }
}

[ServiceClass(Lifetime = Lifetime.Singleton)]
public sealed class UserDictionaryWriterFactory(WriteLogFunc writeLog) 
{
    [ServiceFunction(ServiceName = "CreateUserDictionaryWriterFunc")]
    public IUserDictionaryWriter Create(string path) => new UserDictionaryWriter(path, writeLog);
}
