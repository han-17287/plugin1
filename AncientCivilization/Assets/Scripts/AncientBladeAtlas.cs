using UnityEngine;

namespace Plugin.AncientCivilization
{
    // Measured against the unchanged transparent source PNGs after the runtime four-pixel palette grid.
    // Source rectangles use top-left pixels; pivots anchor both boots to the same gameplay root.
    public static class AncientBladeAtlas
    {
        public const int FrameCount=16,GridSize=4;
        static readonly string[] Sheets={"BladeVertical","BladeHorizontal","BladeDiagonal"};
        static readonly float[] Ppu={90f,120f,116f};
        static readonly Rect[] Rectangles={
            // BladeVertical
            new Rect(81,107,114,186),
            new Rect(369,127,134,166),
            new Rect(681,115,134,178),
            new Rect(1005,43,130,250),
            new Rect(73,343,126,262),
            new Rect(365,399,190,206),
            new Rect(669,455,214,150),
            new Rect(989,455,202,150),
            new Rect(61,743,182,154),
            new Rect(357,751,178,146),
            new Rect(673,755,178,142),
            new Rect(985,747,174,150),
            new Rect(61,1051,174,158),
            new Rect(361,1047,162,162),
            new Rect(685,1043,146,166),
            new Rect(1009,1031,114,178),
            // BladeHorizontal
            new Rect(65,63,126,246),
            new Rect(353,71,154,238),
            new Rect(645,79,178,230),
            new Rect(953,91,190,218),
            new Rect(33,411,170,210),
            new Rect(341,407,210,214),
            new Rect(653,407,258,214),
            new Rect(961,411,282,210),
            new Rect(25,723,282,214),
            new Rect(337,723,266,214),
            new Rect(645,719,242,218),
            new Rect(965,715,194,222),
            new Rect(29,1023,162,226),
            new Rect(357,1015,146,234),
            new Rect(677,1011,138,238),
            new Rect(997,1003,134,246),
            // BladeDiagonal
            new Rect(73,71,130,238),
            new Rect(349,79,166,230),
            new Rect(645,91,190,218),
            new Rect(989,47,174,262),
            new Rect(45,343,194,278),
            new Rect(349,355,186,266),
            new Rect(657,407,290,214),
            new Rect(953,415,294,206),
            new Rect(13,727,286,210),
            new Rect(329,735,270,202),
            new Rect(649,743,250,194),
            new Rect(969,731,206,206),
            new Rect(45,1035,178,214),
            new Rect(353,1023,178,226),
            new Rect(673,1015,170,234),
            new Rect(985,1011,154,238),
        };
        static readonly Vector2[] Pivots={
            // BladeVertical
            new Vector2(0.653508772f,0.016129032f),
            new Vector2(0.645522388f,0.018072289f),
            new Vector2(0.585820896f,0.016853933f),
            new Vector2(0.557692308f,0.012000000f),
            new Vector2(0.559523810f,0.011450382f),
            new Vector2(0.423684211f,0.014563107f),
            new Vector2(0.357476636f,0.020000000f),
            new Vector2(0.358910891f,0.020000000f),
            new Vector2(0.409340659f,0.019480519f),
            new Vector2(0.485955056f,0.020547945f),
            new Vector2(0.463483146f,0.021126761f),
            new Vector2(0.474137931f,0.020000000f),
            new Vector2(0.485632184f,0.018987342f),
            new Vector2(0.533950617f,0.018518519f),
            new Vector2(0.496575342f,0.018072289f),
            new Vector2(0.548245614f,0.016853933f),
            // BladeHorizontal
            new Vector2(0.496031746f,0.012195122f),
            new Vector2(0.574675325f,0.012605042f),
            new Vector2(0.609550562f,0.013043478f),
            new Vector2(0.560526316f,0.013761468f),
            new Vector2(0.485294118f,0.014285714f),
            new Vector2(0.392857143f,0.014018692f),
            new Vector2(0.335271318f,0.014018692f),
            new Vector2(0.356382979f,0.014285714f),
            new Vector2(0.363475177f,0.014018692f),
            new Vector2(0.377819549f,0.014018692f),
            new Vector2(0.382231405f,0.013761468f),
            new Vector2(0.414948454f,0.013513514f),
            new Vector2(0.484567901f,0.013274336f),
            new Vector2(0.496575342f,0.012820513f),
            new Vector2(0.568840580f,0.012605042f),
            new Vector2(0.600746269f,0.012195122f),
            // BladeDiagonal
            new Vector2(0.557692308f,0.012605042f),
            new Vector2(0.617469880f,0.013043478f),
            new Vector2(0.613157895f,0.013761468f),
            new Vector2(0.497126437f,0.011450382f),
            new Vector2(0.456185567f,0.010791367f),
            new Vector2(0.475806452f,0.011278195f),
            new Vector2(0.305172414f,0.014018692f),
            new Vector2(0.328231293f,0.014563107f),
            new Vector2(0.351398601f,0.014285714f),
            new Vector2(0.394444444f,0.014851485f),
            new Vector2(0.410000000f,0.015463918f),
            new Vector2(0.458737864f,0.014563107f),
            new Vector2(0.485955056f,0.014018692f),
            new Vector2(0.508426966f,0.013274336f),
            new Vector2(0.497058824f,0.012820513f),
            new Vector2(0.626623377f,0.012605042f),
        };
        static readonly Vector2[] Tips={
            // BladeVertical
            new Vector2(-0.638888889f,0.166666667f),
            new Vector2(-0.861111111f,0.388888889f),
            new Vector2(-0.772222222f,1.877777778f),
            new Vector2(-0.016666667f,2.611111111f),
            new Vector2(-0.127777778f,2.744444444f),
            new Vector2(1.116666667f,2.166666667f),
            new Vector2(1.383333333f,1.077777778f),
            new Vector2(1.338888889f,0.344444444f),
            new Vector2(1.094444444f,0.211111111f),
            new Vector2(0.916666667f,0.122222222f),
            new Vector2(0.961111111f,0.100000000f),
            new Vector2(0.916666667f,0.144444444f),
            new Vector2(0.894444444f,0.255555556f),
            new Vector2(0.783333333f,0.300000000f),
            new Vector2(0.672222222f,0.344444444f),
            new Vector2(-0.505555556f,0.122222222f),
            // BladeHorizontal
            new Vector2(-0.445833333f,0.125000000f),
            new Vector2(-0.645833333f,0.475000000f),
            new Vector2(-0.829166667f,0.591666667f),
            new Vector2(-0.812500000f,0.658333333f),
            new Vector2(-0.612500000f,0.758333333f),
            new Vector2(0.920833333f,0.741666667f),
            new Vector2(1.354166667f,0.891666667f),
            new Vector2(1.370833333f,1.141666667f),
            new Vector2(1.420833333f,1.225000000f),
            new Vector2(1.237500000f,0.991666667f),
            new Vector2(1.087500000f,0.775000000f),
            new Vector2(0.820833333f,0.575000000f),
            new Vector2(0.604166667f,0.391666667f),
            new Vector2(0.287500000f,0.391666667f),
            new Vector2(-0.512500000f,0.258333333f),
            new Vector2(-0.629166667f,0.125000000f),
            // BladeDiagonal
            new Vector2(-0.375000000f,0.163793103f),
            new Vector2(-0.806034483f,0.474137931f),
            new Vector2(-0.926724138f,0.543103448f),
            new Vector2(0.607758621f,2.146551724f),
            new Vector2(0.831896552f,2.301724138f),
            new Vector2(-0.133620690f,2.198275862f),
            new Vector2(1.659482759f,1.543103448f),
            new Vector2(1.556034483f,0.525862069f),
            new Vector2(1.521551724f,0.370689655f),
            new Vector2(1.297413793f,0.267241379f),
            new Vector2(1.228448276f,0.129310345f),
            new Vector2(0.831896552f,0.318965517f),
            new Vector2(0.711206897f,0.336206897f),
            new Vector2(0.435344828f,0.336206897f),
            new Vector2(-0.185344828f,0.198275862f),
            new Vector2(-0.616379310f,0.232758621f),
        };
        // 각 자세의 기계 손목 단자와 등허리/보조 손의 케이블 감개. 기준은 부츠 피벗, 단위는 월드이다.
        static readonly Vector2[] Hilts={
            new Vector2(-0.361f,0.900f), new Vector2(-0.506f,0.900f), new Vector2(-0.083f,1.522f), new Vector2(-0.028f,2.022f),
            new Vector2(-0.161f,2.122f), new Vector2(0.550f,1.678f), new Vector2(0.728f,1.067f), new Vector2(0.561f,0.689f),
            new Vector2(0.483f,0.689f), new Vector2(0.328f,0.467f), new Vector2(0.328f,0.456f), new Vector2(0.350f,0.578f),
            new Vector2(0.317f,0.656f), new Vector2(0.183f,0.700f), new Vector2(0.206f,0.722f), new Vector2(-0.206f,0.811f),
            new Vector2(-0.179f,0.950f), new Vector2(-0.371f,1.042f), new Vector2(-0.396f,1.092f), new Vector2(-0.213f,1.033f),
            new Vector2(0.154f,1.067f), new Vector2(0.454f,1.008f), new Vector2(0.629f,1.092f), new Vector2(0.646f,1.183f),
            new Vector2(0.663f,1.242f), new Vector2(0.546f,1.208f), new Vector2(0.454f,1.075f), new Vector2(0.371f,0.983f),
            new Vector2(0.229f,0.908f), new Vector2(-0.004f,0.917f), new Vector2(-0.279f,0.942f), new Vector2(-0.354f,0.942f),
            new Vector2(-0.228f,0.862f), new Vector2(-0.470f,1.052f), new Vector2(-0.487f,1.103f), new Vector2(-0.013f,1.871f),
            new Vector2(0.073f,2.112f), new Vector2(0.366f,1.966f), new Vector2(0.944f,1.276f), new Vector2(0.953f,0.940f),
            new Vector2(0.909f,0.810f), new Vector2(0.720f,0.698f), new Vector2(0.582f,0.647f), new Vector2(0.220f,0.836f),
            new Vector2(0.151f,0.914f), new Vector2(-0.047f,0.905f), new Vector2(-0.194f,0.914f), new Vector2(-0.409f,0.948f),
        };
        static readonly Vector2[] Reels={
            new Vector2(0.283f,0.944f), new Vector2(0.306f,1.044f), new Vector2(0.394f,1.156f), new Vector2(0.328f,1.000f),
            new Vector2(0.283f,1.078f), new Vector2(0.294f,1.011f), new Vector2(0.039f,0.900f), new Vector2(0.072f,0.900f),
            new Vector2(-0.028f,0.911f), new Vector2(-0.161f,0.822f), new Vector2(-0.194f,0.756f), new Vector2(-0.150f,0.878f),
            new Vector2(-0.139f,0.922f), new Vector2(0.383f,1.000f), new Vector2(0.272f,1.022f), new Vector2(0.328f,1.056f),
            new Vector2(0.388f,1.000f), new Vector2(0.421f,1.150f), new Vector2(0.387f,1.283f), new Vector2(0.287f,1.133f),
            new Vector2(0.437f,0.992f), new Vector2(0.137f,1.058f), new Vector2(0.129f,1.042f), new Vector2(-0.071f,0.958f),
            new Vector2(-0.054f,0.992f), new Vector2(-0.046f,1.042f), new Vector2(0.012f,1.033f), new Vector2(0.137f,1.000f),
            new Vector2(0.013f,1.092f), new Vector2(0.296f,1.092f), new Vector2(0.262f,1.108f), new Vector2(0.237f,1.042f),
            new Vector2(0.358f,0.931f), new Vector2(0.341f,1.164f), new Vector2(0.427f,1.069f), new Vector2(0.470f,1.103f),
            new Vector2(0.573f,1.138f), new Vector2(0.591f,1.060f), new Vector2(0.254f,1.052f), new Vector2(0.194f,0.974f),
            new Vector2(0.272f,1.000f), new Vector2(0.056f,0.983f), new Vector2(-0.013f,0.862f), new Vector2(0.384f,1.060f),
            new Vector2(0.358f,1.052f), new Vector2(0.323f,1.069f), new Vector2(0.228f,1.060f), new Vector2(0.116f,1.043f),
        };
        public static Vector2 HiltLocal(int step,int frame) => Hilts[Index(step,frame)];
        public static Vector2 ReelLocal(int step,int frame) => Reels[Index(step,frame)];
        public const int FieldSheetWidth = 1216;
        static readonly Rect[] FieldRects = PackField();

        static Rect[] PackField()
        {
            // 원본 투명 여백만 덜어 낸다. 각 이미지 크기·발 피벗·단위당 픽셀은 그대로이다.
            var packed = new Rect[Rectangles.Length];
            float x=2, y=2, rowHeight=0;
            for (int i=0; i<packed.Length; i++)
            {
                Rect frame=Rectangles[i];
                if (x+frame.width+2>FieldSheetWidth)
                { x=2; y+=rowHeight+2; rowHeight=0; }
                packed[i]=new Rect(x,y,frame.width,frame.height);
                x+=frame.width+2;
                rowHeight=Mathf.Max(rowHeight,frame.height);
            }
            return packed;
        }

        public static Rect FieldBounds(int step,int frame) => FieldRects[Index(step,frame)];
        static int Index(int step,int frame){return Mathf.Clamp(step-1,0,2)*FrameCount+Mathf.Clamp(frame,0,FrameCount-1);}
        public static Vector2 TipLocal(int step,int frame){return Tips[Index(step,frame)];}
        public static string Sheet(int step){return Sheets[Mathf.Clamp(step-1,0,2)];}
        public static Rect Bounds(int step,int frame){return Rectangles[Index(step,frame)];}
        public static Vector2 Pivot(int step,int frame){return Pivots[Index(step,frame)];}
        public static float PixelsPerUnit(int step){return Ppu[Mathf.Clamp(step-1,0,2)];}
    }
}
