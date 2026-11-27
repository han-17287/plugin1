using Plugin.AncientCivilization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Plugin.PlayerCore
{
    // 경위의 이동 루트와 분리해 설치한 전구는 원래 바닥에 남는다. 효과 선과 손잡이는 한 번만 만든다.
    internal sealed class SummonedBladePresentation
    {
        readonly GameObject root, steel;
        readonly SpriteRenderer body, light;
        readonly LineRenderer floor, fieldInner, flare, tail, cord, current;
        static readonly Vector2[] unitRing = BuildUnitRing(40);
        readonly Vector3[] ellipse = new Vector3[unitRing.Length];
        readonly Color amber = new Color(1,.57f,.16f), gold = new Color(1,.85f,.42f);
        Vector2 burstCenter;
        float burstSize, burstTime;
        internal readonly BluePlugSocket handle;
        readonly BluePlugPointLight bulbLight;

        internal SummonedBladePresentation(Scene scene)
        {
            root = new GameObject("소환 블레이드 · 지면과 전선");
            if (scene.IsValid() && scene != root.scene) SceneManager.MoveGameObjectToScene(root, scene);
            bulbLight = new BluePlugPointLight(root.transform);
            steel = new GameObject("필라멘트 칼날"); steel.transform.SetParent(root.transform,false);
            body = BluePlugArt.AddSprite(steel.transform,"전구 외장",FilamentBladeArt.Blade,Vector2.zero,31,.72f);
            light = BluePlugArt.AddSprite(steel.transform,"전구 심지",FilamentBladeArt.Emission,Vector2.zero,32,.72f);
            light.sharedMaterial = BluePlugArt.GlowMaterial;
            var connector = new GameObject("블레이드 손잡이 콘센트");
            connector.transform.SetParent(root.transform,false);
            connector.transform.localScale = Vector3.one * .48f;
            connector.AddComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder = 34;
            handle = connector.AddComponent<BluePlugSocket>();
            handle.Configure(Vector2.zero, BluePlugSkill.Tow, Vector2.right);
            floor = Line("낙하 표지와 전기 장판",.07f,15,amber,true);
            fieldInner = Line("전력파",.037f,16,gold,true);
            flare = Line("착지 방출파",.15f,35,gold,true);
            tail = Line("낙하 필라멘트 잔광",.10f,28,gold,false);
            cord = Line("왼손에서 손잡이까지 절연 피복",.110f,29,new Color(.09f,.12f,.17f),false,opaque:true);
            current = Line("손잡이로 흐르는 전류",.031f,30,BluePlugArt.MovementColor,false);
            tail.positionCount=2;
            cord.positionCount=current.positionCount=3;
            Hide(true);
        }

        LineRenderer Line(string name,float width,int order,Color color,bool loop,bool opaque=false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform,false);
            var line=go.AddComponent<LineRenderer>();
            line.sharedMaterial = opaque ? BluePlugArt.SolidMaterial : BluePlugArt.GlowMaterial;
            line.useWorldSpace=true; line.loop=loop; line.numCornerVertices=1;
            line.positionCount=loop ? ellipse.Length : 2;
            line.widthMultiplier=width;
            line.startColor=line.endColor=color;
            line.sortingOrder=order;
            return line;
        }

        static Vector2[] BuildUnitRing(int count)
        {
            var points = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                float angle = i * (2 * Mathf.PI / count);
                points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }
            return points;
        }

        void Oval(LineRenderer line,Vector2 center,float radius,float flat,Color colour)
        {
            for (int i=0;i<ellipse.Length;i++)
                ellipse[i]=center+new Vector2(unitRing[i].x*radius,unitRing[i].y*radius*flat);
            line.startColor=line.endColor=colour;
            line.SetPositions(ellipse);
            line.enabled=true;
        }

        internal void OnGround(SummonedBladeState state,Vector2 point,Vector2 normal,float drop,float grip,float radius,
            float electricity,float age,float warning)
        {
            bool preview=state==SummonedBladeState.Summoning;
            bool falling=state==SummonedBladeState.Falling;
            if (steel.activeSelf == preview) steel.SetActive(!preview);
            if (handle.gameObject.activeSelf == preview) handle.gameObject.SetActive(!preview);
            cord.enabled=current.enabled=false;
            tail.enabled=falling && AncientVisuals.HighQuality;
            if (preview)
            {
                bulbLight.Set(point,amber,.55f,1.6f);
                float t=Mathf.Clamp01(warning);
                Oval(floor,point+Vector2.up*.05f,.38f+t*.74f,.34f,amber);
                Oval(fieldInner,point+Vector2.up*.08f,.30f,.5f,gold);
                return;
            }
            body.enabled=true; light.enabled=AncientVisuals.HighQuality;
            var tip = point + Vector2.up*drop;
            steel.transform.position=tip; steel.transform.up=normal;
            Vector2 h = tip+normal*grip;
            handle.transform.position=h;
            bulbLight.Set(h, gold, falling ? .9f : .42f, falling ? 2.1f : 1.35f);
            if (falling)
            {
                tail.SetPosition(0,h); tail.SetPosition(1,h+Vector2.up*Mathf.Min(1.7f,drop+.25f));
                Oval(floor,point,.36f,.3f,amber);
            }
            else if(electricity>0)
            {
                Oval(floor,point+normal*.10f,radius,.24f,new Color(1,.59f,.18f,.83f));
                float t=Mathf.Repeat(age*.95f,1);
                if(AncientVisuals.HighQuality)
                    Oval(fieldInner,point+normal*.12f,radius*(.15f+.85f*t),.25f,new Color(1,.87f,.42f,1-t));
            }
            else floor.enabled=false;
            if(falling || electricity<=0 || !AncientVisuals.HighQuality) fieldInner.enabled=false;
        }

        internal void InHand(Vector2 left,Vector2 right,Vector2 tipDirection,float grip,bool draw)
        {
            if (!steel.activeSelf) steel.SetActive(true);
            body.enabled=draw; light.enabled=draw && AncientVisuals.HighQuality;
            if (handle.gameObject.activeSelf) handle.gameObject.SetActive(false);
            floor.enabled=fieldInner.enabled=tail.enabled=false;
            Vector2 direction=tipDirection.sqrMagnitude>.005f ? tipDirection.normalized : Vector2.right;
            steel.transform.position=right+direction*grip;
            steel.transform.up=-direction;
            bulbLight.Set(right, gold, .48f, 1.5f);
            Vector2 mid=(left+right)*.5f+Vector2.down*.16f;
            cord.enabled=current.enabled=true;
            cord.SetPosition(0,left);cord.SetPosition(1,mid);cord.SetPosition(2,right);
            current.SetPosition(0,left);current.SetPosition(1,mid);current.SetPosition(2,right);
        }

        internal void Pulse(Vector2 center,float radius)
        {
            burstCenter=center;burstSize=radius;burstTime=.2f;
            StepPulse(0);
        }

        internal void StepPulse(float dt)
        {
            if(burstTime<=0) return;
            burstTime=Mathf.Max(0,burstTime-dt);
            if(burstTime<=0){flare.enabled=false;return;}
            float t=1-burstTime/.2f;
            Oval(flare,burstCenter,Mathf.Lerp(.17f,burstSize,t),.68f,new Color(1,.86f,.50f,(1-t)*.88f));
        }

        internal void Hide(bool reset)
        {
            if (!root) return;
            bulbLight.Clear();
            if (steel.activeSelf) steel.SetActive(false);
            if (handle.gameObject.activeSelf) handle.gameObject.SetActive(false);
            floor.enabled=fieldInner.enabled=tail.enabled=cord.enabled=current.enabled=false;
            if(reset){flare.enabled=false;burstTime=0;}
        }
        internal void Dispose() { if(root) Object.Destroy(root); }
    }
}
