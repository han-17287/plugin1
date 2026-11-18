using UnityEngine;
namespace Plugin.PlayerCore
{
    // 월드 표지판을 2D 화면에 투영한다. MeshRenderer/3D 텍스트는 사용하지 않는다.
    public sealed class DistrictWorldSign : MonoBehaviour
    {
        public string text;
        static GUIStyle style;Camera view;
        void Awake(){view=Camera.main;}
        void OnGUI()
        {
            if(!view||string.IsNullOrEmpty(text))return;
            Vector3 p=view.WorldToScreenPoint(transform.position);
            if(p.z<0||p.x<-500||p.x>Screen.width||p.y<0||p.y>Screen.height)return;
            if(style==null)style=new GUIStyle(GUI.skin.label){font=Resources.Load<Font>("Fonts/NeoDunggeunmo"),wordWrap=false};
            style.fontSize=Mathf.Clamp(Mathf.RoundToInt(Screen.height/view.orthographicSize*.13f),10,20);
            Color old=GUI.color;int depth=GUI.depth;GUI.depth=20;GUI.color=new Color(.64f,.79f,.79f,.9f);
            GUI.Label(new Rect(p.x,Screen.height-p.y,Mathf.Min(600,Screen.width-p.x),style.fontSize+8),text,style);GUI.color=old;GUI.depth=depth;
        }
    }
}
