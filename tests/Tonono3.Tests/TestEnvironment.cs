using System.IO.Compression;
using System.Text;
using Tonono3.SKKEngine;

namespace Tonono3.Tests;

internal sealed class TestEnvironment : IDisposable
{
    private readonly string root;

    public TestEnvironment()
    {
        root = Path.Combine(Path.GetTempPath(), "Tonono3.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
    }

    public string PathFor(string fileName) => Path.Combine(root, fileName);

    public AppConfig CreateConfig(
        IEnumerable<string>? dictionaryPaths = null,
        string? userDictionaryPath = null,
        IEnumerable<string>? viCompatibleApps = null) =>
        new(
            new Dictionary<string, string>
            {
                ["a"] = "あ", ["i"] = "い", ["u"] = "う", ["e"] = "え", ["o"] = "お",
                ["ka"] = "か", ["ki"] = "き", ["ku"] = "く", ["ke"] = "け", ["ko"] = "こ",
                ["nn"] = "ん", [","] = "、", ["."] = "。", ["-"] = "ー"
            },
            new Dictionary<string, string> { ["kk"] = "っ", ["nk"] = "ん" },
            new Dictionary<string, string> { ["n"] = "ん" },
            new Dictionary<char, string> { ['a'] = "ａ", [' '] = "　", ['!'] = "！" },
            dictionaryPaths?.ToArray() ?? [CreateMainDictionary("main.txt", Encoding.UTF8, false)],
            userDictionaryPath ?? PathFor("user.txt"),
            viCompatibleApps?.ToArray() ?? ["vim.exe"]);

    public string CreateMainDictionary(string fileName, Encoding encoding, bool gzip, params string[]? lines)
    {
        lines ??= ["かんじ /漢字/感じ/", "かんせい /完成/", "かk /書/", "alpha /Alpha/"];
        var path = PathFor(fileName + (gzip && !fileName.EndsWith(".gz", StringComparison.OrdinalIgnoreCase) ? ".gz" : ""));
        var bytes = encoding.GetBytes(string.Join(Environment.NewLine, lines));
        if (gzip)
        {
            using var file = File.Create(path);
            using var stream = new GZipStream(file, CompressionMode.Compress);
            stream.Write(bytes);
        }
        else
        {
            File.WriteAllBytes(path, bytes);
        }
        return path;
    }

    public static KeyCommand Key(int vkCode, char ch = '\0', bool shift = false, bool control = false) =>
        EngineFunctions.CreateKeyCommand(vkCode, shift, control, ch);

    public SkkDictionarySnapshot LoadDictionary(AppConfig config) =>
        new SkkDicManager(EngineFunctions.ParseDictionaryLine)
            .Load(config.DictionaryPaths, config.UserDictionaryPath);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
