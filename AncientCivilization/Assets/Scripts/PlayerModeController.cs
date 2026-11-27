using System;
using UnityEngine;
namespace Plugin.PlayerCore
{
    public enum PlayerMode { Hook,Attack }
    public sealed class PlayerModeController : MonoBehaviour
    {
        public PlayerMode Mode {get;private set;}
        public event Action<PlayerMode> Changed;
        public void Set(PlayerMode mode){if(Mode==mode)return;Mode=mode;Changed?.Invoke(mode);}
        public Color ModeColor=>Mode==PlayerMode.Hook?new Color(.2f,.8f,1):new Color(1,.82f,.18f);
    }
}
