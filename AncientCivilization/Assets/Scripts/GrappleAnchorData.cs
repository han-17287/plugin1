using UnityEngine;
namespace Plugin.PlayerCore
{
    [CreateAssetMenu(menuName="PLUGIN/Player/Anchor Data")]
    public sealed class GrappleAnchorData : ScriptableObject
    {
        [Min(.05f)] public float radius=.38f;
        [Min(0)] public float movingAmplitude=1,movingPeriod=3.5f;
        public Vector2 movingAxis=Vector2.right;
        public bool orbit;
        public Color color=new Color(.25f,.85f,1);
    }
}
