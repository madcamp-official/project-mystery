#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Wake.Core;
using Wake.UI;
using Object = UnityEngine.Object;

namespace Wake.Editor
{
    internal sealed class UiVisualQaFrameCapture : MonoBehaviour
    {
        public void Save(string path)
        {
            StartCoroutine(SaveAtEndOfFrame(path));
        }

        private static IEnumerator SaveAtEndOfFrame(string path)
        {
            yield return new WaitForSecondsRealtime(0.55f);
            yield return new WaitForEndOfFrame();
            Texture2D capture = ScreenCapture.CaptureScreenshotAsTexture(1);
            if (capture == null)
            {
                Debug.LogError($"UI visual QA could not capture {path}.");
                yield break;
            }
            File.WriteAllBytes(path, capture.EncodeToPNG());
            Object.Destroy(capture);
        }
    }

    /// <summary>
    /// Captures the actual Game view for the UI screens that can be opened
    /// without mutating production content. Run from the menu or with
    /// -executeMethod Wake.Editor.UiVisualQaCapture.Begin.
    /// </summary>
    [InitializeOnLoad]
    public static class UiVisualQaCapture
    {
        private const string RunningKey = "Wake.UiVisualQa.Running";
        private const string StartStageKey = "Wake.UiVisualQa.StartStage";
        private static string OutputFolder => Path.Combine(
            Directory.GetParent(Application.dataPath)?.FullName ??
            Application.dataPath,
            "Logs",
            "VisualQA");
        private static int stage;
        private static double nextStageTime;
        private static bool inTick;

        static UiVisualQaCapture()
        {
            if (SessionState.GetBool(RunningKey, false))
            {
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
            }
        }

        [MenuItem("Wake/QA/Capture UI Screens")]
        public static void Begin()
        {
            BeginAtStage(0);
        }

        public static void BeginEvidenceAndEnding()
        {
            BeginAtStage(4);
        }

        private static void BeginAtStage(int startStage)
        {
            Directory.CreateDirectory(OutputFolder);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetInt(StartStageKey, startStage);
            stage = startStage;
            nextStageTime = 0d;
            EditorSceneManager.OpenScene(
                "Assets/_Project/Scenes/UI/UI Basic Scene.unity");
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.EnterPlaymode();
            }
        }

        private static void Tick()
        {
            if (inTick)
            {
                return;
            }
            inTick = true;
            try
            {
                TickCore();
            }
            finally
            {
                inTick = false;
            }
        }

        private static void TickCore()
        {
            if (!SessionState.GetBool(RunningKey, false))
            {
                EditorApplication.update -= Tick;
                return;
            }
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            if (nextStageTime <= 0d)
            {
                stage = SessionState.GetInt(StartStageKey, stage);
                Screen.SetResolution(1920, 1080, false);
                nextStageTime = EditorApplication.timeSinceStartup + 1.2d;
                return;
            }
            if (EditorApplication.timeSinceStartup < nextStageTime)
            {
                return;
            }
            nextStageTime = EditorApplication.timeSinceStartup + 1.2d;

            switch (stage++)
            {
                case 0:
                    Capture("01_title_1920x1080.png");
                    break;
                case 1:
                    Object.FindFirstObjectByType<SaveSlotSelectionController>()
                        ?.Open();
                    Capture("02_save_slots_1920x1080.png");
                    break;
                case 2:
                    UIManager.Instance?.ShowIngame();
                    Capture("03_ingame_1920x1080.png");
                    break;
                case 3:
                    UIManager.Instance?.ShowMap();
                    Capture("04_map_1920x1080.png");
                    break;
                case 4:
                    UIManager.Instance?.ShowEvidence();
                    Capture("05_evidence_1920x1080.png");
                    break;
                case 5:
                    ShowEnding();
                    Capture("06_ending_1920x1080.png");
                    break;
                default:
                    Finish();
                    break;
            }
        }

        private static void ShowEnding()
        {
            ProductionEndingUIController ending =
                Object.FindFirstObjectByType<ProductionEndingUIController>(
                    FindObjectsInactive.Include);
            MethodInfo show = typeof(ProductionEndingUIController).GetMethod(
                "Show",
                BindingFlags.Instance | BindingFlags.NonPublic);
            show?.Invoke(
                ending,
                new object[]
                {
                    FinalAccusationResolver.CompleteEndingId,
                    "모든 진실을 확인했습니다."
                });
        }

        private static void Capture(string fileName)
        {
            UiVisualQaFrameCapture runner =
                Object.FindFirstObjectByType<UiVisualQaFrameCapture>();
            if (runner == null)
            {
                GameObject host = new("UI Visual QA Frame Capture");
                runner = host.AddComponent<UiVisualQaFrameCapture>();
                Object.DontDestroyOnLoad(host);
            }
            runner.Save(Path.GetFullPath(Path.Combine(OutputFolder, fileName)));
        }

        private static void Finish()
        {
            SessionState.SetBool(RunningKey, false);
            EditorApplication.update -= Tick;
            EditorApplication.ExitPlaymode();
            if (Application.isBatchMode ||
                Array.Exists(
                    Environment.GetCommandLineArgs(),
                    value => value == "-executeMethod"))
            {
                EditorApplication.delayCall += () => EditorApplication.Exit(0);
            }
        }
    }
}
#endif
