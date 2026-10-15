using UnityEngine;
public static class RiftBootstrap {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Boot(){if(Object.FindFirstObjectByType<RiftGame>()==null)new GameObject("Rift Game").AddComponent<RiftGame>();}
}
