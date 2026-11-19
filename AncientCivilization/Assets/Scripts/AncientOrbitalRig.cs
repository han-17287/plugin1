using UnityEngine;

namespace Plugin.AncientCivilization
{
    // 2차전과 같은 상층 병기 차체에 기계식 조리개와 두 개의 독립된 궤도 작동등을 단다.
    internal sealed class AncientOrbitalRig
    {
        static readonly Vector2[] BracerOffsets = {
            new Vector2(.53f, 1.18f), new Vector2(.82f, 1.5f), new Vector2(1.27f, 1.46f),
            new Vector2(.91f, 1.61f), new Vector2(1.35f, 1.62f), new Vector2(.61f, 1.04f)
        };
        static readonly Sprite[] Parts = new Sprite[9];
        readonly Transform rig;
        readonly SpriteRenderer body, lens, leftCover, rightCover, bracer, primary, secondary;
        readonly SpriteRenderer[] surfaces;
        static readonly Color StrikeFlash = new Color(.65f, .92f, 1f);

        static void Load()
        {
            if (Parts[0]) return;
            var sheet = Resources.Load<Texture2D>("Art/OrbitalIris");
            for (int i = 0; i < Parts.Length; i++)
            {
                Parts[i] = Sprite.Create(sheet, new Rect(i * 32, 0, 32, 32), Vector2.one * .5f,
                    29, 0, SpriteMeshType.FullRect);
            }
        }

        public static void AttachRematchIris(Transform owner)
        {
            Load();
            Vector2 core = Vector2.up * 1.65f;
            AncientArt.Display("재교전 · 코어 장갑 테두리", Parts[0], core, 12, owner);
            AncientArt.Display("재교전 · 노출된 렌즈", Parts[1], core, 13, owner);
            var damagedCover = AncientArt.Display("첫 교전에서 휜 셔터", Parts[3],
                core + new Vector2(-.16f, -.10f), 14, owner);
            damagedCover.transform.localRotation = Quaternion.Euler(0, 0, 14f);
        }

        public AncientOrbitalRig(Transform owner)
        {
            Load();
            rig = new GameObject("궤도 봉쇄기 작동부").transform;
            rig.SetParent(owner, false);
            body = AncientArt.Display("상층 보안 병기 차체", AncientArt.Boss(3,0), Vector2.zero, 10, rig);
            Vector2 core = Vector2.up * 1.65f;
            var rim = AncientArt.Display("코어 장갑 테두리", Parts[0], core, 12, rig);
            lens = AncientArt.Display("렌즈", Parts[1], core, 13, rig);
            leftCover = AncientArt.Display("왼쪽 코어 셔터", Parts[3], core, 14, rig);
            rightCover = AncientArt.Display("오른쪽 코어 셔터", Parts[4], core, 14, rig);
            bracer = AncientArt.Display("궤도 작동 팔", Parts[5], BracerOffsets[0], 15, rig);
            primary = AncientArt.Display("첫째 발사등", Parts[6], new Vector2(-.17f,0), 16, bracer.transform);
            secondary = AncientArt.Display("후속 발사등", Parts[6], new Vector2(.17f,0), 16, bracer.transform);
            surfaces = new[] { body, rim, lens, leftCover, rightCover, bracer, primary, secondary };
        }

        public void Pose(int index, int facing, bool exposed, bool followup, bool flashLens, float hitFlash)
        {
            rig.localScale = new Vector3(facing,1,1);
            body.sprite = AncientArt.Boss(3, index);
            body.color = hitFlash > 0 ? StrikeFlash : Color.white;
            bracer.transform.localPosition = BracerOffsets[index];
            float opening = exposed || flashLens ? .31f : .025f;
            leftCover.transform.localPosition = new Vector2(-opening, 1.65f);
            rightCover.transform.localPosition = new Vector2(opening, 1.65f);
            lens.sprite = Parts[exposed || flashLens ? 2 : 1];
            primary.sprite = Parts[index > 0 && index < 5 ? 7 : 6];
            secondary.sprite = Parts[followup ? 8 : 6];
        }

        public void Opacity(float opacity)
        {
            var tint = new Color(1, 1, 1, opacity);
            foreach (var image in surfaces) image.color = tint;
        }
    }
}
