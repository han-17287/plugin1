using Plugin.AncientCivilization;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Plugin.PlayerCore
{
    // 연습 씬은 순찰 게임을 생성하지 않고도 본편의 믹서와 음소거 설정을 공유한다.
    internal sealed class PracticeMusic
    {
        static readonly string[] Flow =
            { "shift-current", "alley-trail", "numbered-facades", "glass-terrace", "summit-trespass" };
        static readonly string[] Challenge =
            { "filament", "spotlight-grid", "shutter-index", "greenhouse-fault", "prism-circuit" };

        readonly AncientScorePlayer player;
        readonly AncientFeedbackProfile profile;
        bool muted;
        bool changedRoom;

        public PracticeMusic(Transform owner, Camera view)
        {
            if (!Object.FindAnyObjectByType<AudioListener>())
                view.gameObject.AddComponent<AudioListener>();
            profile = Resources.Load<AncientFeedbackProfile>("Audio/FieldFeedback");
            player = new AncientScorePlayer(owner, profile ? profile.music : null);
            muted = AncientGame.Saved("muted") != 0;
            player.SetMuted(muted);
        }

        public void Tick(int floor, bool challenge, bool paused)
        {
            if (Application.isFocused && MutePressed())
            {
                muted = !muted;
                player.SetMuted(muted);
                AncientGame.SetSaved("muted", muted ? 1 : 0);
                PlayerPrefs.Save();
            }
            player.Tick(challenge ? Challenge[floor] : Flow[floor], paused || !Application.isFocused,
                changedRoom, profile ? profile.scoreVolume : .42f, false);
            changedRoom = false;
        }

        static bool MutePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.f7Key.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.F7);
#else
            return false;
#endif
        }

        public void EnterRoom() { changedRoom = true; }
        public void Checkpoint() { player.PlayEvent("checkpoint"); }
        public void Retry() { player.PlayEvent("death"); }
        public void Suspend() { player.Suspend(); }
        public void Dispose() { player.Dispose(); }
    }
}
