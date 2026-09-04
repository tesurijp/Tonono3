using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Tonono3.AutoDefined;
using tsr_di;

namespace Tonono3.SkkEngine;

[ServiceClass(Lifetime = Lifetime.Singleton)]
public sealed class SkkDictionaryFileLoader(LoadDictionaryFunc loadDictionary, WriteLogFunc writeLog)
{
    static SkkDictionaryFileLoader() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    [ServiceFunction(ServiceName = "LoadSkkDictionaryFunc")]
    public DictionarySnapshot Load(IEnumerable<string> mainPaths, string userPath) =>
        loadDictionary(ReadMainDictionary(mainPaths), ReadUserDictionary(userPath));

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

    private IEnumerable<string> ReadMainDictionary(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            if (!File.Exists(path))
            {
                writeLog($"Dictionary file does not exist. {path}");
            }

            var buffer = FileBuffer(path);
            using var reader = ReadBuffer(buffer);
            while (reader.ReadLine() is string line)
            {
                yield return line;
            }
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

    private static IEnumerable<string> ReadUserDictionary(string path)
    {
        if (!File.Exists(path))
        {
            yield break;
        }

        using var reader = new StreamReader(path);
        while (reader.ReadLine() is string line)
        {
            yield return line;
        }
    }
}
