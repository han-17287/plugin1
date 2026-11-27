using System;
using Plugin.PlayerCore;

internal static class ElectricPlugChecks
{
    static int passed;
    static void Require(bool result,string test)
    {
        if(!result)throw new InvalidOperationException(test);
        passed++;
    }

    static void Close(float actual,float expected,string test) => Require(Math.Abs(actual-expected)<.0005f,test);

    static void Contact()
    {
        Require(ElectricPlugRules.SegmentHit(20,0,8,0,.6f,out var fraction),"긴 틱 도중 충돌");
        Close(fraction,.37f,"첫 접촉 시점");
        Require(!ElectricPlugRules.SegmentHit(4,0,8,0,.6f,out _),"벽에서 잘린 투척");
        Require(!ElectricPlugRules.SegmentHit(4,0,-1,0,.6f,out _),"등 뒤의 표적");
        Require(!ElectricPlugRules.SegmentHit(10,0,2,.62f,.6f,out _),"빗나간 투척");
        Require(ElectricPlugRules.SegmentHit(2,0,1,1,1,out var tangent),"접선 접촉");
        Close(tangent,.5f,"접선 접촉 시간");
        Require(ElectricPlugRules.SegmentHit(0,0,.2f,.1f,.5f,out var t) && t==0,"겹친 출발점");
        Require(!ElectricPlugRules.SegmentHit(0,0,3,0,.5f,out _),"정지한 투척 끝점");
    }

    static void Main()
    {
        Contact();
        var clock=new ElectricPlugClock<object>();
        var a=new object();var b=new object();
        Require(clock.Spin(a,.4f) && !clock.Spin(a,.4f) && clock.Spin(b,.4f),"회전 쿨타임은 대상별");
        Require(clock.Chain(a,.6f) && !clock.Chain(a,.6f),"전이 재접촉 제한");
        clock.Tick(.39f,(_,_)=>{});
        Require(!clock.Spin(a,.4f),"400ms 이전 피해 중복");
        clock.Tick(.011f,(_,_)=>{});
        Require(clock.Spin(a,.4f) && !clock.Chain(a,.6f),"400ms 후 회전만 다시 허용");
        clock.Tick(.201f,(_,_)=>{});
        Require(clock.Chain(a,.6f),"전이 쿨다운 종료");
        foreach(int fps in new[]{24,30,50,60,120,240})
        {
            clock.Clear();float total=0;int ticks=0;
            clock.ShockTarget(a,2,8,.5f);
            for(int i=0;i<fps*2+2;i++)clock.Tick(1f/fps,(o,d)=>{total+=d;ticks++;});
            Close(total,16,"2초간 감전 총량 @"+fps);
            Require(ticks==4 && clock.ShockRemaining(a)==0,"감전 종료 @"+fps);
        }
        clock.Clear();float refreshed=0;
        Action<object,float> charge=(o,d)=>refreshed+=d;
        clock.ShockTarget(a,2,8,.5f);
        clock.Tick(.25f,charge);clock.ShockTarget(a,2,8,.5f);clock.Tick(0,charge);
        Close(clock.ShockRemaining(a),2,"갱신으로 중첩 없는 연장");
        clock.Tick(4,charge);Close(refreshed,18,"긴 틱/남은 부분 틱 지급");
        clock.Clear();int events=0;
        clock.ShockTarget(a,2,8,.5f);clock.ShockTarget(b,2,8,.5f);
        clock.Tick(.5f,(o,d)=>{events++;clock.Clear();});
        Require(events==1 && clock.Time==0 && clock.ShockRemaining(a)==0,"피격 중 재시도 시 콜백 폐기");
        Require(clock.Spin(a,.4f) && clock.Chain(a,.6f),"방 이동 후 새 입력");
        Console.WriteLine("PASS ElectricPlug "+passed+" assertions");
    }
}
