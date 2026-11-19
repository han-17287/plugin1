using UnityEngine;

namespace Plugin.AncientCivilization
{
    [CreateAssetMenu(menuName = "PLUGIN/무전 대사")]
    public sealed class AncientRadioCue : ScriptableObject
    {
        [SerializeField] string speaker = "사령관";
        [SerializeField, TextArea(1, 4)] string words = "";

        public void Play(AncientStory story)
        {
            if (story && !string.IsNullOrWhiteSpace(words)) story.RadioBark(speaker, words);
        }
    }
}
