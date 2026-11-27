using System.Collections.Generic;
using UnityEngine;

namespace Plugin.AncientCivilization
{
    // 적의 실제 스케줄러가 보낸 신호를 기록한다. 렌더러와 피해 판정은 이 테스트 더블에 없다.
    public static class AncientBossCue
    {
        public enum Style { Captain, Mech, Enforcer, Orbital, Joker, Clock, Convoy, Guardian }
        public enum Beat { Tell, Strike, Contact }

        public readonly record struct Sample(AncientGame Game, Style Style, Beat Beat,
            Vector2 Point, Vector2 Direction, float Scale, Transform Support);

        public static readonly List<Sample> Events = new();
        public static readonly List<(AncientGame Game, Style Style)> Clears = new();

        public static void Play(AncientGame game, Style style, Beat beat, Vector2 point, Vector2 direction,
            float worldScale = 1f, Transform support = null)
        {
            Events.Add(new Sample(game, style, beat, point, direction, worldScale, support));
        }

        public static void Clear(AncientGame game, Style style) => Clears.Add((game, style));
        public static void ClearAll(AncientGame game)
        {
            foreach (Style style in System.Enum.GetValues<Style>())
                Clear(game, style);
        }

        public static void Reset()
        {
            Events.Clear();
            Clears.Clear();
        }
    }
}
