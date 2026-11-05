using UnityEngine;
namespace Plugin.AncientCivilization
{
    // Cache formatted labels by their actual values; IMGUI repaint frequency need not allocate strings.
    public sealed class AncientHudText
    {
        public string Resources="",Objective="",Movement="",Performance="",Mode="",Capture="",Blade="",Boss="",Speed="",CoreDistance="";
        static readonly string[] cuts={"세로 베기","가로 베기","사선 마무리"};
        int bladeStep=-1;bool bladeActive,bladeQueued;
        int hp=-1,energy=-1,credits=-1,cores=-1,stage=-1,floor=-1,chain=-1,capture=-1,speed=-1,metres=-1;
        bool bike,running,attack,quality,enraged,wired,finale;AncientEnemy boss;AncientPickup core;
        string performance;
        public void Refresh(AncientGame game)
        {
            var hero=game.hero;int h=Mathf.CeilToInt(hero.hp),e=Mathf.FloorToInt(hero.CaptureEnergy);
            if(h!=hp||e!=energy||game.credits!=credits)
            {hp=h;energy=e;credits=game.credits;Resources="체력 "+h+"   입자 "+e+"   재화 "+credits;}
            bool chase=game.Finale&&game.Finale.Active;
            if(game.Cores!=cores||game.Story.Stage!=stage||game.Story.CurrentFloor!=floor||chase!=finale)
            {
                cores=game.Cores;stage=game.Story.Stage;floor=game.Story.CurrentFloor;finale=chase;
                string goal=chase?"하강 화물차 추격":stage<4?"순찰 훈련 완료":stage<=5?"도난 코어 회수":stage==6?"용의자 확보":AncientCityLayout.Name(floor)+" 현장 조사";
                Objective="에너지 코어 "+cores+" / 3\n"+goal;
            }
            if(Movement.Length==0||bike!=hero.bike||running!=hero.running||chain!=hero.WireChain||wired!=hero.Wired)
            {bike=hero.bike;running=hero.running;chain=hero.WireChain;wired=hero.Wired;Movement=(bike?"오토바이":running?"달리기":"걷기")+(chain>0?" · 와이어 연결 "+chain:"")+" · K 정비";}
            if(Mode.Length==0||attack!=hero.AttackMode){attack=hero.AttackMode;Mode="휠 / "+(attack?"공격":"와이어")+" · 우클릭 입자 수집";}
            if(hero.Combat)
            {
                bool active=hero.Combat.BladeAnimating,queued=hero.Combat.BladeBuffered;int cut=active?hero.Combat.BladeStep:hero.Combat.NextBladeStep;
                if(cut!=bladeStep||active!=bladeActive||queued!=bladeQueued||Blade.Length==0)
                {bladeStep=cut;bladeActive=active;bladeQueued=queued;Blade=(active?"":"다음 · ")+cuts[Mathf.Clamp(cut-1,0,2)]+(queued?" · 입력 연결됨":"");}
            }
            if(capture!=hero.CaptureStage||Capture.Length==0||e!=lastCaptureEnergy)
            {capture=hero.CaptureStage;lastCaptureEnergy=e;Capture=e>=100?"완충 · 최대 출력 · 100%":"축적 "+capture+" / 3단계 · "+e+"%";}
            if(performance!=game.Performance.Summary||quality!=AncientVisuals.HighQuality||Performance.Length==0)
            {performance=game.Performance.Summary;quality=AncientVisuals.HighQuality;Performance=(quality?"F6 고품질":"F6 가볍게")+" · "+performance;}
            if(boss!=game.boss||(game.boss&&enraged!=game.boss.Enraged)||(game.Sniper&&lastSniperPhase!=game.Sniper.Phase))
            {boss=game.boss;enraged=boss&&boss.Enraged;lastSniperPhase=game.Sniper?game.Sniper.Phase:-1;Boss=boss?boss.Role+(game.Sniper?" / "+lastSniperPhase+"페이즈":enraged?" / 2단계":""):"";}
            int velocity=Mathf.RoundToInt(Mathf.Abs(hero.HorizontalSpeed)*3.6f);
            if(velocity!=speed){speed=velocity;Speed=velocity+" km/h";}
        }
        int lastCaptureEnergy=-1,lastSniperPhase=-1;
        public void Distance(AncientPickup item,float value)
        {
            int current=Mathf.CeilToInt(value);
            if(item!=core||current!=metres){core=item;metres=current;CoreDistance="코어 "+current+"m";}
        }
    }
}
