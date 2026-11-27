using System;
using Plugin.AncientCivilization;
using UnityEngine;
using UnityEngine.Rendering;

namespace Plugin.PlayerCore
{
    // URP가 있는 프로젝트에서는 2D 광원을 쓰고, 기본 렌더러에서는 스프라이트 후광을 사용한다.
    internal sealed class BluePlugPointLight
    {
        readonly Transform parent;
        Behaviour light;
        Action<float> setIntensity, setRadius;
        Action<Color> setColor;
        bool initialized;

        internal BluePlugPointLight(Transform parent) { this.parent = parent; }

        internal void Set(Vector2 point, Color color, float intensity, float radius)
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            if (!pipeline || !AncientVisuals.HighQuality || intensity <= 0)
            {
                Clear();
                return;
            }
            if (!initialized)
            {
                initialized = true;
                if (pipeline.GetType().FullName == "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset")
                    Create();
            }
            if (!light) return;
            light.transform.position = point;
            setColor(color);
            setIntensity(intensity);
            setRadius(radius);
            light.enabled = true;
        }

        void Create()
        {
            var type = Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime", false);
            var kind = type?.GetProperty("lightType");
            var intensity = type?.GetProperty("intensity")?.GetSetMethod();
            var radius = type?.GetProperty("pointLightOuterRadius")?.GetSetMethod();
            var color = type?.GetProperty("color")?.GetSetMethod();
            if (kind == null || !kind.CanWrite || intensity == null || radius == null || color == null) return;
            var name = Enum.IsDefined(kind.PropertyType, "Point") ? "Point" : "Spot";
            if (!Enum.IsDefined(kind.PropertyType, name)) return;

            var holder = new GameObject("플러그 국소광");
            holder.transform.SetParent(parent, false);
            light = (Behaviour)holder.AddComponent(type);
            kind.SetValue(light, Enum.Parse(kind.PropertyType, name));
            setIntensity = (Action<float>)intensity.CreateDelegate(typeof(Action<float>), light);
            setRadius = (Action<float>)radius.CreateDelegate(typeof(Action<float>), light);
            setColor = (Action<Color>)color.CreateDelegate(typeof(Action<Color>), light);
        }

        internal void Clear()
        {
            if (light) light.enabled = false;
        }
    }
}
