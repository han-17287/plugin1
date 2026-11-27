using System;
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
        // 탐험 구간을 잠시 벗어나도 같은 도입부를 되풀이하지 않도록 다음 마디를 보관한다.
        readonly Dictionary<string, int> regionReturns = new Dictionary<string, int>();
        readonly GameObject host;
        readonly AudioSource music, stinger;
        readonly AudioLowPassFilter filter;
        string candidate, selected, playing, queuedEvent, playingEvent;
        string boundaryRoute;
        int previousSample, samplesToBoundary, returnSample;
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

        // 수록된 4/4 탐험 믹스의 악보 템포. Unity 임포트 후 샘플레이트로 경계를 계산한다.
        static int RegionBpm(string slug)
        {
            switch (slug)
            {
                case "morning-inventory": return 126;
                case "main-theme": return 204;
                case "shift-current": return 220;
                case "filament": return 158;
                case "prism-circuit": return 132;
                case "heat-riser": return 216;
                case "alley-trail": return 136;
                case "salvage-orbit": return 138;
                case "spotlight-grid": return 154;
                case "warehouse-crew": return 208;
                case "service-lantern": return 78;
                case "sealed-gate": return 85;
                case "numbered-facades": return 140;
                case "shutter-index": return 162;
                case "ledger-imprint": return 87;
                case "witness-crossing": return 152;
                case "greenhouse-fault": return 172;
                case "glass-terrace": return 110;
                case "summit-trespass": return 184;
                case "clock-ante": return 92;
                case "vacant-office": return 83;
                default: return 0;
            }
        }

        enum BossSuite { None, IronSuit, Enforcer, Warden, Joker }

        // 같은 보스의 편곡 전환에만 마디 경계를 쓴다. 실제 음원 길이에서 한 마디를 구해 임포트 주파수에 대응한다.
        static BossSuite PhaseSuite(string slug, out int bars)
        {
            bars = 32;
            switch (slug)
            {
                case "iron-suit-1": case "iron-suit-2": case "iron-suit-3": return BossSuite.IronSuit;
                case "enforcer-1": case "enforcer-2": case "enforcer-3": return BossSuite.Enforcer;
                case "residence-warden-1": bars = 24; return BossSuite.Warden;
                case "residence-warden-2": case "residence-warden-3": return BossSuite.Warden;
                case "joker-sightline": bars = 28; return BossSuite.Joker;
                case "joker-double": case "joker-turrets": return BossSuite.Joker;
                // 마지막 레이저 충돌과 승패 신호는 마디를 기다리지 않는다.
                default: return BossSuite.None;
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
            if (selected != null && requested != playing && !menuOrBoot && RegionBpm(requested) != 0 && candidateAge < RegionHoldSeconds)
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

        bool FinishBoundary(string nextRoute, double interval, bool rememberExplorationBar, float volume, float dt)
        {
            var clip = music.clip;
            int cursor = music.timeSamples;
            if (boundaryRoute != nextRoute)
            {
                double lead = rememberExplorationBar ? ReleaseSeconds * clip.frequency : 0;
                int index = (int)Math.Ceiling((cursor + lead) / interval);
                samplesToBoundary = (int)Math.Round(index * interval) - cursor;
                if (rememberExplorationBar)
                {
                    // 첫 Update의 프레임 지터가 복귀 마디를 한 마디 더 밀지 않도록 실제로 겨눈 박을 쓴다.
                    int nextBar = (index + 3) / 4;
                    returnSample = (int)Math.Round(nextBar * 4 * interval) % clip.samples;
                }
                previousSample = cursor;
                boundaryRoute = nextRoute;
            }
            int advanced = cursor - previousSample;
            if (advanced < 0) advanced += clip.samples;
            previousSample = cursor;
            samplesToBoundary -= advanced;
            if (samplesToBoundary <= 0)
            {
                if (rememberExplorationBar) regionReturns[playing] = returnSample;
                return true;
            }
            float envelope = Mathf.Clamp01(samplesToBoundary / (ReleaseSeconds * clip.frequency));
            musicGain = Mathf.Min(envelope, Mathf.MoveTowards(musicGain, 1, dt / ReleaseSeconds));
            music.volume = volume * musicGain;
            return false;
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
            if (regionReturns.Count > 0 && (cancelEvents || RegionBpm(route) == 0)) regionReturns.Clear();
            if (selected != route)
            {
                boundaryRoute = null;
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
            if (suspended || roomMusic || playingEvent != null || accent || cancelEvents)
                boundaryRoute = null;
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
                int tempo = RegionBpm(playing);
                var suite = PhaseSuite(playing, out int bars);
                bool sameBoss = suite != BossSuite.None && suite == PhaseSuite(route, out _);
                bool fieldChange = tempo > 0 && RegionBpm(route) > 0;
                // 포커스나 사건 음악으로 멈춰 있던 트랙도 보관한 위치에서 남은 박을 마친다.
                if (heldMusic && !cancelEvents && (sameBoss || fieldChange)) ResumeHeldMusic(0);
                bool clockRunning = !cancelEvents && music.isPlaying && music.clip &&
                    music.clip.samples > 0 && music.clip.frequency > 0;
                if (clockRunning && sameBoss)
                {
                    if (!FinishBoundary(route, music.clip.samples / (double)bars, false, volume, dt)) return;
                }
                else if (clockRunning && fieldChange)
                {
                    if (!FinishBoundary(route, music.clip.frequency * 60d / tempo, true, volume, dt)) return;
                }
                else if (!Released(music, ref musicGain, volume, dt)) return;
                StopMusic();
                boundaryRoute = null;
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
            else if (!cancelEvents && regionReturns.TryGetValue(route, out int start))
            {
                music.timeSamples = Mathf.Clamp(start, 0, next.samples - 1);
                // 보관한 마디의 첫 음도 들리도록 복귀 프레임부터 페이드인을 시작한다.
                musicGain = Mathf.Min(.5f, dt / ReleaseSeconds);
            }
            music.volume = musicGain * volume;
            playing = route;
            music.Play();
        }

        void AlignClock(float elapsed)
        {
            AudioClip clip = music.clip;
            if (clip && clip.samples > 0)
                music.timeSamples = Mathf.Clamp(Mathf.RoundToInt(elapsed * clip.frequency), 0, clip.samples - 1);
        }

        void ResumeHeldMusic(float elapsed)
        {
            if (elapsed > 0) AlignClock(elapsed);
            musicGain = 0;
            music.volume = 0;
            music.UnPause();
            heldMusic = false;
        }

        void ContinueMusic(float volume, float dt, float elapsed)
        {
            if (heldMusic) ResumeHeldMusic(elapsed);
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
            UnityEngine.Object.Destroy(host);
        }
    }
}
