namespace Plugin.AncientCivilization
{
    // Shared course data; coordinates also drive the standalone layout checks.
    public static class AncientTraversalLayout
    {
        public struct Ledge
        {
            public readonly float left,right,top;
            public Ledge(float l,float r,float y){left=l;right=r;top=y;}
        }
        public static readonly Ledge[] MainRoute={
            new Ledge(128,140,0),new Ledge(143,147,1.5f),new Ledge(152,157,2.2f),
            new Ledge(162,167,1.5f),new Ledge(172,177,2.8f),new Ledge(182,187,1.6f),
            new Ledge(192,198,2.5f),new Ledge(203,208,1.8f),new Ledge(214,220,3),
            new Ledge(226,232,1.8f),new Ledge(238,244,2),new Ledge(250,264,0)};
        public static readonly Ledge[] FastRoute={new Ledge(147,149,5.8f),new Ledge(166,168,6.2f),
            new Ledge(188,190,6.4f),new Ledge(211,213,6.2f),new Ledge(234,236,6.4f)};
        public static readonly float[] TargetX={150,168,185,203,220,237};
        public static readonly float[] TargetY={5.4f,6,6.8f,5.7f,6.5f,5.3f};
        // Lower factory rooftops keep the truck/motorcycle road clear underneath.
        public static readonly Ledge[] LowerRoute={new Ledge(22,27,3.4f),new Ledge(31,36,3.6f),
            new Ledge(40,45,4.8f),new Ledge(51,56,5.2f),new Ledge(62,67,4.3f),
            new Ledge(73,78,3.5f),new Ledge(84,89,4),new Ledge(94,98,3.5f)};
        public static readonly Ledge[] LowerFastRoute={new Ledge(34,36,6.4f),new Ledge(56,58,6.6f),
            new Ledge(78,80,6.4f)};
        public const float LowerStart=23,LowerFinish=96;
        public const float Start=139,Finish=252,Ceiling=10;
    }
}
