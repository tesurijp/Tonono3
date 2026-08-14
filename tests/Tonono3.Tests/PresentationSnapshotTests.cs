using Tonono3.SKKEngine;

namespace Tonono3.Tests;

[TestClass]
public sealed class PresentationSnapshotTests
{
    [TestMethod]
    public void InitialStateProducesHiddenSnapshot()
    {
        var snapshot = EngineFunctions.CreateUiSnapshot(EngineFunctions.CreateInitialState());

        Assert.IsFalse(snapshot.IsVisible);
        Assert.AreEqual("[？]", snapshot.StatusText);
        Assert.AreEqual("▽", snapshot.Composition);
        Assert.AreEqual("", snapshot.CandidateList);
    }

    [TestMethod]
    public void CompositionAndCompletionAreDerivedWithoutMutatingState()
    {
        var state = new EngineState(
            "n", "かん", InputMode.Hiragana, true, false, null, "",
            [], -1, ["かんじ", "かんせい"], 1, []);

        var snapshot = EngineFunctions.CreateUiSnapshot(state);

        Assert.IsTrue(snapshot.IsVisible);
        Assert.AreEqual("[あ]", snapshot.StatusText);
        Assert.AreEqual("▽かんせいn", snapshot.Composition);
        Assert.AreEqual("かん", state.CompositionBuffer);
    }

    [TestMethod]
    public void CandidatePageAndNestedRegistrationAreRenderedFromOneState()
    {
        var state = new EngineState(
            "", "よみ", InputMode.Hiragana, true, false, null, "",
            ["一", "二", "三", "四", "五", "六", "七", "八"], 4, [], -1,
            [
                new RegistrationFrame("outer", InputMode.Hiragana, "外"),
                new RegistrationFrame("inner", InputMode.Katakana, "内")
            ]);

        var snapshot = EngineFunctions.CreateUiSnapshot(state);

        Assert.AreEqual("[[[あ]]]", snapshot.StatusText);
        Assert.IsTrue(snapshot.IsInRegistrationMode);
        Assert.AreEqual("inner", snapshot.RegistrationReading);
        Assert.AreEqual("内", snapshot.RegistrationWord);
        Assert.AreEqual("▼", snapshot.Composition);
        Assert.Contains("[J] : 五", snapshot.CandidateList);
    }

    [TestMethod]
    public void ControllerPublishesOneIncreasingSnapshotPerHandledInput()
    {
        using var env = new TestEnvironment();
        var config = env.CreateConfig();
        var dictionary = env.LoadDictionary(config);
        using var context = new ControllerTestContext(config, dictionary);
        var controller = context.Controller;
        var snapshots = new List<SkkUiSnapshot>();
        controller.UiUpdated += snapshots.Add;

        controller.ProcessCommand(TestEnvironment.Key(SkkKeyConstants.VkJ, control: true), null);
        controller.ProcessCommand(TestEnvironment.Key(SkkKeyConstants.VkK, 'K', shift: true), null);

        Assert.HasCount(2, snapshots);
        Assert.AreEqual(1, snapshots[0].Version);
        Assert.AreEqual(2, snapshots[1].Version);
        Assert.AreEqual("[あ]", snapshots[1].StatusText);
        Assert.IsTrue(snapshots[1].IsVisible);
    }
}
