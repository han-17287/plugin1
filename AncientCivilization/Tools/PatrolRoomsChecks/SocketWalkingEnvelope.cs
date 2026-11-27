using System;
using System.Collections.Generic;
using UnityEngine;

namespace Plugin.PlayerCore
{
    public sealed partial class PatrolRoom
    {
        // 엔진 밖에서 방을 검사하므로 캡슐 대신 조금 더 보수적인 사각형을 쓴다.
        // 도약은 기본 점프 하나와 달리기만 허용한다. 지지면을 밟지 않은 채 공중에서 다시 뛰지 않는다.
        sealed class WalkingEnvelope
        {
            readonly struct Surface
            {
                public readonly float Left, Right, Top, Bottom;
                public readonly bool OneWay;
                public Surface(BoxCollider2D box)
                {
                    var center = (Vector2)box.transform.position + box.offset;
                    float a = Mathf.Deg2Rad * box.transform.localRotation.ZDegrees;
                    float c = Mathf.Abs(Mathf.Cos(a)), s = Mathf.Abs(Mathf.Sin(a));
                    float halfWidth = (c * box.size.x + s * box.size.y) * .5f;
                    float halfHeight = (s * box.size.x + c * box.size.y) * .5f;
                    Left=center.x-halfWidth; Right=center.x+halfWidth;
                    Bottom=center.y-halfHeight; Top=center.y+halfHeight;
                    OneWay=box.usedByEffector;
                }
                public bool Supports(float x) => x > Left-.15f && x < Right+.15f;
            }

            const float HalfWidth=.36f, Height=1.8f;
            static readonly Dictionary<string,float> Movement=SocketMovementTuning();
            static readonly float Jump=Movement["jumpSpeed"];
            static readonly float RiseGravity=Movement["gravity"];
            static readonly float FallGravity=RiseGravity*Movement["fallMultiplier"];
            readonly List<Surface> surfaces=new List<Surface>();
            readonly List<Vector2>[] ledges=new List<Vector2>[89];
            readonly int direction;

            public WalkingEnvelope(PatrolRoom room, int direction)
            {
                this.direction=direction;
                foreach (var (_,go) in room.objects)
                {
                    var box=go.GetComponent<BoxCollider2D>();
                    if (box && box.enabled && go.activeSelf) surfaces.Add(new Surface(box));
                }
                for (int i=0;i<ledges.Length;i++)
                {
                    float x=2+i*.5f;
                    var candidates=ledges[i]=new List<Vector2>();
                    foreach (var s in surfaces)
                    {
                        var node=new Vector2(x,s.Top);
                        if (!s.Supports(x) || s.Top>9 || s.Top < -2 || candidates.Contains(node)) continue;
                        if (Clear(node)) candidates.Add(node);
                    }
                }
            }

            bool Clear(Vector2 feet)
            {
                foreach(var s in surfaces)
                    if (!s.OneWay && feet.x+HalfWidth>s.Left && feet.x-HalfWidth<s.Right &&
                        feet.y+Height>s.Bottom && feet.y+.035f<s.Top)
                        return false;
                return true;
            }

            bool CanJump(Vector2 from,Vector2 to)
            {
                float height = to.y-from.y;
                float peak=Jump*Jump/(2*RiseGravity);
                if (height > peak-.025f) return false;
                float ascent=Jump/RiseGravity;
                float duration=ascent+(float)Math.Sqrt(2*(peak-height)/FallGravity);
                // 기본 달리기 속도의 86%만 사용해 이륙 직전의 속도 오차를 남긴다.
                if (Math.Abs(to.x-from.x)>Movement["runSpeed"]*.86f*duration) return false;
                int steps=Mathf.CeilToInt(duration/.024f);
                float priorY=from.y;
                for (int k=1;k<=steps;k++)
                {
                    float u=(float)k/steps, t=u*duration;
                    float y=t<=ascent ? from.y+Jump*t-.5f*RiseGravity*t*t :
                        from.y+peak-.5f*FallGravity*(t-ascent)*(t-ascent);
                    var feet=new Vector2(Mathf.Lerp(from.x,to.x,u),y);
                    if (!Clear(feet)) return false;
                    foreach(var s in surfaces)
                        if (s.OneWay && s.Supports(feet.x) && priorY>=s.Top && feet.y<s.Top-.04f)
                            return false;
                    priorY=y;
                }
                return true;
            }

            public bool Traversable()
            {
                int start=direction>0 ? 0 : ledges.Length-1;
                int end=direction>0 ? ledges.Length-1 : 0;
                var queue=new Queue<(int index,Vector2 feet)>();
                var seen=new HashSet<(int index,float height)>();
                foreach(var feet in ledges[start])
                    if (Math.Abs(feet.y)<.05f) { seen.Add((start,feet.y)); queue.Enqueue((start,feet)); }
                while(queue.Count>0)
                {
                    var (index,feet)=queue.Dequeue();
                    if(index==end && Math.Abs(feet.y)<.05f) return true;
                    for(int step=0;step<=14;step++)
                    {
                        int next=index+step*direction;
                        if(next<0 || next>=ledges.Length) break;
                        foreach(var dest in ledges[next])
                        {
                            if (seen.Contains((next,dest.y))) continue;
                            bool walk=step==1 && Math.Abs(dest.y-feet.y)<.02f && Clear(Vector2.Lerp(feet,dest,.5f));
                            if (!walk && !CanJump(feet,dest)) continue;
                            seen.Add((next,dest.y));
                            queue.Enqueue((next,dest));
                        }
                    }
                }
                return false;
            }
        }
    }
}
