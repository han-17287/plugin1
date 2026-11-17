using UnityEngine;

namespace Plugin.AncientCivilization
{
    // Visual-only motion. The capsule, wire joint and launch velocity stay on the player root.
    public sealed class AncientSwingMotion
    {
        readonly SpriteRenderer art;
        readonly Sprite[] poses=new Sprite[8];
        float angle,angleVelocity,facing=1,releaseAt=-100,releaseAngle;
        bool wasSwinging;
        int frame;
        public bool Presenting {get;private set;}
        public Vector2 EmitterWorld
        {
            get
            {
                if(!art)return Vector2.zero;
                if(!Presenting||frame<0||frame>=poses.Length)return art.transform.position;
                Vector2 offset=EmitterLocal(frame,facing);
                return art.transform.TransformPoint(offset);
            }
        }
        public AncientSwingMotion(SpriteRenderer target)
        {art=target;for(int i=0;i<poses.Length;i++)poses[i]=AncientArt.Swing(i);}
        static Vector2 EmitterLocal(int pose,float direction)
        {
            Vector2 delta=AncientSwingAtlas.EmitterPixels[pose]-AncientSwingAtlas.PivotPixels[pose];
            return new Vector2(delta.x*(direction<0?-1:1),-delta.y)/AncientSwingAtlas.PixelsPerUnit;
        }
        public bool Apply(bool swinging,bool canRecover,Vector2 ropeDelta,Vector2 velocity,
            float attachedAge,float arc,float initialFacing,float now,float deltaTime)
        {
            if(swinging)
            {
                wasSwinging=true;releaseAt=-100;Presenting=true;
                facing=AncientSwingRules.Facing(velocity.x,initialFacing);
                frame=AncientSwingRules.AttachedFrame(attachedAge,arc,velocity.x,velocity.y);
                Vector2 emitter=EmitterLocal(frame,facing);
                float target=AncientSwingRules.AlignAngle(ropeDelta.x,ropeDelta.y,emitter.x,emitter.y);
                if(deltaTime>0)angle=Mathf.SmoothDampAngle(angle,target,ref angleVelocity,.075f,900,deltaTime);
            }
            else
            {
                if(wasSwinging){releaseAt=now;releaseAngle=angle;wasSwinging=false;}
                frame=canRecover?AncientSwingRules.ReleaseFrame(now-releaseAt):-1;
                if(frame<0){Clear();return false;}
                Presenting=true;angle=AncientSwingRules.ReleaseAngle(releaseAngle,now-releaseAt);angleVelocity=0;
            }
            art.sprite=poses[frame];art.flipX=facing<0;
            art.transform.localPosition=Vector3.zero;art.transform.localScale=Vector3.one;
            art.transform.localRotation=Quaternion.Euler(0,0,angle);
            return true;
        }
        public void Clear()
        {
            Presenting=wasSwinging=false;releaseAt=-100;angle=angleVelocity=releaseAngle=0;
            if(art)art.transform.localRotation=Quaternion.identity;
        }
    }
}
