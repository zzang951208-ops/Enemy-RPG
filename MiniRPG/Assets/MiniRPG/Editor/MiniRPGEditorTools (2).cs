using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiniRPG.EditorTools
{
    /// <summary>
    /// 상단 메뉴 [MiniRPG]에서 씬 준비, 바로 플레이, 실행 파일 빌드를 한 번에 할 수 있게 해주는 에디터 도구.
    /// (Editor 폴더에 있으므로 빌드된 게임에는 포함되지 않음)
    /// </summary>
    [InitializeOnLoad]
    public static class MiniRPGEditorTools
    {
        const string ScenePath = "Assets/MiniRPG/Scenes/Main.unity";
        const string WelcomeKey = "MiniRPG_WelcomeShown_v2";

        static MiniRPGEditorTools()
        {
            EditorApplication.delayCall += Welcome;
        }

        static void Welcome()
        {
            string key = WelcomeKey + "_" + Application.dataPath.GetHashCode();
            if (EditorPrefs.GetBool(key, false)) return;
            EditorPrefs.SetBool(key, true);
            if (EditorUtility.DisplayDialog("새벽빛 전설 설치 완료",
                    "스크립트를 불러왔습니다.\n\n상단 메뉴 [MiniRPG] → [▶ 게임 시작]을 누르면 게임 씬을 자동으로 만들고 바로 실행합니다.\n\n실행 파일(.exe)이 필요하면 [MiniRPG] → [실행 파일 만들기]를 누르세요.",
                    "지금 게임 시작", "나중에"))
            {
                PlayGame();
            }
        }

        [MenuItem("MiniRPG/▶ 게임 시작", false, 0)]
        public static void PlayGame()
        {
            if (EditorApplication.isPlaying) { EditorApplication.isPlaying = false; return; }
            if (EditorApplication.isCompiling)
            {
                EditorUtility.DisplayDialog("잠시만요", "스크립트를 컴파일하는 중입니다. 잠시 후 다시 눌러주세요.", "확인");
                return;
            }
            if (!PrepareScene()) return;
            EditorApplication.isPlaying = true;
        }

        [MenuItem("MiniRPG/게임 씬 만들기 · 열기", false, 1)]
        public static void OpenSceneMenu() { PrepareScene(); }

        /// <summary>게임 씬을 만들거나 열고 빌드 목록에 등록</summary>
        static bool PrepareScene()
        {
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
                if (File.Exists(ScenePath))
                {
                    EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                    // 기본 카메라와 조명만 있는 빈 씬. 나머지는 GameManager가 실행 시 자동으로 생성한다.
                    var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                    EditorSceneManager.SaveScene(scene, ScenePath);
                    AssetDatabase.Refresh();
                }
            }
            RegisterBuildScene();
            return true;
        }

        static void RegisterBuildScene()
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            list.RemoveAll(s => s.path == ScenePath);
            list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        [MenuItem("MiniRPG/실행 파일 만들기 (현재 OS용)", false, 20)]
        public static void BuildPlayer()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("플레이 중", "플레이 모드를 끝낸 뒤 다시 시도해주세요.", "확인");
                return;
            }
            if (!PrepareScene()) return;

            BuildTarget target;
            string path;
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds"));
            switch (Application.platform)
            {
                case RuntimePlatform.OSXEditor:
                    target = BuildTarget.StandaloneOSX;
                    path = Path.Combine(root, "Mac/DawnlightLegend.app");
                    break;
                case RuntimePlatform.LinuxEditor:
                    target = BuildTarget.StandaloneLinux64;
                    path = Path.Combine(root, "Linux/DawnlightLegend.x86_64");
                    break;
                default:
                    target = BuildTarget.StandaloneWindows64;
                    path = Path.Combine(root, "Windows/DawnlightLegend.exe");
                    break;
            }

            // 창 모드 1600x900, 크기 조절 가능
            PlayerSettings.productName = "Dawnlight Legend";
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = path,
                target = target,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            var sum = report.summary;
            if (sum.result == BuildResult.Succeeded)
            {
                EditorUtility.DisplayDialog("빌드 완료",
                    "실행 파일을 만들었습니다.\n\n" + path + "\n\n제출할 때는 이 파일이 들어 있는 폴더 전체를 압축해서 제출하세요. (실행 파일만 따로 옮기면 실행되지 않습니다)", "폴더 열기");
                EditorUtility.RevealInFinder(path);
            }
            else
            {
                EditorUtility.DisplayDialog("빌드 실패",
                    "빌드에 실패했습니다 (" + sum.result + ", 오류 " + sum.totalErrors + "개).\n\n" +
                    "· Unity Hub → Installs → 사용 중인 버전 → Add modules에서\n  '" + TargetModuleName(target) + "'이 설치되어 있는지 확인하세요.\n" +
                    "· Console 창(Ctrl+Shift+C)의 빨간 오류 첫 줄을 확인하세요.", "확인");
            }
        }

        static string TargetModuleName(BuildTarget t)
        {
            switch (t)
            {
                case BuildTarget.StandaloneOSX: return "Mac Build Support (Mono)";
                case BuildTarget.StandaloneLinux64: return "Linux Build Support (Mono)";
                default: return "Windows Build Support (Mono)";
            }
        }

        [MenuItem("MiniRPG/저장 데이터 삭제", false, 40)]
        public static void DeleteSave()
        {
            if (!EditorUtility.DisplayDialog("저장 데이터 삭제", "새벽빛 전설의 저장 데이터를 삭제할까요?", "삭제", "취소")) return;
            PlayerPrefs.DeleteKey("MiniRPG_Save");
            PlayerPrefs.Save();
        }

        [MenuItem("MiniRPG/문제 해결 도움말", false, 41)]
        public static void Help()
        {
            EditorUtility.DisplayDialog("문제 해결",
                "1. Console 창(Window > General > Console)에 빨간 오류가 있으면 첫 줄을 확인하세요.\n" +
                "2. 'error CS...' 컴파일 오류가 있으면 Play 버튼이 동작하지 않습니다. 다른 스크립트와 충돌하지 않도록 새 프로젝트에서 실행하세요.\n" +
                "3. 게임 화면 위쪽에 빨간 오류 창이 뜨면, 그 첫 줄을 알려주세요.\n" +
                "4. 글자가 보이지 않으면 Windows/macOS에서 실행해 주세요 (한글 OS 폰트 사용).",
                "확인");
        }
    }
}
