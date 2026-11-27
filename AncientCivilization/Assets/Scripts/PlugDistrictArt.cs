using System;
using UnityEngine;

namespace Plugin.PlayerCore
{
    internal enum PlugDistrictPiece { Backplate, Fixture, Deck, Indicator }

    internal static class PlugDistrictArt
    {
        const int FloorCount = 5;
        const int RowHeight = 64;
        const float PixelsPerUnit = 32f;
        static readonly RectInt[] cells =
        {
            new RectInt(0, 0, 40, 40),
            new RectInt(42, 0, 28, 62),
            new RectInt(72, 0, 96, 16),
            new RectInt(170, 0, 12, 12)
        };

        static readonly Sprite[,] parts = new Sprite[FloorCount, cells.Length];
        static Texture2D sheet;

        static Sprite Frame(int floor, PlugDistrictPiece piece)
        {
            int index = (int)piece;
            if (floor < 0 || floor >= FloorCount) throw new ArgumentOutOfRangeException(nameof(floor));
            if (index < 0 || index >= cells.Length) throw new ArgumentOutOfRangeException(nameof(piece));
            if (parts[floor, index]) return parts[floor, index];

            if (!sheet) sheet = Resources.Load<Texture2D>("Traversal/PlugDistrictMachinery");
            if (!sheet) throw new InvalidOperationException("층별 전력 설비 아틀라스를 읽을 수 없습니다.");
            // 원화는 위에서 아래로 생산·생활·행정·거주·최상층 순서다.
            RectInt cell = cells[index];
            var rect = new Rect(cell.x, sheet.height - floor * RowHeight - cell.height, cell.width, cell.height);
            // 플랫폼 양 끝의 브래킷 폭을 고정해 짧은 디딤판에서도 모양이 무너지지 않도록 한다.
            var border = piece == PlugDistrictPiece.Deck ? new Vector4(8, 0, 8, 0) : Vector4.zero;
            var sprite = Sprite.Create(sheet, rect, Vector2.one * .5f, PixelsPerUnit, 0,
                SpriteMeshType.FullRect, border, false);
            sprite.name = $"전력 설비 {floor + 1} · {piece}";
            parts[floor, index] = sprite;
            return sprite;
        }

        internal static SpriteRenderer Part(Transform parent, string label, int floor, PlugDistrictPiece piece,
            Vector2 localPosition, Vector2 size, int sortingOrder)
        {
            Sprite sprite = Frame(floor, piece);
            var part = new GameObject(label);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            var art = part.AddComponent<SpriteRenderer>();
            art.sprite = sprite;
            art.drawMode = SpriteDrawMode.Sliced;
            art.size = size;
            art.sortingOrder = sortingOrder;
            if (piece == PlugDistrictPiece.Indicator)
            {
                // 신호색·밝기는 실제 기계의 전력 상태에서 전달한다.
                art.sharedMaterial = BluePlugArt.GlowMaterial;
                art.color = new Color32(75, 85, 92, 255);
            }
            return art;
        }
    }
}
