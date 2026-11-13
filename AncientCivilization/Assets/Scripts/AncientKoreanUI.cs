using UnityEngine;

namespace Plugin.AncientCivilization
{
    public static class AncientKoreanUI
    {
        static UnityEngine.Font cached;
        public static UnityEngine.Font Font
        {
            get
            {
                if(cached)return cached;
                cached=Resources.Load<UnityEngine.Font>("Fonts/NeoDunggeunmo");
                if(cached)return cached;
                try
                {
                    cached=UnityEngine.Font.CreateDynamicFontFromOSFont(new[]{
                        "Malgun Gothic","Apple SD Gothic Neo","Noto Sans CJK KR",
                        "Noto Sans KR","NanumGothic","Arial Unicode MS"},20);
                }
                catch(System.Exception){cached=null;}
                return cached?cached:GUI.skin.font;
            }
        }
    }
}
