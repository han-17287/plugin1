namespace Plugin.PlayerCore
{
    // M1 실측 전 저작 제한이다. 실제 조작 성공률을 의미하지 않는다.
    public static class TraversalLayout
    {
        public static float ExitX(int count,int gap){return 6+(count+1)*gap;}
        public static float Width(int count,int gap){return ExitX(count,gap)+6;}
        public static void Anchor(int index,out float x,out float y,int gap)
        {x=6+index*gap;y=index==0?4:index%2==0?6:5;}
        public static readonly int[] FlowTiles={96,144,180,120,72};
        public static readonly int[] ChallengeTiles={32,48,60,40,24};
        public static readonly int[] RestTiles={16,16,20,20,24};
        public static readonly string[] Stages={"교대 통로","운송 벨트","검수 회랑","장비 검수실","코어 재고 관리실"};
    }
}
