using System.Collections.Immutable;
using System.Text;
using Tonono3.SKKEngine;

namespace Tonono3.Tests;

[TestClass]
public sealed class RuntimeInfrastructureTests
{
    [TestMethod]
    public void CompileConfigRejectsIncompleteConfiguration()
    {
        using var env = new TestEnvironment();

        var exception = Assert.ThrowsExactly<InvalidDataException>(() => EngineFunctions.CompileConfig(
            env.PathFor("config"),
            [],
            "",
            "",
            [],
            [],
            [],
            [],
            '\0',
            '\0',
            0,
            [],
            [],
            ""));

        Assert.AreEqual("Empty Romaji table", exception.Message);
    }

    [TestMethod]
    public void ConfigLoaderReadsCandidateSelectionKeysFromYaml()
    {
        using var env = new TestEnvironment();
        var paths = new TestConfigPathProvider(env.PathFor("yaml-config"));
        var yaml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "config.yaml"))
            .Replace(
                "candidateSelectionKeys: ASDFJKL",
                "candidateSelectionKeys: ABCD",
                StringComparison.Ordinal);
        File.WriteAllText(paths.ConfigPath, yaml);
        var loader = new ConfigLoader(paths, EngineFunctions.CompileConfig, _ => { });
        var config = loader.Reload();
        var state = new EngineState(
            "", "よみ", InputMode.Hiragana, true, false, null, "",
            ["一", "二", "三", "四", "五"], 4, [], -1, []);

        var snapshot = EngineFunctions.CreateUiSnapshot(state, config, 0);

        Assert.Contains("[A] : 五", snapshot.CandidateList);
    }

    [TestMethod]
    public async Task UserDictionaryWriterPersistsLatestQueuedSnapshotInBackground()
    {
        using var env = new TestEnvironment();
        var path = env.PathFor("async-user.txt");
        var logger = new DummyLogger();
        var first = EngineFunctions.LoadDictionary([], ["よみ /一/"]);
        var latest  = EngineFunctions.LoadDictionary([], ["よみ /二/一/"]);

        using (var writer = new UserDictionaryWriterFactory(EngineFunctions.SerializeUserDictionary, logger.Log).Create(path))
        {
            writer.Enqueue(first);
            writer.Enqueue(latest);
        }

        await Task.Delay(TimeSpan.FromSeconds(1));

        Assert.Contains("よみ /二/一/", File.ReadAllText(path, Encoding.UTF8));
    }

    [TestMethod]
    public async Task ConfigWatcherDebouncesRepeatedNotificationsAndPublishesOnlyLatestGeneration()
    {
        using var env = new TestEnvironment();
        var config = env.CreateConfig();
        var dictionary = env.LoadDictionary(config);
        var published = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reloadCount = 0;
        using var watcher = new ConfigWatcher(
            new TestConfigPathProvider(env.PathFor("watcher")),
            new StubConfigLoader(() => config).Reload,
            new StubDictionaryLoader((_, _) =>
            {
                Interlocked.Increment(ref reloadCount);
                return dictionary;
            }).Load,
            new DummyLogger().Log);
        watcher.RegisterCallback((generation, _, _) => published.TrySetResult(generation));

        UnsafeAccessors.ScheduleReload(watcher);
        UnsafeAccessors.ScheduleReload(watcher);
        UnsafeAccessors.ScheduleReload(watcher);

        var generation = await published.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.AreEqual(3L, generation);
        Assert.AreEqual(1, reloadCount);
    }

    [TestMethod]
    public async Task ConfigWatcherKeepsLastGoodValueWhenReloadFails()
    {
        var callbackCount = 0;
        using var env = new TestEnvironment();
        var logger = new DummyLogger();
        using var watcher = new ConfigWatcher(
            new TestConfigPathProvider(env.PathFor("watcher")),
            new StubConfigLoader(() => throw new InvalidDataException("invalid config")).Reload,
            new StubDictionaryLoader((_, _) => throw new AssertFailedException("Dictionary must not load.")).Load,
            logger.Log);
        watcher.RegisterCallback((_, _, _) => Interlocked.Increment(ref callbackCount));

        UnsafeAccessors.ScheduleReload(watcher);
        await Task.Delay(800);

        Assert.AreEqual(0, callbackCount);
        Assert.Contains(message => message.Contains("invalid config", StringComparison.Ordinal), logger.Messages);
    }

    [TestMethod]
    public async Task ConfigWatcherDoesNotPublishAfterDispose()
    {
        using var env = new TestEnvironment();
        var config = env.CreateConfig();
        var dictionary = env.LoadDictionary(config);
        var callbackCount = 0;
        var watcher = new ConfigWatcher(
            new TestConfigPathProvider(env.PathFor("watcher")),
            new StubConfigLoader(() => config).Reload,
            new StubDictionaryLoader((_, _) => dictionary).Load,
            new DummyLogger().Log);
        watcher.RegisterCallback((_, _, _) => Interlocked.Increment(ref callbackCount));

        UnsafeAccessors.ScheduleReload(watcher);
        watcher.Dispose();
        await Task.Delay(800);

        Assert.AreEqual(0, callbackCount);
    }

    [TestMethod]
    public void ControllerAppliesPendingRuntimeOnlyAtNextHandlerBoundary()
    {
        using var env = new TestEnvironment();
        var first = env.CreateConfig(viCompatibleApps: ["first.exe"]);
        var second = env.CreateConfig(viCompatibleApps: ["second.exe"]);
        var firstDictionary = env.LoadDictionary(first);
        var secondDictionary = env.LoadDictionary(second);
        using var context = new ControllerTestContext(first, firstDictionary);
        var controller = context.Controller;
        controller.Start();

        context.Watcher.Publish(1, second, secondDictionary);
        Assert.AreSame(first, context.Session.CurrentConfig);

        controller.ProcessCommand(TestEnvironment.Key(SkkKeyConstants.VkLeft), null);
        Assert.AreSame(second, context.Session.CurrentConfig);
    }

    [TestMethod]
    public async Task ControllerSerializesConcurrentHandlersAndRejectsInputAfterDispose()
    {
        using var env = new TestEnvironment();
        var config = env.CreateConfig();
        var dictionary = env.LoadDictionary(config);
        var context = new ControllerTestContext(config, dictionary);
        var controller = context.Controller;
        controller.Start();
        var tasks = Enumerable.Range(0, 50)
            .Select(_ => Task.Run(() => controller.ProcessCommand(
                TestEnvironment.Key(SkkKeyConstants.VkLeft), null)))
            .ToArray();

        await Task.WhenAll(tasks);
        context.Dispose();

        Assert.IsTrue(tasks.All(task => task.Result == false));
        Assert.IsFalse(controller.ProcessCommand(
            TestEnvironment.Key(SkkKeyConstants.VkJ, control: true), null));
        Assert.AreEqual(1, context.Watcher.DisposeCount);
        Assert.AreEqual(1, context.Hook.DisposeCount);
    }

    [TestMethod]
    public void ApplicationCoordinatorClosesOwnedGraphExactlyOnce()
    {
        using var env = new TestEnvironment();
        var config = env.CreateConfig();
        var context = new ControllerTestContext(config, env.LoadDictionary(config));
        var applicationControl = new FakeApplicationControl();
        var coordinator = new ApplicationCoordinator(
            context.Controller,
            applicationControl.Initialize);

        coordinator.Start(null);
        coordinator.Dispose();
        coordinator.Dispose();

        Assert.AreEqual(1, applicationControl.InitializeCount);
        Assert.AreEqual(1, context.Ui.CloseCount);
        Assert.AreEqual(1, context.Menu.DisposeCount);
        Assert.AreEqual(1, context.Watcher.DisposeCount);
        Assert.AreEqual(1, context.Hook.DisposeCount);
    }
}
