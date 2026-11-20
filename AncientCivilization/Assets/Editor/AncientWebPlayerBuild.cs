#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Plugin.AncientCivilization.EditorChecks
{
    public static class AncientWebPlayerBuild
    {
        const string Menu = "PLUGIN/빌드/WebGL 플레이어 내보내기";
        const string MainScene = "Assets/Scenes/Main.unity";

        [MenuItem(Menu, true)]
        static bool CanExport() => !EditorApplication.isPlayingOrWillChangePlaymode &&
            !EditorApplication.isCompiling;

        [MenuItem(Menu)]
        static void Export()
        {
            string parent = EditorUtility.OpenFolderPanel("WebGL 빌드를 저장할 위치", "", "");
            if (string.IsNullOrEmpty(parent)) return;
            string output = Path.Combine(parent, $"PLUGIN-v{PlayerSettings.bundleVersion}-WebGL");
            try
            {
                Build(output);
                EditorUtility.RevealInFinder(Path.Combine(output, "index.html"));
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorUtility.DisplayDialog("WebGL 내보내기 실패", error.Message, "확인");
            }
        }

        // 명령줄에서는 -buildTarget WebGL과 함께 이 메서드를 실행한다.
        public static void BuildFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-pluginWebOutput");
            if (index < 0 || index + 1 == args.Length || args[index + 1].StartsWith("-"))
                throw new ArgumentException("-pluginWebOutput 뒤에 WebGL 출력 경로를 지정하세요.");
            Build(args[index + 1]);
        }

        static void Build(string output)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                throw new InvalidOperationException("Unity Hub에서 이 에디터에 Web Build Support를 설치하세요.");

            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length == 0 || scenes[0] != MainScene)
                throw new InvalidOperationException("빌드의 첫 씬을 Assets/Scenes/Main.unity로 설정하세요.");

            output = Path.GetFullPath(output);
            if (File.Exists(output) || (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any()))
                throw new IOException($"기존 파일을 보존합니다. 비어 있는 출력 경로를 선택하세요.\n{output}");
            Directory.CreateDirectory(output);

            // 브라우저가 서버의 압축 헤더 없이도 Unity 빌드 파일을 열 수 있게 한다.
            var previousFormat = PlayerSettings.WebGL.compressionFormat;
            bool previousFallback = PlayerSettings.WebGL.decompressionFallback;
            try
            {
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
                PlayerSettings.WebGL.decompressionFallback = true;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = output,
                    target = BuildTarget.WebGL,
                    options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException($"WebGL 빌드 결과 {report.summary.result}. Unity Console을 확인하세요.");
                if (!File.Exists(Path.Combine(output, "index.html")))
                    throw new FileNotFoundException("빌드 결과에 index.html이 없습니다.");
            }
            finally
            {
                PlayerSettings.WebGL.compressionFormat = previousFormat;
                PlayerSettings.WebGL.decompressionFallback = previousFallback;
            }
            Debug.Log($"WebGL 플레이어 빌드 경로 {output}");
        }
    }
}
#endif
