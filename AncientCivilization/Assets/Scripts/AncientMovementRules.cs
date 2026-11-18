using System;
namespace Plugin.AncientCivilization
{
    public enum AncientJumpKind { None,Ground,Air }
    public static class AncientMovementRules
    {
        public const float BufferSeconds=.12f,CoyoteSeconds=.12f;
        public static bool StableGround(bool contact,float relativeX,float relativeY,float normalX,float normalY)
        {
            // A nearby floor must not rearm a jump while the capsule is leaving it.
            // Project onto the support normal so running uphill still counts as grounded.
            if(!contact||!Finite(relativeX)||!Finite(relativeY)||!Finite(normalX)||!Finite(normalY))return false;
            float length=(float)Math.Sqrt(normalX*normalX+normalY*normalY);
            return length>.0001f&&(relativeX*normalX+relativeY*normalY)/length<=.5f;
        }
        public static AncientJumpKind ChooseJump(float now,float bufferedUntil,float groundedAt,bool airUnlocked,bool airUsed)
        {
            if(!Finite(now)||!Finite(bufferedUntil)||now>=bufferedUntil)return AncientJumpKind.None;
            float groundAge=now-groundedAt;
            if(Finite(groundedAt)&&groundAge>=0&&groundAge<CoyoteSeconds)return AncientJumpKind.Ground;
            return airUnlocked&&!airUsed?AncientJumpKind.Air:AncientJumpKind.None;
        }
        public static float LaunchSpeed(AncientJumpKind kind,bool held)
        {
            float speed=kind==AncientJumpKind.Ground?14:kind==AncientJumpKind.Air?12:0;
            return held?speed:ReleaseSpeed(speed);
        }
        public static float ReleaseSpeed(float speed){return speed>4?speed*.55f:speed;}
        static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
    }
}
