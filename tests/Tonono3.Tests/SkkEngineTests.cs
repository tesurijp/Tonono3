using System.Text;
using System.Diagnostics;
using Tonono3.AutoDefined;
using Tonono3.SkkEngine;

namespace Tonono3.Tests;

internal static class SkkKeyConstants
{
    internal const int VkTab = 0x09;
    internal const int VkReturn = 0x0D;
    internal const int VkEscape = 0x1B;
    internal const int VkSpace = 0x20;
    internal const int VkLeft = 0x25;
    internal const int VkA = 0x41;
    internal const int VkJ = 0x4A;
    internal const int VkK = 0x4B;
    internal const int VkL = 0x4C;
    internal const int VkQ = 0x51;
    internal const int VkSlash = 0xBF;
}

[TestClass]
public sealed class SkkEngineTests
{
    [TestMethod]
    public void TransitionDoesNotMutateInputState()
    {
        using var env = new TestEnvironment();
        var runner = CreateRunner(env.CreateConfig());
        var initial = runner.State;

        var result = runner.Process(Key(SkkKeyConstants.VkJ, control: true));

        Assert.AreEqual(InputMode.Direct, initial.Mode);
        Assert.AreEqual(InputMode.Hiragana, result.State.Mode);
        Assert.IsTrue(result.IsHandled);
    }

    [TestMethod]
    public void KanaInputReturnsCommitEffect()
    {
        using var env = new TestEnvironment();
        var runner = CreateRunner(env.CreateConfig());
        runner.Process(Key(SkkKeyConstants.VkJ, control: true));

        var result = runner.Process(Key(SkkKeyConstants.VkA, 'a'));

        Assert.AreEqual("あ", result.Effects.OfType<CommitTextEffect>().Single().Text);
    }

    [TestMethod]
    public void ControlJTurnsOffImeWhenEnteringKanaMode()
    {
        using var env = new TestEnvironment();
        var runner = CreateRunner(env.CreateConfig());

        var result = runner.Process(Key(SkkKeyConstants.VkJ, control: true));

        Assert.AreEqual(InputMode.Hiragana, result.State.Mode);
        Assert.HasCount(1, result.Effects.OfType<TurnOffImeEffect>());
    }

    [TestMethod]
    public void EnterPassesThroughImmediatelyAfterDirectKanaCommit()
    {
        using var env = new TestEnvironment();
        var runner = CreateRunner(env.CreateConfig());
        runner.Process(Key(SkkKeyConstants.VkJ, control: true));
        var kana = runner.Process(Key(SkkKeyConstants.VkA, 'a'));

        var enter = runner.Process(Key(SkkKeyConstants.VkReturn));

        Assert.AreEqual("あ", kana.Effects.OfType<CommitTextEffect>().Single().Text);
        Assert.AreEqual("", kana.State.RomajiBuffer);
        Assert.AreEqual("", kana.State.CompositionBuffer);
        Assert.IsFalse(enter.IsHandled);
        Assert.IsEmpty(enter.Effects);
    }

    [TestMethod]
    public void CandidateCommitUpdatesDictionaryAndReturnsEffectsInOrder()
    {
        using var env = new TestEnvironment();
        var main = env.CreateMainDictionary("fsharp-main.txt", Encoding.UTF8, false, "か /蚊/科/");
        var runner = CreateRunner(env.CreateConfig(dictionaryPaths: [main]));
        runner.Process(Key(SkkKeyConstants.VkJ, control: true));
        runner.Process(Key(SkkKeyConstants.VkK, 'K', shift: true));
        runner.Process(Key(SkkKeyConstants.VkA, 'a'));
        runner.Process(Key(SkkKeyConstants.VkSpace, ' '));

        var result = runner.Process(Key(SkkKeyConstants.VkReturn));

        Assert.IsInstanceOfType<PersistUserDictionaryEffect>(result.Effects[0]);
        Assert.AreEqual("蚊", ((CommitTextEffect)result.Effects[1]).Text);

        var dictionaryUser = EngineFunctions.SerializeUserDictionary(result.Dictionary);
        Assert.AreEqual("か /蚊/", dictionaryUser.FirstOrDefault());
    }

    [TestMethod]
    public void ConfiguredSelectionKeyCommitsItsCandidateOnTheSecondPage()
    {
        using var env = new TestEnvironment();
        var main = env.CreateMainDictionary(
            "custom-selection-keys.txt",
            Encoding.UTF8,
            false,
            "か /一/二/三/四/五/六/七/八/九/");
        var runner = CreateRunner(env.CreateConfig(
            dictionaryPaths: [main],
            candidateSelectionKeys: "ABCD"));
        runner.Process(Key(SkkKeyConstants.VkJ, control: true));
        runner.Process(Key(SkkKeyConstants.VkK, 'K', shift: true));
        runner.Process(Key(SkkKeyConstants.VkA, 'a'));
        runner.Process(Key(SkkKeyConstants.VkSpace, ' '));
        runner.Process(Key(SkkKeyConstants.VkSpace, ' '));
        runner.Process(Key(SkkKeyConstants.VkSpace, ' '));
        runner.Process(Key(SkkKeyConstants.VkSpace, ' '));
        runner.Process(Key(SkkKeyConstants.VkSpace, ' '));

        var result = runner.Process(Key('B', 'b'));

        Assert.AreEqual("六", result.Effects.OfType<CommitTextEffect>().Single().Text);
    }

    [TestMethod]
    public void MissingCandidateStartsRegistrationAndReturnsLog()
    {
        using var env = new TestEnvironment();
        var empty = env.CreateMainDictionary("empty.txt", Encoding.UTF8, false, "; empty");
        var runner = CreateRunner(env.CreateConfig(dictionaryPaths: [empty]));
        runner.Process(Key(SkkKeyConstants.VkJ, control: true));
        runner.Process(Key(SkkKeyConstants.VkK, 'K', shift: true));
        runner.Process(Key(SkkKeyConstants.VkA, 'a'));

        var result = runner.Process(Key(SkkKeyConstants.VkSpace, ' '));

        Assert.IsNotEmpty(result.Effects.OfType<WriteLogEffect>());
        Assert.AreEqual("か", result.State.RegistrationStack.Single().Reading);
    }

    [TestMethod]
    public void SpaceDoesNotStartConversionBeforeKanaIsConfirmed()
    {
        using var env = new TestEnvironment();
        var runner = CreateRunner(env.CreateConfig());
        runner.Process(Key(SkkKeyConstants.VkJ, control: true));
        runner.Process(Key(SkkKeyConstants.VkK, 'K', shift: true));

        var result = runner.Process(Key(SkkKeyConstants.VkSpace, ' '));

        Assert.IsFalse(result.IsHandled);
        Assert.AreEqual("k", result.State.RomajiBuffer);
        Assert.AreEqual("", result.State.CompositionBuffer);
        Assert.IsEmpty(result.State.RegistrationStack);
        Assert.IsEmpty(result.Effects);
    }

    [TestMethod]
    public void ModeChangesAreRepresentedWithoutCompositionFlags()
    {
        using var env = new TestEnvironment();
        var runner = CreateRunner(env.CreateConfig());

        Assert.AreEqual(InputMode.Hiragana, runner.Process(Key(SkkKeyConstants.VkJ, control: true)).State.Mode);
        Assert.AreEqual(InputMode.Katakana, runner.Process(Key(SkkKeyConstants.VkQ, 'q')).State.Mode);
        Assert.AreEqual(InputMode.Hiragana, runner.Process(Key(SkkKeyConstants.VkQ, 'q')).State.Mode);
        Assert.AreEqual(InputMode.Direct, runner.Process(Key(SkkKeyConstants.VkL, 'l')).State.Mode);
        runner.Process(Key(SkkKeyConstants.VkJ, control: true));
        Assert.AreEqual(InputMode.Zenkaku, runner.Process(Key(SkkKeyConstants.VkL, 'L', shift: true)).State.Mode);
    }

    [TestMethod]
    public void LowercaseLDisablesAfterKanaInput()
    {
        using var env = new TestEnvironment();
        var runner = CreateRunner(env.CreateConfig());
        runner.Process(Key(SkkKeyConstants.VkJ, control: true));
        var kana = runner.Process(Key(SkkKeyConstants.VkA, 'a'));

        Assert.AreEqual("あ", kana.Effects.OfType<CommitTextEffect>().Single().Text);

        var disabled = runner.Process(Key(SkkKeyConstants.VkL, 'l'));

        Assert.HasCount(1, disabled.Effects.OfType<TurnOffImeEffect>());
        Assert.IsInstanceOfType<TurnOffImeEffect>(disabled.Effects[0]);

        Assert.IsTrue(disabled.IsHandled);
        Assert.AreEqual(InputMode.Direct, disabled.State.Mode);
    }

    [TestMethod]
    public void CompletionSelectionIsCreatedAndCancelledAsOneState()
    {
        using var env = new TestEnvironment();
        var main = env.CreateMainDictionary("completion-main.txt", Encoding.UTF8, false, "alpha /Alpha/");
        var runner = CreateRunner(env.CreateConfig(dictionaryPaths: [main]));
        runner.Process(Key(SkkKeyConstants.VkJ, control: true));
        runner.Process(Key(SkkKeyConstants.VkSlash, '/'));
        runner.Process(Key(SkkKeyConstants.VkA, 'a'));

        var completed = runner.Process(Key(SkkKeyConstants.VkTab));
        Assert.AreEqual(0, completed.State.CompletionIndex);
        Assert.IsNotEmpty(completed.State.Completions);

        var cancelled = runner.Process(Key(SkkKeyConstants.VkEscape));
        Assert.AreEqual(-1, cancelled.State.CompletionIndex);
        Assert.AreEqual("", cancelled.State.CompositionBuffer);
    }

    [TestMethod]
    public void ViCompatibleEscapeDisablesEngineAndPassesThrough()
    {
        using var env = new TestEnvironment();
        var runner = CreateRunner(env.CreateConfig(viCompatibleApps: ["vim.exe"]));
        runner.Process(Key(SkkKeyConstants.VkJ, control: true));

        var result = runner.Process(Key(SkkKeyConstants.VkEscape), @"C:\tools\vim.exe");

        Assert.IsFalse(result.IsHandled);
        Assert.AreEqual(InputMode.Direct, result.State.Mode);
    }

    [TestMethod]
    [TestCategory("Performance")]
    public void LargeImmutableSystemDictionarySupportsRepeatedQueriesWithoutReplacement()
    {
        var main = Enumerable.Range(0, 20_000)
            .Select(index => $"よみ{index:D5} /候補{index}/");
        var dictionary = EngineFunctions.LoadDictionary(main, []);
        var stopwatch = Stopwatch.StartNew();

        for (var index = 0; index < 1_000; index++)
        {
            var candidates = EngineFunctions.GetCandidates(dictionary, $"よみ{index:D5}");
            Assert.AreEqual($"候補{index}", candidates.Single());
        }

        var dictionaryUser = EngineFunctions.SerializeUserDictionary(dictionary);
        stopwatch.Stop();
        Assert.IsLessThan(TimeSpan.FromSeconds(1), stopwatch.Elapsed, $"Queries took {stopwatch.Elapsed}.");
        Assert.IsEmpty(dictionaryUser);
    }

    private static KeyCommand Key(int vkCode, char ch = '\0', bool shift = false, bool control = false) =>
        TestEnvironment.Key(vkCode, ch, shift, control);

    private static Runner CreateRunner(AppConfig config) => new(
        config,
        EngineFunctions.CreateInitialState,
        EngineFunctions.LoadDictionary,
        EngineFunctions.ProcessKey);

    private sealed class Runner
    {
        private readonly AppConfig config;
        private DictionarySnapshot dictionary;

        public Runner(
            AppConfig source,
            CreateInitialStateFunc createInitialState,
            LoadDictionaryFunc loadDictionary,
            ProcessKeyFunc processKey)
        {
            config = source;
            dictionary = new SkkDictionaryFileLoader(loadDictionary, _ => { })
                .Load(source.DictionaryPaths, source.UserDictionaryPath);
            State = createInitialState();
            this.processKey = processKey;
        }

        private readonly ProcessKeyFunc processKey;

        public EngineState State { get; private set; }

        public TransitionResult Process(KeyCommand command, string? activeProcessPath = null)
        {
            var result = processKey(
                State, config, dictionary,
                command, activeProcessPath!);
            State = result.State;
            dictionary = result.Dictionary;
            return result;
        }
    }
}
