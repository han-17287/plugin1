using System;
using UnityEngine;

namespace Plugin.PlayerCore
{
    // 판정과 독립인 허리 전지 표시. 불이 켜진 창의 개수로 충전 단계를 읽을 수 있다.
    public sealed class FilamentBatteryLamp : MonoBehaviour
    {
        static readonly Sprite[] faces = new Sprite[4];
        SpriteRenderer display;
        Func<int> charge;
        Func<float> facing;
        Func<bool> visible;
        int lastTier = -1;

        public static void Attach(Transform actor, Func<int> readCharge, Func<float> readFacing, Func<bool> isVisible)
        {
            var holder = new GameObject("필라멘트 전지 표시");
            holder.transform.SetParent(actor, false);
            var lamp = holder.AddComponent<FilamentBatteryLamp>();
            lamp.charge = readCharge; lamp.facing = readFacing; lamp.visible = isVisible;
            lamp.display = holder.AddComponent<SpriteRenderer>();
            lamp.display.sortingOrder = 17;
            if (!faces[0]) BuildFaces();
            lamp.Refresh();
        }

        static void BuildFaces()
        {
            const int w = 9, h = 7;
            var ink = new Color32(32,37,48,255);
            var bezel = new Color32(105,106,106,255);
            var dark = new Color32(61,53,49,255);
            var on = new Color32(255,197,64,255);
            var hot = new Color32(255,245,186,255);
            var tex = new Texture2D(w * 4, h, TextureFormat.RGBA32, false)
            { name="경위 전지 상태 네 장", filterMode=FilterMode.Point, wrapMode=TextureWrapMode.Clamp };
            var pixels = new Color32[w * 4 * h];
            for (int phase=0; phase<4; phase++)
            for (int y=0; y<h; y++)
            for (int x=0; x<w; x++)
            {
                bool edge = y==0 || y==h-1 || x==0 || x==w-1;
                bool corner = (y==0 || y==h-1) && (x==0 || x==w-1);
                Color32 pixel = corner ? default : edge ? ink : y==h-2 ? bezel : dark;
                if (x>=2 && x<=6 && (x&1)==0 && y>=2 && y<=4 && x/2<=phase)
                    pixel = y==4 ? hot : on;
                pixels[y*w*4+phase*w+x] = pixel;
            }
            tex.SetPixels32(pixels);
            tex.Apply(false,true);
            for (int i=0;i<faces.Length;i++)
                faces[i] = Sprite.Create(tex,new Rect(w*i,0,w,h),new Vector2(.5f,.5f),31,0,SpriteMeshType.FullRect);
        }

        void LateUpdate() => Refresh();
        void Refresh()
        {
            if (!display) return;
            display.enabled = visible == null || visible();
            if (!display.enabled) return;
            int value = charge();
            int tier = value <= 0 ? 0 : value < 40 ? 1 : value < 80 ? 2 : 3;
            if (tier != lastTier) { lastTier = tier; display.sprite = faces[tier]; }
            transform.localPosition = new Vector3(-.18f*Mathf.Sign(facing()), -.085f, 0);
        }
    }
}
