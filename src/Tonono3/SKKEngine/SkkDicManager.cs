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

public interface ISkkDictionaryLoader;

[ServiceClass(Lifetime = Lifetime.Singleton)]
public sealed class SkkDicManager(IParseDictionaryLine parseDictionaryLine) : ISkkDictionaryLoader
{
    static SkkDicManager() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    [ServiceFunction(ServiceName = "LoadSkkDictionary")]
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

    private static byte[] DicBuffer(string path)
    {
        using var fileStream = File.OpenRead(path);
        using Stream inputStream = path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)
            ? new GZipStream(fileStream, CompressionMode.Decompress)
            : fileStream;

        // Read into memory to handle encoding detection
        using var memoryStream = new MemoryStream();
        inputStream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    private void LoadMainDictionary(string path, Dictionary<string, List<string>> builder)
    {
        if (!File.Exists(path))
        {
            throw new DictionaryLoadException(path, new FileNotFoundException("Dictionary file does not exist.", path));
        }

        try
        {
            var buffer = DicBuffer(path);
            var encoding = DetectEncoding(buffer);
            using var reader = new StreamReader(new MemoryStream(buffer), encoding);

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                ParseLine(line, builder);
            }
        }
        catch (Exception ex) when (ex is not DictionaryLoadException)
        {
            throw new DictionaryLoadException(path, ex);
        }
    }

    private static Encoding DetectEncoding(byte[] buffer)
    {
        // Simple heuristic for SKK dictionaries: usually EUC-JP or UTF-8.
        // If it has UTF-8 BOM, it's UTF-8.
        if (buffer.Length >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
        {
            return Encoding.UTF8;
        }

        // Try to see if it's valid UTF-8
        try
        {
            var utf8 = new UTF8Encoding(false, true);
            utf8.GetString(buffer);
            return utf8;
        }
        catch (ArgumentException)
        {
            // Fallback to EUC-JP
            return Encoding.GetEncoding("euc-jp");
        }
    }

    private ImmutableDictionary<string, ImmutableArray<string>> LoadUserDictionary(string path)
    {
        var builder = new Dictionary<string, List<string>>();
        if (File.Exists(path))
        {
            foreach (var line in File.ReadLines(path, Encoding.UTF8))
            {
                ParseLine(line, builder);
            }
        }
        return Freeze(builder);
    }

    private void ParseLine(string line, Dictionary<string, List<string>> targetDict)
    {
        var entry = parseDictionaryLine(line);
        if (entry.IsValid)
        {
            IEnumerable<string> candidates = entry.Candidates;

            if (targetDict.TryGetValue(entry.Reading, out var prev))
            {
                candidates = [.. prev.Union(candidates)];
            }
            targetDict[entry.Reading] = [.. candidates];
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
