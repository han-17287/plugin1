using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Plugin.OptionalURP.Editor
{
    public static class BluePlugGraphChecks
    {
        [MenuItem("Tools/Blue Plug/Validate URP Graphs")]
        public static void Run()
        {
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
                throw new InvalidOperationException("활성 URP 에셋에서 검사를 실행하세요.");

            Check("SignalCord", "_Signal", "_Energy", "_Flow", "_CableLength");
            Check("PlugFilament", "_MainTex", "_Intensity");
            Debug.Log("플러그의 두 URP 그래프와 런타임 프로퍼티를 확인했습니다.");
        }

        static void Check(string asset, params string[] inputs)
        {
            string resourcePath = "BluePlugURP/" + asset;
            Shader shader = Resources.Load<Shader>(resourcePath);
            if (!shader) throw new InvalidOperationException(resourcePath + " 그래프를 읽지 못했습니다.");
            string path = AssetDatabase.GetAssetPath(shader);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            shader = Resources.Load<Shader>(resourcePath);
            if (!shader || !shader.isSupported || ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException("Shader Graph 임포트 오류: " + path);

            var material = new Material(shader);
            try
            {
                foreach (string input in inputs)
                    if (!material.HasProperty(input))
                        throw new InvalidOperationException(asset + "에 " + input + " 프로퍼티가 없습니다.");
            }
            finally { UnityEngine.Object.DestroyImmediate(material); }
        }
    }
}
