#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Plugin.AncientCivilization.EditorChecks
{
    public static class AncientWindowsPlayerBuild
    {
        const BuildTarget Target = BuildTarget.StandaloneWindows64;
        const string Menu = "PLUGIN/빌드/Windows 게임 빌드 후 실행";
        const string Pending = "PLUGIN.WindowsPlayerBuild.RunAfterSwitch";
        const string MainScene = "Assets/Scenes/Main.unity";
        const string PlayerName = "PLUGIN.exe";

        [MenuItem(Menu, true)]
        static bool CanBuildAndRun() =>
            Application.platform == RuntimePlatform.WindowsEditor && !BuildPipeline.isBuildingPlayer &&
            !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling &&
            !EditorApplication.isUpdating && !SessionState.GetBool(Pending, false);

        [MenuItem(Menu)]
        static void BuildAndRun()
        {
            try
            {
                EnsureSupported();
                if (EditorUserBuildSettings.activeBuildTarget == Target)
                {
                    BuildAndLaunchPlayer();
                    return;
                }

                // 플랫폼 전환으로 스크립트가 다시 로드되어도 빌드 요청을 한 번만 이어간다.
                SessionState.SetBool(Pending, true);
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, Target))
                    throw new InvalidOperationException("Windows 빌드 대상으로 전환할 수 없습니다.");
                ResumePendingBuild();
            }
            catch (Exception error) { ShowError(error); }
        }

        [InitializeOnLoadMethod]
        static void ResumePendingBuild()
        {
            if (!SessionState.GetBool(Pending, false)) return;
            EditorApplication.update -= FinishSwitch;
            EditorApplication.update += FinishSwitch;
        }

        static void FinishSwitch()
        {
            if (EditorApplication.isUpdating || EditorApplication.isCompiling || BuildPipeline.isBuildingPlayer) return;
            EditorApplication.update -= FinishSwitch;
            if (!SessionState.GetBool(Pending, false)) return;
            SessionState.EraseBool(Pending);
            try { BuildAndLaunchPlayer(); }
            catch (Exception error) { ShowError(error); }
        }

        static void ShowError(Exception error)
        {
            SessionState.EraseBool(Pending);
            Debug.LogException(error);
            EditorUtility.DisplayDialog("Windows 게임 빌드 실패", error.Message, "확인");
        }

        static void BuildAndLaunchPlayer()
        {
            string project = Path.GetDirectoryName(Application.dataPath);
            string run = DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N");
            string player = Path.Combine(project, "Builds", "Windows", run, PlayerName);
            Build(player);
            using (var process = Process.Start(new ProcessStartInfo
            {
                FileName = player,
                WorkingDirectory = Path.GetDirectoryName(player),
                UseShellExecute = false
            }))
            {
                if (process == null) throw new IOException("빌드된 게임을 시작할 수 없습니다.\n" + player);
            }
        }

        public static void BuildFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-pluginWindowsOutput");
            if (i < 0 || i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i+1]) || args[i+1].StartsWith("-"))
                throw new ArgumentException("-pluginWindowsOutput 뒤에 출력할 .exe 경로를 지정하세요.");
            Build(args[i+1]);
        }

        static void EnsureSupported()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, Target))
                throw new InvalidOperationException("Unity Hub에서 이 에디터의 Windows Build Support를 확인하세요.");
        }

        static void Build(string player)
        {
            EnsureSupported();
            if (EditorUserBuildSettings.activeBuildTarget != Target)
                throw new InvalidOperationException("명령줄에서 -buildTarget Win64를 지정하세요.");
            player = Path.GetFullPath(player);
            if (!string.Equals(Path.GetExtension(player), ".exe", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrEmpty(Path.GetFileNameWithoutExtension(player)))
                throw new ArgumentException("파일 이름이 있는 .exe 경로가 필요합니다.");

            // 빌드 설정에서 켠 씬만 가져오며, 본편을 항상 첫 씬으로 실행한다.
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0 || scenes[0] != MainScene)
                throw new InvalidOperationException("빌드 설정의 첫 씬은 " + MainScene + "이어야 합니다.");
            foreach (string scene in scenes)
                if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(scene))
                    throw new FileNotFoundException("빌드할 씬을 열 수 없습니다.", scene);

            string directory = Path.GetDirectoryName(player);
            if (Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any())
                throw new IOException("기존 파일을 보존하기 위해 빈 출력 폴더가 필요합니다.\n" + directory);
            Directory.CreateDirectory(directory);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = player,
                target = Target,
                subtarget = (int)StandaloneBuildSubtarget.Player,
                options = BuildOptions.None
            });
            if (report == null || report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows 게임 빌드에 실패했습니다. Unity 빌드 로그를 확인하세요.");

            string data = Path.Combine(directory, Path.GetFileNameWithoutExtension(player) + "_Data");
            if (!File.Exists(player) || !File.Exists(Path.Combine(directory, "UnityPlayer.dll")) ||
                !Directory.Exists(data) || !Directory.EnumerateFileSystemEntries(data).Any())
                throw new IOException("실행에 필요한 Windows 플레이어 파일이 누락되었습니다.\n" + directory);
            using (var binary = new BinaryReader(File.OpenRead(player)))
            {
                // 실행 파일의 PE 헤더에서 x64 플레이어인지 최종 확인한다.
                if (binary.ReadUInt16() != 0x5a4d) throw new InvalidDataException("Windows 실행 파일의 헤더가 잘못되었습니다.");
                binary.BaseStream.Position = 0x3c;
                binary.BaseStream.Position = binary.ReadUInt32();
                if (binary.ReadUInt32() != 0x4550 || binary.ReadUInt16() != 0x8664)
                    throw new InvalidDataException("Windows x64 플레이어가 필요합니다.\n" + player);
            }
            Debug.Log("Windows x64 게임 빌드 완료\n" + player);
        }
    }
}
#endif
