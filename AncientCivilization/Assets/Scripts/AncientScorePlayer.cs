using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Plugin.AncientCivilization
{
    // 사건 음악이 연주되는 동안은 배경곡의 재생 헤드를 보존한다.
    public sealed class AncientScorePlayer
    {
        const string ResourceFolder = "Music/CityScore/";
        const int CacheLimit = 6;
        const float ReleaseSeconds = .075f;
        const float RegionHoldSeconds = .28f;

        sealed class ClipEntry
        {
            public ResourceRequest request;
            public AudioClip clip;
            public long lastUse;
            public bool preparing, failed;
        }

        readonly Dictionary<string, ClipEntry> cache = new Dictionary<string, ClipEntry>();
        readonly GameObject host;
        readonly AudioSource music, stinger;
        readonly AudioLowPassFilter filter;
        string candidate, selected, playing, queuedEvent, playingEvent;
        float candidateAge;
        bool heldMusic, heldEvent;
        float musicGain = 1, eventGain = 1;
        long use;

        public string CurrentScore => playingEvent ?? playing;
        public bool AllowsRoomMusic => !music.isPlaying && playingEvent == null && queuedEvent == null;

        public AncientScorePlayer(Transform parent, AudioMixerGroup musicGroup)
        {
            host = new GameObject("도시의 악보");
            host.transform.SetParent(parent, false);
            music = AddVoice(host, musicGroup);
            filter = host.AddComponent<AudioLowPassFilter>();
            filter.cutoffFrequency = 22000;
            filter.enabled = false;
            var events = new GameObject("사건 음악");
            events.transform.SetParent(host.transform, false);
            stinger = AddVoice(events, musicGroup);
        }

        static AudioSource AddVoice(GameObject owner, AudioMixerGroup group)
        {
            var voice = owner.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 0;
            voice.dopplerLevel = 0;
            voice.priority = 120;
            voice.outputAudioMixerGroup = group;
            return voice;
        }

        public static bool Loops(string slug)
        {
            switch (slug)
            {
                case "death": case "checkpoint": case "retreat": case "rank-access":
                case "breach-alarm": case "cargo-verdict": case "sixty-to-recovery":
                case "core-ignition": case "first-ending":
                    return false;
                default:
                    return true;
            }
        }

        static bool IsRegion(string slug)
        {
            switch (slug)
            {
                case "morning-inventory": case "main-theme": case "shift-current": case "filament":
                case "prism-circuit": case "heat-riser": case "alley-trail": case "salvage-orbit":
                case "spotlight-grid": case "warehouse-crew": case "service-lantern": case "sealed-gate":
                case "numbered-facades": case "shutter-index": case "ledger-imprint": case "witness-crossing":
                case "greenhouse-fault": case "glass-terrace": case "summit-trespass": case "clock-ante":
                case "vacant-office": return true;
                default: return false;
            }
        }

        string StableRoute(string requested, bool menuOrBoot, float dt)
        {
            if (requested != candidate)
            {
                candidate = requested;
                candidateAge = 0;
                Ready(requested);
            }
            else candidateAge += dt;
            // 발판 경계를 오가도 선율이 다시 시작되지 않게 하고 전투와 컷신에는 즉시 응답한다.
            if (selected != null && !menuOrBoot && IsRegion(requested) && candidateAge < RegionHoldSeconds)
                return selected;
            return requested;
        }

        static int EventPriority(string slug)
        {
            switch (slug)
            {
                case "death": return 4;
                case "rank-access": return 3;
                case "retreat": return 2;
                case "checkpoint": return 1;
                default: return 0;
            }
        }

        public void PlayEvent(string slug)
        {
            int priority = EventPriority(slug);
            if (priority == 0 || slug == playingEvent || slug == queuedEvent) return;
            if (priority >= EventPriority(playingEvent) && priority >= EventPriority(queuedEvent))
                queuedEvent = slug;
        }

        public void SetMuted(bool value)
        {
            music.mute = value;
            stinger.mute = value;
        }

        public void Suspend()
        {
            if (music && music.isPlaying) { music.Pause(); heldMusic = true; }
            if (stinger && stinger.isPlaying) { stinger.Pause(); heldEvent = true; }
        }

        AudioClip Ready(string slug)
        {
            if (slug == null) return null;
            if (!cache.TryGetValue(slug, out var entry))
            {
                if (cache.Count >= CacheLimit && !EvictIdleClip()) return null;
                entry = new ClipEntry { request = Resources.LoadAsync<AudioClip>(ResourceFolder + slug) };
                cache.Add(slug, entry);
            }
            entry.lastUse = ++use;
            if (entry.failed) return null;
            if (entry.request != null)
            {
                if (!entry.request.isDone) return null;
                entry.clip = entry.request.asset as AudioClip;
                entry.request = null;
            }
            if (entry.clip && entry.clip.loadState == AudioDataLoadState.Loaded) return entry.clip;
            if (entry.clip && entry.clip.loadState == AudioDataLoadState.Unloaded)
            {
                if (!entry.preparing) entry.preparing = entry.clip.LoadAudioData();
                if (entry.preparing) return null;
            }
            if (entry.clip && entry.clip.loadState == AudioDataLoadState.Loading) return null;
            entry.failed = true;
            Debug.LogWarning("게임 음악을 불러올 수 없습니다: " + ResourceFolder + slug);
            return null;
        }

        bool EvictIdleClip()
        {
            string oldest = null;
            long stamp = long.MaxValue;
            foreach (var pair in cache)
            {
                if (pair.Key == selected || pair.Key == playing || pair.Key == playingEvent || pair.Key == queuedEvent) continue;
                if (pair.Value.request != null && !pair.Value.request.isDone) continue;
                if (pair.Value.lastUse < stamp) { oldest = pair.Key; stamp = pair.Value.lastUse; }
            }
            if (oldest == null) return false;
            var entry = cache[oldest];
            var clip = entry.request != null ? entry.request.asset as AudioClip : entry.clip;
            cache.Remove(oldest);
            if (clip) Resources.UnloadAsset(clip);
            return true;
        }

        static bool Released(AudioSource voice, ref float gain, float volume, float dt)
        {
            if (!voice.isPlaying) return true;
            gain = Mathf.MoveTowards(gain, 0, dt / ReleaseSeconds);
            voice.volume = gain * volume;
            return gain <= 0;
        }

        void StopEvent()
        {
            stinger.Stop();
            stinger.clip = null;
            playingEvent = null;
            heldEvent = false;
            eventGain = 1;
        }

        void StopMusic()
        {
            music.Stop();
            music.clip = null;
            playing = null;
            heldMusic = false;
            musicGain = 1;
        }

        public void Tick(string route, bool suspended, bool cancelEvents, float volume, bool lowHealth, float elapsedOnEntry = 0, bool roomMusic = false)
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, .1f);
            volume = Mathf.Clamp01(volume);
            route = StableRoute(route, cancelEvents, dt);
            if (selected != route)
            {
                // 재입장한 단발 곡은 다시 시작하고, 한 장소에 계속 있으면 한 번만 재생한다.
                if (playing != null && !music.isPlaying && !heldMusic) StopMusic();
                selected = route;
            }
            AudioClip next = Ready(route);
            if (cancelEvents)
            {
                queuedEvent = null;
                if (playingEvent != null) StopEvent();
            }
            AudioClip accent = Ready(queuedEvent);
            if (queuedEvent != null && cache.TryGetValue(queuedEvent, out var queued) && queued.failed)
                queuedEvent = null;
            if (suspended) { Suspend(); return; }

            float targetCutoff = lowHealth ? 1800 : 22000;
            float cutoff = Mathf.Lerp(filter.cutoffFrequency, targetCutoff, 1 - Mathf.Exp(-6 * dt));
            if (Mathf.Abs(cutoff - targetCutoff) < 12) cutoff = targetCutoff;
            filter.cutoffFrequency = cutoff;
            filter.enabled = cutoff < 22000;

            if (playingEvent != null)
            {
                if (heldEvent) { stinger.UnPause(); heldEvent = false; }
                stinger.volume = eventGain * volume;
                if (stinger.isPlaying && (!accent || !Released(stinger, ref eventGain, volume, dt))) return;
                StopEvent();
            }
            if (accent)
            {
                if (!Released(music, ref musicGain, volume, dt)) return;
                if (music.isPlaying) { music.Pause(); heldMusic = true; }
                stinger.clip = accent;
                stinger.volume = volume;
                stinger.loop = false;
                playingEvent = queuedEvent;
                queuedEvent = null;
                stinger.Play();
                return;
            }

            // 방 안에서 실제로 들려오는 선율에 자리를 내주고, 사건 음악에는 우선권을 준다.
            if (roomMusic)
            {
                if (Released(music, ref musicGain, volume, dt) && music.isPlaying)
                {
                    music.Pause();
                    heldMusic = true;
                }
                return;
            }
            if (playing != route && playing != null)
            {
                // 다음 파일의 첫 버퍼가 준비될 때까지 현재 연주는 계속한다.
                if (route != null && !next)
                {
                    ContinueMusic(volume, dt, 0);
                    return;
                }
                if (!Released(music, ref musicGain, volume, dt)) return;
                StopMusic();
            }
            if (route == null) return;
            if (playing == route)
            {
                ContinueMusic(volume, dt, elapsedOnEntry);
                return;
            }
            if (!next) return;
            music.clip = next;
            music.loop = Loops(route);
            if (elapsedOnEntry > 0) AlignClock(elapsedOnEntry);
            music.volume = volume;
            playing = route;
            music.Play();
        }

        void AlignClock(float elapsed)
        {
            AudioClip clip = music.clip;
            if (clip && clip.samples > 0)
                music.timeSamples = Mathf.Clamp(Mathf.RoundToInt(elapsed * clip.frequency), 0, clip.samples - 1);
        }

        void ContinueMusic(float volume, float dt, float elapsed)
        {
            if (heldMusic)
            {
                if (elapsed > 0) AlignClock(elapsed);
                musicGain = 0;
                music.volume = 0;
                music.UnPause();
                heldMusic = false;
            }
            // 오브젝트 전체가 비활성화되면 Unity가 보관하던 루프 재생 헤드를 해제할 수 있다.
            if (music.loop && !music.isPlaying) music.Play();
            musicGain = Mathf.MoveTowards(musicGain, 1, dt / ReleaseSeconds);
            music.volume = volume * musicGain;
        }

        public void Dispose()
        {
            if (music) StopMusic();
            if (stinger) StopEvent();
            cache.Clear();
            // 장면을 바꾸면 Unity가 참조가 끝난 리소스를 해제한다.
            Object.Destroy(host);
        }
    }
}
