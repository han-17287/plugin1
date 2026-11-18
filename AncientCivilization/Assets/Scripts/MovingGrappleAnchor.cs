using UnityEngine;
namespace Plugin.PlayerCore
{
    public sealed class MovingGrappleAnchor : GrappleAnchor
    {
        Vector2 origin;float age;bool initialized;
        protected override void OnEnable(){if(!initialized){origin=transform.position;initialized=true;}base.OnEnable();}
        void FixedUpdate()
        {
            if(Leased||!data||!clockOwner||clockOwner.Paused||!clockOwner.Ready||!Application.isFocused)return;
            age+=Time.fixedDeltaTime;float angle=age*2*Mathf.PI/Mathf.Max(.1f,data.movingPeriod);
            Vector2 offset=data.orbit?new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)):data.movingAxis.normalized*Mathf.Sin(angle);
            transform.position=origin+offset*data.movingAmplitude;
        }
        public override void ResetForRetry(){base.ResetForRetry();age=0;transform.position=origin;}
    }
}
