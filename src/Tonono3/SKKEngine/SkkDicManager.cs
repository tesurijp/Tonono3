using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Tonono3.AutoDefined;
using tsr_di;

namespace Tonono3.SKKEngine;

[ServiceClass(Lifetime = Lifetime.Singleton)]
public sealed class SkkDicManager(ParseDictionaryLineFunc parseDictionaryLine, WriteLogFunc writeLog)
{
    static SkkDicManager() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    [ServiceFunction(ServiceName = "LoadSkkDictionaryFunc")]
    public SkkDictionarySnapshot Load(IEnumerable<string> mainPaths, string userPath) =>
        new(LoadMainDictionaries(mainPaths), LoadUserDictionary(userPath));

    private ImmutableDictionary<string, ImmutableArray<string>> LoadMainDictionaries(IEnumerable<string> paths)
    {
        var builder = new Dictionary<string, List<string>>();
        foreach (var path in paths)
        {
            LoadMainDictionary(path, builder);
        }
        return Freeze(builder);
    }

    private static byte[] FileBuffer(string path)
    {
        if (path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
        {
            using var fileStream = File.OpenRead(path);
            using var gzStream = new GZipStream(fileStream, CompressionMode.Decompress);
            using var ms = new MemoryStream();
            gzStream.CopyTo(ms);
            return ms.ToArray();
        }
        else
        {
            return File.ReadAllBytes(path);
        }
    }

    private void LoadMainDictionary(string path, Dictionary<string, List<string>> builder)
    {
        if (File.Exists(path))
        {
            try
            {
                var buffer = FileBuffer(path);
                using var reader = ReadBuffer(buffer);
                ParseLines(reader, builder);
            }
            catch (Exception ex) when (ex is not DictionaryLoadException)
            {
                throw new DictionaryLoadException(path, ex);
            }
        }
        else
        {
            writeLog($"Dictionary file does not exist. {path}");
            return;
        }
    }

    private static StringReader ReadBuffer(byte[] buffer)
    {
        try
        {
            return new StringReader(new UTF8Encoding(false, true).GetString(buffer));
        }
        catch (ArgumentException)
        {
            return new StringReader(Encoding.GetEncoding("euc-jp").GetString(buffer));
        }
    }

    private ImmutableDictionary<string, ImmutableArray<string>> LoadUserDictionary(string path)
    {
        var builder = new Dictionary<string, List<string>>();
        if (File.Exists(path))
        {
            using var reader = new StreamReader(path);
            ParseLines(reader, builder);
        }
        return Freeze(builder);
    }

    private void ParseLines(TextReader reader, Dictionary<string, List<string>> targetDict)
    {
        while (reader.ReadLine() is string line)
        {
            var entry = parseDictionaryLine(line);
            if (entry.IsValid)
            {
                if (targetDict.TryGetValue(entry.Reading, out var prev))
                {
                    targetDict[entry.Reading] = [.. prev.Union(entry.Candidates)];
                }
                else
                {
                    targetDict[entry.Reading] = [.. entry.Candidates];
                }
            }
        }
    }

    private static ImmutableDictionary<string, ImmutableArray<string>> Freeze(Dictionary<string, List<string>> source) =>
        source.ToImmutableDictionary(pair => pair.Key, pair => pair.Value.ToImmutableArray());
}

public sealed record SkkDictionarySnapshot(
    ImmutableDictionary<string, ImmutableArray<string>> Main,
    ImmutableDictionary<string, ImmutableArray<string>> User);

public sealed class DictionaryLoadException(string path, Exception innerException)
    : IOException($"Failed to load SKK dictionary: {path}", innerException)
{
    public string DictionaryPath { get; } = path;
}
