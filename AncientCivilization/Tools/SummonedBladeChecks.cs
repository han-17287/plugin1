using System;
using Plugin.PlayerCore;

static class SummonedBladeChecks
{
    static int count;
    static void Check(bool test,string message) { if (!test) throw new InvalidOperationException(message); count++; }

    static void VerifyAt(int frequency)
    {
        float dt=1f/frequency;
        var b=new SummonedBladeRules();
        Check(!b.Equip() && !b.Detach(20,15) && !b.Land(20), "빈 손 상태에서 건너뛴 전이");
        Check(b.Begin(1.6f) && !b.Begin(1.3f),"소환은 한 번만");
        var pending = b.Snapshot(12);
        Check(!pending.equipped && pending.cooldown==0 && b.State==SummonedBladeState.Summoning,"예고 중 저장은 상태 보존");
        for (int i=0;i<frequency;i++) b.AdvanceWarning(dt);
        Check(!b.Catchable && b.State==SummonedBladeState.Summoning,"예고 중 손잡이 비활성");
        b.Retarget(1.6f);
        b.AdvanceWarning(1f);
        Check(b.State==SummonedBladeState.Summoning,"재추첨한 위치도 예고한다");
        b.AdvanceWarning(.7f);
        Check(b.Catchable && b.State==SummonedBladeState.Falling,"낙하 도중 장착 가능");
        b.Retarget(.75f);
        Check(!b.Catchable && b.AdvanceWarning(.76f),"낙하 시 새로 생긴 위험은 새 예고부터");
        Check(b.Equip() && !b.Land(20),"낙하 중 장착 뒤 기존 충격 중복 금지");
        var next = new SummonedBladeRules();
        next.EnterRoom(b.Snapshot(12));
        Check(next.Equipped && !next.Catchable,"다음 방은 손에 든 무기로 입장");
        Check(b.Equipped && !b.Equip() && !b.Begin(1),"장착 상태의 추가 소환 또는 재장착 금지");
        Check(b.Detach(20,15) && !b.Detach(20,15),"설치 한 번");
        next.Reset();
        next.EnterRoom(b.Snapshot(12));
        Check(next.State==SummonedBladeState.Cooldown && next.Remaining==12 && next.FieldRemaining==0,"설치한 칼은 방 이동 후 쿨타임");
        next.AdvanceCooldown(4.25f);
        var third = new SummonedBladeRules();
        third.EnterRoom(next.Snapshot(12));
        Check(third.State==SummonedBladeState.Cooldown && third.Remaining==7.75f,"연속 방 이동은 남은 쿨타임만");
        float dealt=0;
        for(int i=0;i<=frequency*20+1;i++)
        {
            dealt+=8*b.AdvanceInstalled(dt);
            if(i==frequency*16)Check(b.Catchable && b.FieldRemaining==0,"전기 장판이 먼저 꺼진다");
        }
        Check(Math.Abs(dealt-120)<.05f && b.Remaining==0,"프레임과 관계없이 15초 총 피해 120");
        b.Break(12);
        Check(!b.Catchable && !b.Equipped && !b.Begin(1),"소멸 뒤 대기");
        b.AdvanceCooldown(11.5f);
        Check(!b.Begin(1),"재소환 대기 시간이 남는다");
        b.AdvanceCooldown(.51f);
        Check(b.Begin(1),"새 소환 복구");
        b.AdvanceWarning(1);
        Check(b.Land(20) && !b.Land(20),"낙하 충격 1회");
        Check(b.AdvanceInstalled(1)==0,"분리하기 전에는 바닥 장판이 켜지지 않는다");
        b.Reset();
        Check(b.State==SummonedBladeState.None && b.FieldRemaining==0 && !b.Catchable,"체크포인트 정리");
    }

    public static void Main()
    {
        foreach (int rate in new[]{30,50,60,120,240}) VerifyAt(rate);
        Check(SummonedBladeRules.Hit(100,12,4,220)==148,"거리 4 견인 일격");
        Check(SummonedBladeRules.Hit(100,12,12,220)==220,"거리 상한");
        Check(SummonedBladeRules.Hit(0,6,5,300)==30,"부분 충전 방출");
        Check(SummonedBladeRules.Hit(0,6,100,300)==300,"완충 방출 상한");
        var transfer = new SummonedBladeRules();
        int wallet = 30;
        transfer.Begin(1,20); wallet-=20;
        Check(wallet+transfer.ReservedParticles==30,"소환 마커 상태 저장의 잔액");
        transfer.AdvanceWarning(1); transfer.Retarget(.5f);
        Check(wallet+transfer.ReservedParticles==30,"위험 지점 재추첨 시 추가 차감 금지");
        transfer.AdvanceWarning(.5f);
        wallet+=transfer.RefundReservation();
        wallet+=transfer.RefundReservation();
        transfer.Reset();
        Check(wallet==30 && transfer.ReservedParticles==0,"방 전환/재시도 중복 호출 후 입자 보존");
        transfer.Begin(1,20); wallet-=20; transfer.AdvanceWarning(1); transfer.Land(20);
        Check(transfer.RefundReservation()==0 && wallet==10,"이미 낙하한 블레이드 비용 확정");
        transfer.Reset(); transfer.Begin(1,7); transfer.AdvanceWarning(1); transfer.Equip();
        Check(transfer.RefundReservation()==0,"완전 장착 시 소환 비용 확정");
        Console.WriteLine($"PASS: {count} summon, falling catch, detach, field, discharge, cooldown and zip/overload checks");
    }
}
