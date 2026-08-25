using System.Text;
using Tonono3.SKKEngine;

namespace Tonono3.Tests;

[TestClass]
public sealed class DictionaryCharacterizationTests
{
    [TestMethod]
    public void LoadedSystemDictionaryIsImmediatelyAvailableToEngineQueries()
    {
        using var env = new TestEnvironment();
        var main = env.CreateMainDictionary("direct-query.txt", Encoding.UTF8, false, "かんじ /漢字/感じ/");
        var config = env.CreateConfig(dictionaryPaths: [main]);
        var loader = CreateLoader();
        var snapshot = loader.Load(config.DictionaryPaths, config.UserDictionaryPath);

        var candidates = Candidates(snapshot, "かんじ");
        Assert.Contains("漢字", candidates);
    }

    [TestMethod]
    public void UserCandidatesPrecedeMainCandidatesAndDuplicatesAreRemoved()
    {
        using var env = new TestEnvironment();
        var userPath = env.PathFor("user.txt");
        var mainPath = env.CreateMainDictionary("main-candidates.txt", Encoding.UTF8, false, "かんじ /漢字/感じ/");
        File.WriteAllText(userPath, "かんじ /感じ/幹事/\n", Encoding.UTF8);
        var config = env.CreateConfig(dictionaryPaths: [mainPath], userDictionaryPath: userPath);
        var dictionary = Load(config);

        CollectionAssert.AreEqual(
            new[] { "感じ", "幹事", "漢字" },
            Candidates(dictionary, "かんじ"));
    }

    [TestMethod]
    public void AnnotationIsDiscardedWhenLoadingAndSavingUserDictionary()
    {
        using var env = new TestEnvironment();
        var userPath = env.PathFor("user.txt");
        File.WriteAllText(userPath, "かんじ /漢字;annotation/\n", Encoding.UTF8);
        var config = env.CreateConfig(userDictionaryPath: userPath);
        var dictionary = Load(config);

        CollectionAssert.AreEqual(new[] { "漢字" }, dictionary.User["かんじ"].ToArray());
    }

    [TestMethod]
    public void CompletionsAreSortedByLengthThenOrdinalAndExcludeKanaOkuriEntries()
    {
        using var env = new TestEnvironment();
        var main = env.CreateMainDictionary("completion.txt", Encoding.UTF8, false,
            "かな /仮名/", "かなう /叶う/", "かなk /書/", "かなもの /金物/");
        var config = env.CreateConfig(dictionaryPaths: [main]);
        var dictionary = Load(config);

        CollectionAssert.AreEqual(
            new[] { "かな", "かなう", "かなもの" },
            Completions(dictionary, "かな"));
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    public void MainDictionaryLoadsUtf8EucJpAndGzip(bool eucJp, bool gzip)
    {
        using var env = new TestEnvironment();
        var encoding = eucJp ? Encoding.GetEncoding("euc-jp") : Encoding.UTF8;
        var path = env.CreateMainDictionary("encoded.txt", encoding, gzip, "かな /仮名/");
        var config = env.CreateConfig(dictionaryPaths: [path]);

        var dictionary = Load(config);

        CollectionAssert.AreEqual(new[] { "仮名" }, Candidates(dictionary, "かな"));
    }

    [TestMethod]
    public void CommentsBlankAndMalformedLinesAreIgnored()
    {
        using var env = new TestEnvironment();
        var path = env.CreateMainDictionary("invalid.txt", Encoding.UTF8, false,
            "; comment", "", "no-space", "かな not-delimited", "かな /仮名/");
        var config = env.CreateConfig(dictionaryPaths: [path]);

        var dictionary = Load(config);

        CollectionAssert.AreEqual(new[] { "仮名" }, Candidates(dictionary, "かな"));
    }

    private static SkkDictionaryFileLoader CreateLoader() => new(EngineFunctions.LoadDictionary, _ => { });

    private static DictionarySnapshot Load(AppConfig config) =>
        CreateLoader().Load(config.DictionaryPaths, config.UserDictionaryPath);

    private static string[] Candidates(DictionarySnapshot dictionary, string reading) =>
        EngineFunctions.GetCandidates(dictionary, reading);

    private static string[] Completions(DictionarySnapshot dictionary, string prefix) =>
        EngineFunctions.GetCompletions(dictionary, prefix);
}
