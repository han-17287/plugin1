using Plugin.AncientCivilization;
using UnityEngine;

namespace Plugin.PlayerCore
{
    // 원화의 칼끝과 손잡이 사이에만 면을 펼친다. 접점은 월드 좌표에 남긴다.
    internal sealed class FilamentCutInk
    {
        internal const int Wall = 0, Hit = 1, Finisher = 2;
        const int Frames = 8, CellWidth = 144, CellHeight = 112;
        static readonly Sprite[] Cells = new Sprite[Frames * 3];
        readonly Transform owner;
        readonly Mesh ribbon;
        readonly Vector3[] vertices;
        readonly MeshRenderer sweep;
        readonly SpriteRenderer mark;
        Vector2 impact;
        float start, life, angle;
        int strip;

        internal FilamentCutInk(Transform root, Material light, int count)
        {
            owner = root;
            var cut = new GameObject("칼날의 유리 잔광");
            cut.transform.SetParent(root, false);
            sweep = cut.AddComponent<MeshRenderer>();
            sweep.sharedMaterial = light;
            sweep.sortingOrder = 19;
            sweep.enabled = false;
            ribbon = new Mesh { name = "필라멘트 칼끝 단면" };
            ribbon.MarkDynamic();
            vertices = new Vector3[count * 3];
            var colors = new Color32[vertices.Length];
            var triangles = new int[(count - 1) * 12];
            for (int i = 0; i < count; i++)
            {
                int v = i * 3;
                float t = i / (float)(count - 1);
                colors[v] = new Color32(255, 234, 156, (byte)(164 * t));
                colors[v + 1] = new Color32(255, 152, 52, (byte)(123 * t));
                colors[v + 2] = new Color32(238, 88, 31, 0);
                if (i == count - 1) break;
                for (int band = 0; band < 2; band++)
                {
                    int a = v + band, k = i * 12 + band * 6;
                    triangles[k] = a; triangles[k + 1] = a + 1; triangles[k + 2] = a + 3;
                    triangles[k + 3] = a + 1; triangles[k + 4] = a + 4; triangles[k + 5] = a + 3;
                }
            }
            ribbon.vertices = vertices;
            ribbon.colors32 = colors;
            ribbon.triangles = triangles;
            cut.AddComponent<MeshFilter>().sharedMesh = ribbon;

            var contact = new GameObject("필라멘트 절단면");
            contact.transform.SetParent(root, false);
            mark = contact.AddComponent<SpriteRenderer>();
            mark.sortingOrder = 24;
            mark.enabled = false;
            if (!Cells[0])
            {
                var atlas = Resources.Load<Texture2D>("Art/FilamentContacts");
                if (!atlas) return;
                var pivot = new Vector2(43f / CellWidth, 1f - 57f / CellHeight);
                for (int i = 0; i < Cells.Length; i++)
                {
                    var rect = new Rect(i % Frames * CellWidth, (2 - i / Frames) * CellHeight, CellWidth, CellHeight);
                    Cells[i] = Sprite.Create(atlas, rect, pivot, 73f, 0, SpriteMeshType.FullRect);
                }
            }
        }

        internal void Sweep(Vector3[] tips, Vector3[] grips, int step, bool detailed)
        {
            sweep.enabled = detailed;
            if (!detailed) return;
            for (int i = 0; i < tips.Length; i++)
            {
                int v = i * 3;
                float t = i / (float)(tips.Length - 1);
                float width = Mathf.Sin(t * Mathf.PI) * (step == 3 ? .32f : .23f);
                Vector3 tip = owner.InverseTransformPoint(tips[i]);
                Vector3 span = owner.InverseTransformVector(grips[i] - tips[i]) * width;
                vertices[v] = tip;
                vertices[v + 1] = tip + span * .32f;
                vertices[v + 2] = tip + span;
            }
            ribbon.vertices = vertices;
            ribbon.RecalculateBounds();
        }

        internal void Contact(Vector2 point, Vector2 outward, int kind)
        {
            strip = kind;
            life = kind == Wall ? .13f : kind == Finisher ? .21f : .17f;
            start = Time.time;
            impact = point;
            angle = Mathf.Atan2(outward.y, outward.x) * Mathf.Rad2Deg;
            var game = AncientGame.Instance;
            bool subdued = game && game.ReducedFeedback;
            mark.color = subdued ? new Color(1f, .91f, .78f, .68f) : Color.white;
            mark.transform.localScale = Vector3.one * (AncientVisuals.HighQuality ? 1f : .85f);
            mark.enabled = Cells[0] != null;
            Tick(start);
        }

        internal void Tick(float now)
        {
            if (!mark.enabled) return;
            int frame = Mathf.FloorToInt((now - start) / life * Frames);
            if (frame >= Frames) { HideContact(); return; }
            mark.transform.SetPositionAndRotation(impact, Quaternion.Euler(0, 0, angle));
            mark.sprite = Cells[strip * Frames + Mathf.Max(0, frame)];
        }

        internal void HideContact() { mark.enabled = false; }
        internal void HideRibbon() { sweep.enabled = false; }
        internal void Dispose() { Object.Destroy(ribbon); }
    }
}
