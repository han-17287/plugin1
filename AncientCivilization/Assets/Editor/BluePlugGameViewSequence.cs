#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using Plugin.PlayerCore;
using UnityEditor;
using UnityEngine;

namespace Plugin.AncientCivilization.EditorChecks
{
    // 전조·빗나감·보스 장면도 포함해 현재 Game 뷰 그대로 짧게 기록하는 에디터 전용 도구다.
    public static class BluePlugGameViewSequence
    {
        const string Menu = "PLUGIN/P2/Game 뷰 3초 연속 촬영";
        const int MaxFrames = 36, SamplesPerSecond = 12, LongEdge = 1280;
        static BluePlugVisuals owner;
        static RenderTexture screen, sample;
        static Texture2D pixels;
        static Coroutine recording;
        static int frames;
        static string folder;

        [MenuItem(Menu, true)]
        static bool Available() => EditorApplication.isPlaying && !EditorApplication.isPaused && recording == null;

        [MenuItem(Menu)]
        static void Start()
        {
            owner = UnityEngine.Object.FindAnyObjectByType<BluePlugVisuals>();
            if (!owner || !owner.isActiveAndEnabled) { Debug.LogWarning("먼저 실제 Game 뷰를 여세요."); return; }
            folder = Path.Combine(Application.persistentDataPath, "PlugCaptures",
                "clip-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));
            Directory.CreateDirectory(folder);
            frames = 0;
            EditorApplication.update += CheckOwner;
            EditorApplication.playModeStateChanged += ModeChanged;
            recording = owner.StartCoroutine(Record());
            Debug.Log("Game 뷰에 포커스를 주세요. 1.5초 뒤 3초간 원래 카메라 구도로 촬영합니다. " + folder);
        }

        static IEnumerator Record()
        {
            yield return new WaitForSecondsRealtime(1.5f);
            var end = new WaitForEndOfFrame();
            yield return end;
            double started = EditorApplication.timeSinceStartup, due = 0;
            try
            {
                int width = Screen.width, height = Screen.height;
                if (width < 2 || height < 2 || (long)width * height > 16777216)
                {
                    Debug.LogWarning("Game 뷰 크기를 2픽셀 이상, 16메가픽셀 이하로 설정한 뒤 다시 촬영하세요.");
                    yield break;
                }
                float scale = Mathf.Min(1, LongEdge / (float)Mathf.Max(width, height));
                int w = Mathf.Max(1, Mathf.RoundToInt(width * scale)), h = Mathf.Max(1, Mathf.RoundToInt(height * scale));
                screen = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
                sample = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
                pixels = new Texture2D(w, h, TextureFormat.RGB24, false);
                Rect area = new Rect(0, 0, w, h);
                while (frames < MaxFrames)
                {
                    double elapsed = EditorApplication.timeSinceStartup - started;
                    if (elapsed >= 3 || Screen.width != width || Screen.height != height) break;
                    if (elapsed >= due)
                    {
                        // 프레임을 복제하지 않는다. 늦어진 샘플은 건너뛰고 파일명에 실제 시각을 적는다.
                        due = (Math.Floor(elapsed * SamplesPerSecond) + 1) / SamplesPerSecond;
                        RenderTexture previous = RenderTexture.active;
                        try
                        {
                            ScreenCapture.CaptureScreenshotIntoRenderTexture(screen);
                            Graphics.Blit(screen, sample);
                            RenderTexture.active = sample;
                            pixels.ReadPixels(area, 0, 0, false);
                        }
                        finally { RenderTexture.active = previous; }
                        string file = $"{frames:D2}-{Math.Round(elapsed * 1000):0000}ms.png";
                        File.WriteAllBytes(Path.Combine(folder, file), pixels.EncodeToPNG());
                        frames++;
                    }
                    yield return end;
                }
            }
            finally { Finish(); }
        }

        static void ModeChanged(PlayModeStateChange mode) { if (mode == PlayModeStateChange.ExitingPlayMode) Cancel(); }
        static void CheckOwner() { if (!owner || !owner.isActiveAndEnabled) Cancel(); }
        static void Cancel()
        {
            if (owner && recording != null) owner.StopCoroutine(recording);
            Finish();
        }
        static void Finish()
        {
            if (folder == null) return;
            EditorApplication.update -= CheckOwner;
            EditorApplication.playModeStateChanged -= ModeChanged;
            if (screen) RenderTexture.ReleaseTemporary(screen);
            if (sample) RenderTexture.ReleaseTemporary(sample);
            if (pixels) UnityEngine.Object.DestroyImmediate(pixels);
            screen = sample = null;
            pixels = null;
            recording = null;
            owner = null;
            Debug.Log($"Game 뷰 촬영 종료 · {frames}장 · {folder}");
            folder = null;
        }
    }
}
#endif
