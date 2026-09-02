using Tonono3.SKKEngine;

namespace Tonono3.Tests;

[TestClass]
public sealed class PresentationSnapshotTests
{
    [TestMethod]
    public void InitialStateProducesHiddenSnapshot()
    {
        using var env = new TestEnvironment();
        var config = env.CreateConfig();
        var snapshot = EngineFunctions.CreateUiSnapshot(EngineFunctions.CreateInitialState(), config, 0);

        Assert.IsFalse(snapshot.IsVisible);
        Assert.AreEqual("[？]", snapshot.StatusText);
        Assert.AreEqual("▽", snapshot.Composition);
        Assert.AreEqual("", snapshot.CandidateList);
    }

    [TestMethod]
    public void CompositionAndCompletionAreDerivedWithoutMutatingState()
    {
        using var env = new TestEnvironment();
        var config = env.CreateConfig();
        var state = new EngineState(
            "n", "かん", InputMode.Hiragana, true, false, null, "",
            [], -1, ["かんじ", "かんせい"], 1, []);

        var snapshot = EngineFunctions.CreateUiSnapshot(state, config, 0);

        Assert.IsTrue(snapshot.IsVisible);
        Assert.AreEqual("[あ]", snapshot.StatusText);
        Assert.AreEqual("▽かんせいn", snapshot.Composition);
        Assert.AreEqual("かん", state.CompositionBuffer);
    }

    [TestMethod]
    public void CandidatePageAndNestedRegistrationAreRenderedFromOneState()
    {
        using var env = new TestEnvironment();
        var config = env.CreateConfig();
        var state = new EngineState(
            "", "よみ", InputMode.Hiragana, true, false, null, "",
            ["一", "二", "三", "四", "五", "六", "七", "八"], 7, [], -1,
            [
                new RegistrationFrame("outer", InputMode.Hiragana, "外"),
                new RegistrationFrame("inner", InputMode.Katakana, "内")
            ]);

        var snapshot = EngineFunctions.CreateUiSnapshot(state, config, 0);

        Assert.AreEqual("[[[あ]]]", snapshot.StatusText);
        Assert.IsTrue(snapshot.IsInRegistrationMode);
        Assert.AreEqual("inner", snapshot.RegistrationReading);
        Assert.AreEqual("内", snapshot.RegistrationWord);
        Assert.AreEqual("▼", snapshot.Composition);
        Assert.Contains("[A] : 八", snapshot.CandidateList);
    }

    [TestMethod]
    public void CandidateSelectionKeysDetermineInlineCountAndPageSize()
    {
        using var env = new TestEnvironment();
        var config = env.CreateConfig(candidateSelectionKeys: "ABCD");
        var candidates = new[] { "一", "二", "三", "四", "五", "六", "七", "八", "九" };
        var lastInlineState = new EngineState(
            "", "よみ", InputMode.Hiragana, true, false, null, "",
            candidates, 3, [], -1, []);
        var firstListState = new EngineState(
            "", "よみ", InputMode.Hiragana, true, false, null, "",
            candidates, 4, [], -1, []);

        var lastInline = EngineFunctions.CreateUiSnapshot(lastInlineState, config, 0);
        var firstList = EngineFunctions.CreateUiSnapshot(firstListState, config, 0);

        Assert.AreEqual("▼四", lastInline.Composition);
        Assert.AreEqual("", lastInline.CandidateList);
        Assert.AreEqual("▼", firstList.Composition);
        Assert.Contains("[A] : 五", firstList.CandidateList);
        Assert.Contains(" D  : 八", firstList.CandidateList);
        Assert.DoesNotContain("九", firstList.CandidateList);
    }

    [TestMethod]
    public void ControllerPublishesOneIncreasingSnapshotPerHandledInput()
    {
        using var env = new TestEnvironment();
        var config = env.CreateConfig();
        var dictionary = env.LoadDictionary(config);
        using var context = new ControllerTestContext(config, dictionary);
        var controller = context.Controller;
        var snapshots = context.Ui.Snapshots;
        controller.Start();

        controller.ProcessCommand(TestEnvironment.Key(SkkKeyConstants.VkJ, control: true), null);
        controller.ProcessCommand(TestEnvironment.Key(SkkKeyConstants.VkK, 'K', shift: true), null);

        Assert.HasCount(3, snapshots);
        Assert.AreEqual(1, snapshots[1].Version);
        Assert.AreEqual(2, snapshots[2].Version);
        Assert.AreEqual("[あ]", snapshots[2].StatusText);
        Assert.IsFalse(snapshots[1].IsVisible);
        Assert.IsTrue(snapshots[2].IsVisible);
    }
}
