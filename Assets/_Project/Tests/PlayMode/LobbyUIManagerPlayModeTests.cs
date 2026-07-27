using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wake.Core;
using Wake.UI;

namespace Wake.Tests
{
    /// Regression coverage for the Lobby singleton wiring: a boot from
    /// Bootstrap.unity must additively load Lobby Scene.unity and leave
    /// LobbyUIManager.Instance pointing at the one live LobbyUIManager, with
    /// its three StartScene buttons each holding exactly one runtime
    /// listener. This guards against the invariant gap where
    /// EnsureInitialized() could finish successfully (IsInitialized == true,
    /// buttons rebound) while Instance stayed null because only Awake()
    /// used to assign it.
    public class LobbyUIManagerPlayModeTests
    {
        private const string BootstrapScenePath =
            "Assets/_Project/Scenes/Bootstrap.unity";

        private static readonly string[] ButtonPaths =
        {
            "StartScene/Start Game Btn",
            "StartScene/Settings Btn",
            "StartScene/Continue Btn"
        };

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (GameSystemsBootstrap bootstrap in
                     Object.FindObjectsByType<GameSystemsBootstrap>(
                         FindObjectsSortMode.None))
            {
                Object.Destroy(bootstrap.gameObject);
            }

            yield return null;

            Scene bootstrapScene =
                SceneManager.GetSceneByPath(BootstrapScenePath);
            Scene lobbyScene = SceneManager.GetSceneByName("Lobby Scene");
            bool needsCleanup =
                (bootstrapScene.IsValid() && bootstrapScene.isLoaded) ||
                (lobbyScene.IsValid() && lobbyScene.isLoaded);
            if (!needsCleanup)
            {
                yield break;
            }

            Scene scratch = SceneManager.CreateScene(
                $"LobbyUiManagerTestCleanup_{Time.frameCount}");
            SceneManager.SetActiveScene(scratch);

            if (bootstrapScene.IsValid() && bootstrapScene.isLoaded)
            {
                AsyncOperation unloadBootstrap =
                    SceneManager.UnloadSceneAsync(bootstrapScene);
                while (unloadBootstrap != null && !unloadBootstrap.isDone)
                {
                    yield return null;
                }
            }

            if (lobbyScene.IsValid() && lobbyScene.isLoaded)
            {
                AsyncOperation unloadLobby =
                    SceneManager.UnloadSceneAsync(lobbyScene);
                while (unloadLobby != null && !unloadLobby.isDone)
                {
                    yield return null;
                }
            }
        }

        [UnityTest]
        public IEnumerator BootstrapLoad_InitializesSingletonAndBindsButtonsOnce()
        {
            yield return LoadBootstrapAndWaitForLobby();

            Assert.That(LobbyUIManager.Instance, Is.Not.Null);
            Assert.That(LobbyUIManager.Instance.IsInitialized, Is.True);

            LobbyUIManager[] instances =
                Object.FindObjectsByType<LobbyUIManager>(
                    FindObjectsSortMode.None);
            Assert.That(instances, Has.Length.EqualTo(1));
            Assert.That(LobbyUIManager.Instance, Is.SameAs(instances[0]));

            foreach (string path in ButtonPaths)
            {
                Assert.That(
                    RuntimeListenerCount(RequireButton(path)),
                    Is.EqualTo(1),
                    path);
            }
        }

        [UnityTest]
        public IEnumerator
            EnsureInitialized_ReestablishesInstanceWhenClearedExternally()
        {
            yield return LoadBootstrapAndWaitForLobby();

            LobbyUIManager live = LobbyUIManager.Instance;
            Assert.That(live, Is.Not.Null);

            // Reproduce the reported bug precondition directly: the static
            // singleton is cleared (as OnDestroy would do, or as any other
            // external event might) while the object itself stays alive and
            // fully initialized.
            SetInstance(null);
            Assert.That(LobbyUIManager.Instance, Is.Null);
            Assert.That(
                live.IsInitialized,
                Is.True,
                "The object should still report itself initialized.");

            Assert.That(live.EnsureInitialized(), Is.True);

            Assert.That(
                LobbyUIManager.Instance,
                Is.SameAs(live),
                "EnsureInitialized() must restore the Instance invariant.");
            foreach (string path in ButtonPaths)
            {
                Assert.That(
                    RuntimeListenerCount(RequireButton(path)),
                    Is.EqualTo(1),
                    path);
            }
        }

        private static void SetInstance(LobbyUIManager value)
        {
            PropertyInfo property = typeof(LobbyUIManager).GetProperty(
                nameof(LobbyUIManager.Instance));
            property.GetSetMethod(true).Invoke(null, new object[] { value });
        }

        private static IEnumerator LoadBootstrapAndWaitForLobby()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(
                BootstrapScenePath,
                LoadSceneMode.Single);
            Assert.That(
                load,
                Is.Not.Null,
                $"{BootstrapScenePath} 로드를 시작하지 못했습니다.");

            // DialogueController's own Awake() runs from the Bootstrap scene
            // before Lobby Scene (and its Canvas) has finished loading
            // additively; this benign, pre-existing transient error is
            // unrelated to the Lobby singleton wiring under test here.
            LogAssert.Expect(
                LogType.Error,
                "DialogueController could not find Canvas in scene.");

            while (!load.isDone)
            {
                yield return null;
            }

            float timeout = Time.realtimeSinceStartup + 5f;
            while (LobbyUIManager.Instance == null &&
                   Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            yield return null;

            Assert.That(
                LobbyUIManager.Instance,
                Is.Not.Null,
                "Lobby Scene을 additively 로드하지 못했습니다.");
        }

        private static GameObject RequireObject(string path)
        {
            GameObject canvas = GameObject.Find("Canvas");
            Assert.That(canvas, Is.Not.Null, "Canvas 루트를 찾지 못했습니다.");
            Transform target = canvas.transform.Find(path);
            Assert.That(target, Is.Not.Null, $"Canvas/{path}를 찾지 못했습니다.");
            return target.gameObject;
        }

        private static Button RequireButton(string path)
        {
            Button button = RequireObject(path).GetComponent<Button>();
            Assert.That(
                button,
                Is.Not.Null,
                $"Canvas/{path}에 Button 컴포넌트가 없습니다.");
            return button;
        }

        private static int RuntimeListenerCount(Button button)
        {
            FieldInfo callsField = typeof(UnityEventBase).GetField(
                "m_Calls",
                BindingFlags.NonPublic | BindingFlags.Instance);
            object invokableCallList = callsField.GetValue(button.onClick);
            FieldInfo runtimeCallsField =
                invokableCallList.GetType().GetField(
                    "m_RuntimeCalls",
                    BindingFlags.NonPublic | BindingFlags.Instance);
            var runtimeCalls =
                (IList)runtimeCallsField.GetValue(invokableCallList);
            return runtimeCalls.Count;
        }
    }
}
