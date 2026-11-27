using System;
using Plugin.PlayerCore;
using UnityEditor;
using UnityEngine;

namespace Plugin.AncientCivilization.EditorChecks
{
    public static class FilamentPresentationChecks
    {
        [MenuItem("PLUGIN/검증/블레이드 픽셀과 발광")]
        public static void Run()
        {
            const string path = "Assets/Resources/Traversal/FilamentBladeAtlas.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            var blade = FilamentBladeArt.Blade;
            var emission = FilamentBladeArt.Emission;
            Check(importer && !importer.mipmapEnabled && importer.filterMode == FilterMode.Point,
                "블레이드 아틀라스 임포트 설정");
            Check(blade && emission && blade.texture == emission.texture &&
                blade.rect.size == new Vector2(36,96) && emission.rect.xMin == blade.rect.xMax &&
                blade.pivot == emission.pivot && blade.pixelsPerUnit == 40,"원화와 발광 마스크의 정렬");
            var shader = Resources.Load<Shader>("Shaders/Presentation");
            Check(shader && shader.isSupported && !ShaderUtil.ShaderHasError(shader),
                "현재 그래픽 장치의 화면 후처리 셰이더 지원");
            CheckGlow(shader);
            Debug.Log("통과 · 임포트, 원화/마스크 피벗, 실제 GPU의 발광 범위와 색.");
        }

        static void CheckGlow(Shader shader)
        {
            const int size = 48, mid = size/2;
            var sample = new Texture2D(size,size,TextureFormat.RGBA32,false,true);
            var read = new Texture2D(1,1,TextureFormat.RGBA32,false,true);
            var mat = new Material(shader);
            var destination = RenderTexture.GetTemporary(size,size,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
            var previous = RenderTexture.active;
            try
            {
                sample.filterMode=FilterMode.Point;
                var pixels = new Color[size*size];
                for(int y=0;y<size;y++)
                for(int x=0;x<size;x++)
                    pixels[y*size+x]=Mathf.Abs(x-mid)<=1 && Mathf.Abs(y-mid)<=1 ? new Color(1,.72f,.13f) : Color.black;
                sample.SetPixels(pixels);
                sample.Apply();
                mat.SetFloat("_BloomRadius",3);
                Color Sample(float glow, int distance)
                {
                    mat.SetFloat("_BloomStrength",glow);
                    Graphics.Blit(sample,destination,mat);
                    RenderTexture.active=destination;
                    read.ReadPixels(new Rect(mid+distance,mid,1,1),0,0);
                    read.Apply();
                    return read.GetPixel(0,0);
                }
                var before=Sample(0,4);
                var after=Sample(.65f,4);
                Check(after.r>before.r+.015f && after.r>after.b, "전구 가장자리의 따뜻한 발광");
                Check(Sample(.65f,16).r<after.r,"국소광 범위");
            }
            finally
            {
                RenderTexture.active=previous;
                RenderTexture.ReleaseTemporary(destination);
                UnityEngine.Object.DestroyImmediate(sample);
                UnityEngine.Object.DestroyImmediate(read);
                UnityEngine.Object.DestroyImmediate(mat);
            }
        }

        static void Check(bool ok,string target)
        {
            if (!ok) throw new InvalidOperationException("검증 실패 · "+target);
        }
    }
}
