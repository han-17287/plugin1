using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class ProjectSetup {
 [MenuItem("PLUGIN/Create Starter Scene")]
 public static void CreateScene() {
  if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
  new GameObject("Rift Game").AddComponent<RiftGame>();
  System.IO.Directory.CreateDirectory("Assets/Scenes");
  EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), "Assets/Scenes/Main.unity");
  EditorBuildSettings.scenes = new[]{new EditorBuildSettingsScene("Assets/Scenes/Main.unity",true)};
 }
}
