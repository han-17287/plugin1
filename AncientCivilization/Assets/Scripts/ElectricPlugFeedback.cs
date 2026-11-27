using UnityEngine;

namespace Plugin.PlayerCore
{
    // 같은 선 풀을 감전 아크와 도착 방전에 재사용한다. 표적 표시와 전투선은 별도 슬롯이다.
    internal sealed class ElectricPlugFeedback
    {
        const int Slots=9;
        readonly Transform root;
        readonly LineRenderer[] lines=new LineRenderer[Slots];
        readonly float[] life=new float[Slots];
        int next;
        static readonly Color Gold=new Color(1f,.82f,.35f,1);
        static readonly Color Current=new Color(.66f,.90f,1f,1);

        public ElectricPlugFeedback(Transform root) { this.root=root; }

        LineRenderer Rent(int count,Color colour,float width,float seconds,bool aim=false)
        {
            int i=aim ? Slots-1 : next++%(Slots-1);
            if(next>4096)next=0;
            var line=lines[i];
            if(!line)
            {
                var emitter=new GameObject("재사용 전기 신호");
                emitter.transform.SetParent(root,false);
                line=lines[i]=emitter.AddComponent<LineRenderer>();
                line.useWorldSpace=true;
                line.sharedMaterial=BluePlugArt.GlowMaterial;
                line.sortingOrder=25;
            }
            line.enabled=true;
            line.loop=false;
            line.positionCount=count;
            line.startColor=line.endColor=colour;
            line.widthMultiplier=width;
            life[i]=seconds;
            return line;
        }

        public void Aim(Vector2 p)
        {
            var line=Rent(4,Gold,.045f,.06f,true);
            line.loop=true;
            line.SetPosition(0,p+Vector2.up*.54f);line.SetPosition(1,p+Vector2.right*.43f);
            line.SetPosition(2,p+Vector2.down*.54f);line.SetPosition(3,p+Vector2.left*.43f);
        }
        public void Arc(Vector2 a,Vector2 b)
        {
            var line=Rent(8,Current,.067f,.12f);
            var delta=b-a;var side=new Vector2(-delta.y,delta.x).normalized*.17f;
            for(int i=0;i<8;i++)
            {
                Vector2 trace=Vector2.Lerp(a,b,i/7f);
                if(i>0 && i<7)trace+=side*((i&1)==0?1:-.72f);
                line.SetPosition(i,trace);
            }
        }
        public void Spark(Vector2 point,float size)
        {
            var line=Rent(5,Gold,.06f,.095f);
            line.SetPosition(0,point+new Vector2(-.5f,.6f)*size);
            line.SetPosition(1,point+Vector2.right*.12f*size);
            line.SetPosition(2,point+Vector2.up*size);
            line.SetPosition(3,point);
            line.SetPosition(4,point+new Vector2(.7f,.42f)*size);
        }
        public void Ring(Vector2 p,float radius)
        {
            var line=Rent(24,Current,.055f,.135f);
            line.loop=true;
            for(int i=0;i<24;i++)
            {
                float a=i*Mathf.PI/12f, r=radius*((i&1)==0?1:.94f);
                line.SetPosition(i,p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r);
            }
        }
        public void Step(float dt)
        {
            for(int i=0;i<Slots;i++)if(life[i]>0 && (life[i]-=dt)<=0 && lines[i])lines[i].enabled=false;
        }
        public void Clear()
        {
            for(int i=0;i<Slots;i++) {life[i]=0;if(lines[i])lines[i].enabled=false;}
        }
    }
}
