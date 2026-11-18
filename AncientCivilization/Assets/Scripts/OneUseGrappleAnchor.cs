namespace Plugin.PlayerCore
{
    // 도착을 성공했을 때만 소모한다. 벽에 막힌 발사는 앵커를 잃지 않는다.
    public sealed class OneUseGrappleAnchor : GrappleAnchor {protected override bool Consumable=>true;}
}
