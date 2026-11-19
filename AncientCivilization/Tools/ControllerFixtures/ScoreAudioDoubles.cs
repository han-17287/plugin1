// 수동 로딩과 샘플 시계로 장면 전환을 재현한다. Unity DSP와 임포터는 이중 객체의 범위 밖이다.
using System;
using System.Collections.Generic;

namespace UnityEngine.Audio
{
    public class AudioMixerGroup : UnityEngine.Object { }
}

namespace UnityEngine
{
    public class Object
    {
        public string name;
        public static implicit operator bool(Object value) => value != null;
        public static void Destroy(Object value) { }
    }

    public class Transform : Object { public void SetParent(Transform parent, bool worldPositionStays) { } }

    public class GameObject : Object
    {
        public Transform transform = new Transform();
        public GameObject(string name) { this.name = name; }

        public T AddComponent<T>() where T : Object, new()
        {
            var component = new T();
            if (component is AudioSource voice) AudioSource.All.Add(voice);
            return component;
        }
    }

    public enum AudioDataLoadState { Unloaded, Loading, Loaded, Failed }

    public class AudioClip : Object
    {
        public AudioDataLoadState loadState = AudioDataLoadState.Loaded;
        public int samples = 4000, frequency = 1000;
        public bool LoadAudioData() { loadState = AudioDataLoadState.Loading; return true; }
    }

    public class ResourceRequest
    {
        public bool isDone;
        public Object asset;
    }

    public static class Resources
    {
        public const string Folder = "Music/CityScore/";
        public static readonly Dictionary<string, ResourceRequest> Pending = new();
        public static readonly List<string> Discarded = new();

        public static ResourceRequest LoadAsync<T>(string key)
        {
            var request = new ResourceRequest();
            Pending.Add(key, request);
            return request;
        }

        public static AudioClip Finish(string slug)
        {
            var request = Pending[Folder + slug];
            var clip = new AudioClip { name = slug };
            request.asset = clip;
            request.isDone = true;
            return clip;
        }

        public static void UnloadAsset(Object clip)
        {
            Discarded.Add(clip.name);
            Pending.Remove(Folder + clip.name);
        }
    }

    public class AudioSource : Object
    {
        public static readonly List<AudioSource> All = new();
        public readonly List<string> Attacks = new();
        public Audio.AudioMixerGroup outputAudioMixerGroup;
        public AudioClip clip;
        public bool playOnAwake, loop, isPlaying, mute;
        public int timeSamples, priority;
        public float spatialBlend, dopplerLevel, volume;
        bool paused;

        public void Play() { Attacks.Add(clip.name); paused = false; isPlaying = true; }
        public void Stop() { isPlaying = paused = false; timeSamples = 0; }
        public void Pause() { if (isPlaying) { isPlaying = false; paused = true; } }
        public void UnPause() { if (paused) { paused = false; isPlaying = true; } }
        public void Advance(float seconds)
        {
            if (!isPlaying) return;
            timeSamples += (int)Math.Round(seconds * clip.frequency);
            if (timeSamples < clip.samples) return;
            if (loop) timeSamples %= clip.samples;
            else Stop();
        }
    }

    public class AudioLowPassFilter : Object { public float cutoffFrequency; public bool enabled; }
    public static class Debug { public static void LogWarning(string details) => Console.Error.WriteLine(details); }
    public static class Time { public static float unscaledDeltaTime; }
    public static class Mathf
    {
        public static float Min(float a, float b) => Math.Min(a, b);
        public static float Abs(float a) => Math.Abs(a);
        public static float Exp(float a) => MathF.Exp(a);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float MoveTowards(float a,float b,float delta) => a + Math.Clamp(b-a,-delta,delta);
        public static float Clamp01(float t) => Math.Clamp(t, 0, 1);
        public static int RoundToInt(float f) => (int)MathF.Round(f);
        public static int Clamp(int v, int min, int max) => Math.Clamp(v, min, max);
    }
}
