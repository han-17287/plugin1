using UnityEngine;
namespace Plugin.AncientCivilization
{
    public static class AncientCitizenArt
    {
        public static Sprite ForCitizen(int id){return AncientArt.Citizen(id);}
        static readonly Sprite[] cache=new Sprite[4];
        static readonly string[] body={
            "................",".....######.....","....########....","....########....",".....######.....",".....######.....","....########....","...##########...","..############..","..############..","..############..","..############..","..############..","..############..","...##########...","...##########...","...##########...","...##########...","...##########...","....########....","....########....","....###..###....","....###..###....","....###..###....","....###..###....","....###..###....","....###..###....","....###..###....","...####..####...","...####..####...","................","................"};
        public static Sprite Sprite(int role)
        {
            role=Mathf.Clamp(role,0,3);if(cache[role])return cache[role];
            var pixels=new Color32[16*32];Color coat=role==0?new Color(.68f,.46f,.22f):role==1?new Color(.85f,.9f,.85f):role==2?new Color(.3f,.57f,.6f):new Color(.47f,.45f,.65f);
            for(int y=0;y<32;y++)for(int x=0;x<16;x++)
            {
                if(body[y][x]!='#')continue;Color color=coat;
                if(y<6||y>20)color=new Color(.11f,.16f,.22f);
                if(y>=3&&y<=5&&x>=6&&x<=9)color=new Color(.58f,.44f,.32f);
                if(x==3||x==12||y==18)color*=.6f;
                if(y>=8&&y<=15&&(x==6||x==9))color=role==1?new Color(.7f,.25f,.2f):new Color(.82f,.84f,.71f);
                color.a=1;pixels[(31-y)*16+x]=color;
            }
            var texture=new Texture2D(16,32,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,name="Citizen role "+role};
            texture.SetPixels32(pixels);texture.Apply();cache[role]=UnityEngine.Sprite.Create(texture,new Rect(0,0,16,32),new Vector2(.5f,2/32f),16,0,SpriteMeshType.FullRect);return cache[role];
        }
    }
}
