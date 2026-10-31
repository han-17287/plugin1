using System.Collections.Generic;
using UnityEngine;

namespace Plugin.AncientCivilization
{
    // Atlas rectangles are measured against the shipped PNGs, in top-left pixels.
    // Keep source images intact; Unity creates animation sprites from their alpha bounds.
    public static class AncientArt
    {
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        public static Texture2D Texture(string name)
        {
            Texture2D texture;
            if (textures.TryGetValue(name, out texture) && texture) return texture;
            texture = Resources.Load<Texture2D>("Art/" + name);
            if (!texture) throw new System.InvalidOperationException("Missing Art/" + name + ".png");
            texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp;
            textures[name] = texture; return texture;
        }
        public static Sprite Slice(string sheet, string key, Rect topRect, float pixelsPerUnit, Vector2 pivot, bool trim = true, bool stableX = false)
        {
            Sprite result;
            if (sprites.TryGetValue(key, out result) && result) return result;
            Texture2D texture = Texture(sheet);
            int x = Mathf.RoundToInt(topRect.x), y = texture.height - Mathf.RoundToInt(topRect.yMax);
            int width = Mathf.RoundToInt(topRect.width), height = Mathf.RoundToInt(topRect.height);
            if (trim)
            {
                Color32[] pixels = texture.GetPixels32();
                int left = width, right = -1, bottom = height, top = -1;
                for (int row = 0; row < height; row++) for (int col = 0; col < width; col++)
                {
                    if (pixels[(y + row) * texture.width + x + col].a < 110) continue;
                    left = Mathf.Min(left, col); right = Mathf.Max(right, col);
                    bottom = Mathf.Min(bottom, row); top = Mathf.Max(top, row);
                }
                if (right >= left && top >= bottom)
                {
                    left = Mathf.Max(0, left - 3); bottom = Mathf.Max(0, bottom - 3);
                    right = Mathf.Min(width - 1, right + 3); top = Mathf.Min(height - 1, top + 3);
                    float rootX=pivot.x*width;
                    x += left; y += bottom; width = right - left + 1; height = top - bottom + 1;
                    if(stableX)pivot.x=(rootX-left)/width;
                }
            }
            result = Sprite.Create(texture, new Rect(x, y, width, height), pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect);
            result.name = key; sprites[key] = result; return result;
        }
        public static Sprite Hero(int frame)
        {
            Rect cell = new Rect(frame % 4 * 384, frame / 4 * 512, 384, 512);
            // The absorption pose's rear boot crosses the nominal grid by eight pixels.
            // Split at the actual transparent gutter so neither pose contains a neighbour.
            if (frame == 5) cell = new Rect(384, 512, 376, 512);
            if (frame == 6) cell = new Rect(760, 512, 392, 512);
            float root=frame==6?223:215;
            return Slice("Hero", "hero-" + frame, cell, 195, new Vector2(root/cell.width,0),true,true);
        }
        public static Sprite World(int index)
        {
            Rect[] cells = {
                new Rect(0,244,570,236), new Rect(600,0,166,510),
                new Rect(790,150,336,310), new Rect(1153,0,383,510),
                new Rect(0,535,408,460), new Rect(408,535,342,460),
                new Rect(752,515,416,490), new Rect(1168,735,368,260)
            };
            float[] ppu = {100,100,170,85,180,180,140,135};
            Vector2 pivot = index == 0 ? new Vector2(.5f,1) : index == 2 ? Vector2.one*.5f : new Vector2(.5f,0);
            return Slice("World", "world-"+index, cells[index], ppu[index], pivot, index != 0);
        }
        public static Sprite Action(int frame)
        {
            Rect cell=new Rect(frame%4*384,frame/4*512,384,512);
            if(frame==1)cell=new Rect(384,0,392,512);
            if(frame==2)cell=new Rect(776,0,376,512);
            float root=frame==2?207:215;
            return Slice("Actions","action-"+frame,cell,225,new Vector2(root/cell.width,0),true,true);
        }
        public static Sprite Rooftop(float width,bool tall)
        {
            int pixels=Mathf.Clamp(Mathf.RoundToInt(width*256),1,1536);
            return Slice("Rooftop","roof-"+pixels+"-"+tall,new Rect(0,16,pixels,tall?1008:256),256,new Vector2(.5f,1),false);
        }
        public static Sprite Scrap()
        {var t=Texture("Scrap");return Slice("Scrap","thrown-scrap",new Rect(0,0,t.width,t.height),1100,Vector2.one*.5f);}
        public static Sprite Enemy(int kind)
        {
            // Hand-measured gutters keep weapons and the boss's gauntlet intact.
            Rect[] enemyCells={new Rect(0,0,512,500),new Rect(512,0,488,500),new Rect(1040,0,496,495),
                new Rect(0,600,512,380),new Rect(512,530,466,494),new Rect(990,495,546,529)};
            int index=kind==3?5:kind==4?3:kind==5?4:kind;
            return Slice("Enemies","enemy-"+kind,enemyCells[index],kind==3?135:kind==4?150:185,new Vector2(.5f,0));
        }
        public static Sprite Motorcycle(int part)
        {
            Rect[] bikeCells={new Rect(0,0,950,512),new Rect(980,80,470,432),new Rect(0,520,730,504),new Rect(730,560,806,464)};
            float ppu=part==1?500:320;
            Vector2 pivot=part==2?new Vector2(467f/730,0):Vector2.one*.5f;
            return Slice("Motorcycle","motorcycle-"+part,bikeCells[part],ppu,pivot,true,part==2);
        }
        public static Sprite City()
        {return Slice("City", "city", new Rect(0,0,1536,1024), 32, Vector2.one*.5f,false);}
        public static SpriteRenderer Display(string name, Sprite sprite, Vector2 position, int order, Transform parent = null)
        {
            var o = new GameObject(name); var renderer = o.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite; renderer.sortingOrder = order;
            if(parent) o.transform.SetParent(parent, false);
            o.transform.localPosition = position; return renderer;
        }
    }
}
