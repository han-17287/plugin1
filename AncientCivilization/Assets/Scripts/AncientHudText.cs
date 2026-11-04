using UnityEngine;
namespace Plugin.AncientCivilization
{
    // Cache formatted labels by their actual values; IMGUI repaint frequency need not allocate strings.
    public sealed class AncientHudText
    {
        public string Resources="",Objective="",Movement="",Performance="",Mode="",Capture="",Boss="",Speed="",CoreDistance="";
        int hp=-1,energy=-1,credits=-1,cores=-1,stage=-1,floor=-1,chain=-1,capture=-1,speed=-1,metres=-1;
        bool bike,running,attack,quality,enraged,wired,finale;AncientEnemy boss;AncientPickup core;
        string performance;
        public void Refresh(AncientGame game)
        {
            var hero=game.hero;int h=Mathf.CeilToInt(hero.hp),e=Mathf.RoundToInt(hero.CaptureEnergy);
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
            if(capture!=hero.CaptureStage||Capture.Length==0||e!=lastCaptureEnergy)
            {capture=hero.CaptureStage;lastCaptureEnergy=e;Capture="축적 "+capture+" / 3단계 · "+e+"%";}
            if(performance!=game.Performance.Summary||quality!=AncientVisuals.HighQuality||Performance.Length==0)
            {performance=game.Performance.Summary;quality=AncientVisuals.HighQuality;Performance=(quality?"F6 고품질":"F6 가볍게")+" · "+performance;}
            if(boss!=game.boss||(game.boss&&enraged!=game.boss.Enraged))
            {boss=game.boss;enraged=boss&&boss.Enraged;Boss=boss?boss.Role+(enraged?" / 격노":""):"";}
            int velocity=Mathf.RoundToInt(Mathf.Abs(hero.HorizontalSpeed)*3.6f);
            if(velocity!=speed){speed=velocity;Speed=velocity+" km/h";}
        }
        int lastCaptureEnergy=-1;
        public void Distance(AncientPickup item,float value)
        {
            int current=Mathf.CeilToInt(value);
            if(item!=core||current!=metres){core=item;metres=current;CoreDistance="코어 "+current+"m";}
        }
    }
}
