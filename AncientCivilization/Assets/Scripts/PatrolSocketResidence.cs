using UnityEngine;

namespace Plugin.PlayerCore
{
    public sealed partial class PatrolRoom
    {
        // 모든 방의 0층 산책로는 양방향으로 이어진다. 정밀 경로는 산책로 위에만 설치한다.
        void BuildSocketResidence(int id)
        {
            switch (id)
            {
                case 31: SocketDoubleBalcony(); break;
                case 32: SocketGardenRail(); break;
                case 33: SocketGlassLouvers(); break;
                case 34: SocketFountainCourt(); break;
                case 35: SocketCleaningLifts(); break;
                case 36: SocketReadingRoom(); break;
                case 37: SocketQuietWing(); break;
                case 38: SocketFoldingTerrace(); break;
                case 39: SocketSlopedSkylight(); break;
                case 40: SocketGardenSluice(); break;
            }
        }

        // 한 번에 2타일보다 낮게 오르는 계단. 지상에서 양쪽으로 지날 수 있는 단방향 디딤면이다.
        void ResidenceApproach(float first, int direction, int count)
        {
            for (int i = 0; i < count; i++)
                Deck("석재 디딤단", first + direction * 2.8f * i, 1.45f * (i + 1), 3.0f);
        }

        GameObject ResidencePathLamp(string label, float x, float y)
        {
            Box("황동 등받이", x, y, .55f, .31f, ResStone, false, 3);
            return Box(label, x, y, .37f, .13f, ResBrass, false, 4);
        }

        void ResidenceFind(string label, float x, float y, string key, string line)
        {
            var lamp = ResidencePathLamp(label + " 표지", x, y + .6f).GetComponent<SpriteRenderer>();
            lamp.color = Flag(key) ? ResMoss : ResBrass;
            Use(label, x, y, () =>
            {
                if (!Flag(key))
                {
                    SetFlag(key);
                    AddCharge(1);
                    lamp.color = ResMoss;
                }
                Say(line);
            }, 1.24f);
        }

        // 얇은 판을 펴거나 문을 닫을 때 사람을 겹친 채로 새 콜라이더를 활성화하지 않는다.
        void ResidenceShowSurface(GameObject surface, bool visible, float x, float y, float width, float height)
        {
            if (surface.activeSelf == visible) return;
            if (visible && Mathf.Abs(Hero.x - x) < width * .5f + .38f &&
                Mathf.Abs(Hero.y - y) < height * .5f + .91f) return;
            surface.SetActive(visible);
        }

        void SocketDoubleBalcony()
        {
            ResidenceApproach(5, 1, 3);
            ResidenceWindow(22.2f, 8.2f, ResGlass);
            ResidenceWindow(20.7f, 3.35f, ResBrass);
            ResidencePlanter(23.4f, 6.85f, 2.2f);
            var ledge = WallOutlet("발코니 끝 고정점", 14.4f, 5.75f, 1, 1.65f);
            Deck("정문 발코니", 21.8f, 6.85f, 8.4f);
            // 바닥을 통과해서 창구로 바로 점프할 수 없도록 바닥판은 솔리드로 두고 보행 머리 여유를 남긴다.
            Box("처마 밑 정비문 창턱", 18.8f, 2.02f, 6.0f, .26f, ResStone, true, 3);
            Box("정비문 뒤 벽체", 21.75f, 4.3f, .40f, 4.3f, ResStone, true, 3);
            Deck("창턱 뒤 안뜰", 25.2f, 2.45f, 3.6f);
            Deck("정문 회랑", 30.4f, 6.0f, 5.1f);
            ResidenceApproach(39, -1, 3);
            ResidenceFind("정문 우편함", 22.2f, 7.8f, "socket.res.balconyFront",
                "정문 우편함  ·  난간 위에는 낮 순찰표가 꽂혀 있다.");
            ResidenceFind("발코니 밑 정비문", 20.7f, 3.12f, "socket.res.balconyService",
                "정비문  ·  처마 밑 수리공이 맡긴 예비 전구다.");
            Sign("난간에서 벽점프 · 아래 창턱은 줄을 걸고 돌아 들어간다", 7, 6);
            var sillLight = ResidencePathLamp("창턱 유도등", 16.8f, 2.60f).GetComponent<SpriteRenderer>();
            Step(dt => sillLight.color = Powered(ledge) || Resting(ledge) ? ResIvory : ResBrass);
        }

        void SocketGardenRail()
        {
            ResidenceApproach(5, 1, 2);
            Deck("정원 모노레일 승강장", 11, 3.3f, 4.3f);
            const float railY = 8.0f, cartTop = 4.4f;
            Box("온실 모노레일 궤도", 21.5f, railY, 22, .21f, ResStone, false, -2);
            var wagon = Deck("매달린 화분차", 13, cartTop, 3.4f, .46f);
            var hanger = Box("화분차 걸쇠", 13, 6.25f, .17f, 3.45f, ResBrass, false, -1);
            var pot = Box("흙이 담긴 화분", 13, cartTop + .36f, 1.6f, .66f, ResMoss, false, 2);
            var socket = WallOutlet("화분차 옆 소켓", 14.85f, cartTop + .74f, 1, 1.50f);
            // 가지 윗부분은 실제 차폐물이다. 매달린 높이에서 계속 진행하면 놓쳐서 아래 점검대로 내려간다.
            Box("온실의 낮은 가지", 22.8f, 6.87f, 1.4f, 4.25f, ResMoss, true, 3);
            Deck("나뭇가지 밑 점검대", 22.6f, 2.55f, 6.1f);
            WallOutlet("가지 건너 정원 소켓", 26.9f, 4.95f, -1);
            Deck("장미 회랑", 33.7f, 5.15f, 6.8f);
            ResidenceApproach(40, -1, 2);
            ResidencePlanter(33, 5.15f, 3.7f);
            ResidenceFind("온실 화단", 34, 6.1f, "socket.res.gardenRail", "화단 한쪽에 정원사의 예비 전구가 남아 있다.");
            Step(dt =>
            {
                float x = Mathf.Lerp(13, 30.3f, ResidenceTide(Elapsed, 11));
                Place(wagon, x, cartTop - .23f);
                Place(hanger, x, 6.25f);
                Place(pot, x, cartTop + .36f);
                Place(socket, x + 1.85f, cartTop + .74f);
            });
            Sign("가지 앞에서 아래 점검대로 · 가지 뒤 고정점으로 다시 타기", 7.4f, 4.95f);
        }

        void SocketGlassLouvers()
        {
            ResidenceApproach(5, 1, 2);
            WallOutlet("루버 서쪽 대기점", 12.9f, 4.35f, 1);
            const float period = 7.5f;
            for (int i = 0; i < 3; i++)
            {
                float x = 16.7f + i * 4.05f, top = 4.15f + i * 1.15f;
                float start = 1 + i * .65f;
                var shutter = Deck("온실 루버 " + (i + 1), x, top, 3.8f, .24f);
                shutter.SetActive(false);
                var blade = Box("루버 회전 유리", x, top - .16f, 3.5f, .16f, ResGlass, false, -1);
                var indicator = ResidencePathLamp("루버 순서등", x, top + 1.8f).GetComponent<SpriteRenderer>();
                Step(dt =>
                {
                    float t = Elapsed % period;
                    bool closed = t >= start && t < start + 3.3f;
                    float fold = Mathf.Clamp01((t - start + .38f) / .38f) *
                        Mathf.Clamp01((start + 3.68f - t) / .38f);
                    blade.transform.localRotation = Quaternion.Euler(0, 0, 72 * (1 - fold));
                    indicator.color = closed ? ResIvory : fold > 0 ? ResBrass : ResStone;
                    ResidenceShowSurface(shutter, closed, x, top - .12f, 3.8f, .24f);
                });
            }
            Deck("온실 채광창 회랑", 30.4f, 7.05f, 6.9f);
            ResidenceWindow(31.5f, 8.5f, ResGlass);
            ResidenceFind("온실 관리표", 31.5f, 8, "socket.res.louvers", "관리표  ·  서쪽 창부터 차례로 닫아 둔다.");
            ResidenceApproach(41, -1, 3);
            Sign("황동 등이 켜진 차양부터 디딘다", 7.6f, 4.9f);
        }

        void SocketFountainCourt()
        {
            ResidenceApproach(5, 1, 2);
            var valve = WallOutlet("중정 분수 밸브", 12.5f, 4.05f, 1);
            float clock = 0;
            Step(dt => clock = Powered(valve) ? clock + dt : 0);
            for (int i = 0; i < 2; i++)
            {
                bool high = i == 1;
                float x = high ? 23 : 16.5f, height = high ? 8.4f : 4.6f;
                float start = high ? 1.5f : 0;
                Box("분수 황동 노즐", x, .35f, 1.8f, .42f, ResBrass, false, 2);
                var water = Box("분수 물기둥", x, .55f, 1.05f, .2f, ResWater, false, 2);
                var plume = water.GetComponent<SpriteRenderer>();
                var gauge = ResidencePathLamp("분수 압력등", x, 1.25f).GetComponent<SpriteRenderer>();
                Step(dt =>
                {
                    float phase = clock % 4.9f - start;
                    bool flowing = Powered(valve) && phase >= 0 && phase < 2.6f;
                    float strength = flowing ? Mathf.Min(1, Mathf.Min(phase, 2.6f - phase) / .3f) : 0;
                    float h = .1f + strength * height;
                    plume.size = new Vector2(1.05f, h);
                    Place(water, x, .55f + h / 2);
                    gauge.color = flowing ? ResIvory : ResStone;
                    if (!Zipping && !RestingAnywhere && strength > .15f && Mathf.Abs(Hero.x - x) < .94f &&
                        Hero.y < h + .8f && Hero.y > .4f)
                    {
                        float lift = high ? 11.2f : 8.1f;
                        expedition.Player.ApplyEnvironmentalLift(lift);
                        Push(Velocity.x, Mathf.Max(Velocity.y, lift));
                    }
                });
            }
            Deck("분수 건너편 석교", 20, 3.7f, 4.3f);
            Deck("중정 이층 테라스", 27.7f, 7.75f, 5.7f);
            ResidencePlanter(28.5f, 7.75f, 3.3f);
            ResidenceFind("이층 화분", 28, 8.7f, "socket.res.fountain", "긴 물줄기를 건너 맡긴 손전등을 찾았다.");
            Sign("낮은 분수는 석교로 · 높은 분수는 이층으로", 9.5f, 6.25f);
        }

        void SocketCleaningLifts()
        {
            ResidenceApproach(5, 1, 2);
            WallOutlet("커튼월 환승 소켓", 22.2f, 5.55f, 1);
            for (int i = 0; i < 2; i++)
            {
                bool east = i == 1;
                float x = east ? 28 : 16.7f, low = east ? 4.25f : 1.65f, high = east ? 8.8f : 6.2f;
                float phase = east ? 2.75f : 0, period = east ? 7.9f : 7.25f;
                float initial = Mathf.Lerp(low, high, ResidenceTide(phase, period));
                var cradle = Deck(east ? "청소대 동쪽" : "청소대 서쪽", x, initial, 4.65f, .4f);
                var wire = Box("청소대 수직 케이블", x, 10, .08f, 5, ResIvory, false, -2);
                var strip = wire.GetComponent<SpriteRenderer>();
                Step(dt =>
                {
                    float top = Mathf.Lerp(low, high, ResidenceTide(Elapsed + phase, period));
                    Place(cradle, x, top - .2f);
                    strip.size = new Vector2(.08f, 15.5f - top);
                    Place(wire, x, (15.5f + top) / 2);
                });
            }
            Deck("청소대 환승턱", 20.7f, 4.4f, 2.3f);
            Deck("옥상 장비고", 33.9f, 8.0f, 5.4f);
            ResidenceWindow(34.8f, 9.2f, ResGlass);
            ResidenceFind("청소대 공구함", 34, 8.95f, "socket.res.facade", "청소부의 수리 기록과 새 전구가 들어 있다.");
            Sign("가운데 고정점에서 맞은편 청소대를 기다린다", 9, 4.8f);
        }

        void SocketReadingRoom()
        {
            ResidenceApproach(5, 1, 2);
            const float cx = 22.4f, cy = 5.35f, radius = 3.95f;
            var slot = WallOutlet("책장 옆면 황동 소켓", cx - radius, cy, -1, 1.55f);
            Deck("책장 탑승턱", 15.8f, 4.1f, 3.9f);
            var shelf = Box("회전 책장 본체", cx, cy, 7.3f, 3.55f, ResStone, false, 0);
            for (int i = 0; i < 9; i++)
            {
                var book = Box("정원 서고의 책등", cx - 2.6f + .65f * i, cy, .45f, 1.75f + .22f * (i % 3),
                    i % 2 == 0 ? ResBrass : ResMoss, false, 1);
                book.transform.SetParent(shelf.transform, true);
            }
            var screen = Box("독서실 뒤 가림문", 29.3f, 6.15f, .48f, 2.55f, ResIvory, true, 3);
            ResidenceWindow(32.1f, 6.75f, ResBrass);
            Deck("뒤쪽 독서실 바닥", 32.1f, 4.82f, 6.15f);
            float angle = 0, ridden = 0;
            bool open = false;
            Step(dt =>
            {
                float before = angle;
                angle = Mathf.MoveTowards(angle, Powered(slot) ? 180 : 0, 43 * dt);
                float a = Mathf.Deg2Rad * angle;
                // 회전 중 손이 붙어 있는 거리만 인정한다. 멀리서 전원만 유지하면 독서실 가림문은 남는다.
                if (!open)
                {
                    if (Resting(slot) && Powered(slot))
                        ridden += Mathf.Max(0, angle - before);
                    else if (angle < 175)
                        ridden = 0;
                    open = angle > 177 && ridden >= 168;
                }
                Place(slot, cx - Mathf.Cos(a) * radius, cy + Mathf.Sin(a) * 2.95f);
                shelf.transform.localScale = new Vector3(.18f + .82f * Mathf.Abs(Mathf.Cos(a)), 1, 1);
                ResidenceShowSurface(screen, !open, 29.3f, 6.15f, .48f, 2.55f);
            });
            var note = ResidencePathLamp("독서실 개방등", 32.1f, 6.1f).GetComponent<SpriteRenderer>();
            Use("뒤쪽 독서대", 32.1f, 5.85f, () =>
            {
                if (!open) { Say("책장 뒤 표지가 아직 보이지 않는다."); return; }
                if (!Flag("socket.res.reading")) { SetFlag("socket.res.reading"); AddCharge(1); }
                note.color = ResMoss;
                Say("독서대의 메모  ·  이 책장은 열람실로 이어지는 문이기도 하다.");
            }, 1.2f);
            Sign("소켓 자리에서 붙어 반 바퀴 돌아가기", 14.8f, 5.5f);
        }

        void SocketQuietWing()
        {
            ResidenceApproach(6, 1, 2);
            Deck("기숙사 차음 걸음판", 12, 3.35f, 3.6f);
            var silent = WallOutlet("차음재 벽 소켓", 17.8f, 4.05f, 1);
            Box("소음 감지 바닥선", 23, .11f, 14, .10f, ResBrass, false, 2);
            Deck("기숙사 조용한 난간", 23, 4.1f, 6.4f);
            var door = Box("차음 회랑 셔터", 27.6f, 5.5f, .46f, 3.5f, ResStone, true, 4);
            var lamp = ResidencePathLamp("차음 센서", 26.3f, 6.45f).GetComponent<SpriteRenderer>();
            // 바닥에 내리면 윗길이 닫힌다. 방 전체 바닥을 막지 않아 되돌아갈 수 있다.
            bool quiet = false;
            Step(dt =>
            {
                if (Resting(silent)) quiet = true;
                if (!RestingAnywhere && Grounded && Hero.y < 1.45f && Hero.x > 15 && Hero.x < 30) quiet = false;
                lamp.color = quiet ? ResMoss : ResBrass;
                ResidenceShowSurface(door, !quiet, 27.6f, 5.5f, .46f, 3.5f);
            });
            Deck("차음 셔터 너머 복도", 32, 4.05f, 8.0f);
            ResidenceWindow(32, 5.45f, ResGlass);
            Use("숙소 야간 창구", 32, 5.05f, () =>
            {
                if (!quiet) { Say("야간 창구  ·  노란 센서등이 꺼져야 문을 연다."); return; }
                if (!Flag("socket.res.quiet")) { SetFlag("socket.res.quiet"); AddCharge(1); }
                Say("야간 창구  ·  덕분에 아무도 깨지 않았네요.");
            });
            Sign("옆벽에 붙으면 센서 정지 · 바닥에 닿으면 회랑 폐쇄", 8, 5.1f);
        }

        void SocketFoldingTerrace()
        {
            ResidenceApproach(5.7f, 1, 3);
            var terminal = WallOutlet("테라스 경첩 소켓", 16.15f, 5.45f, -1);
            const float hingeX = 17.0f, top = 3.75f, length = 6.0f;
            var panel = Box("테라스 경첩 날개", hingeX + length / 2, top - .18f, length, .32f, ResIvory, false, 2);
            var deck = Deck("전원이 꺼진 테라스", hingeX + length / 2, top, length, .32f);
            var guard = Box("전원이 켜진 테라스 벽", hingeX, top + length / 2, .32f, length, ResStone, true, 3);
            guard.SetActive(false);
            float fold = 0;
            Step(dt =>
            {
                fold = Mathf.MoveTowards(fold, Powered(terminal) ? 1 : 0, dt * 1.2f);
                float a = fold * 90 * Mathf.Deg2Rad;
                Place(panel, hingeX + Mathf.Cos(a) * length / 2, top - .18f + Mathf.Sin(a) * length / 2);
                panel.transform.localRotation = Quaternion.Euler(0, 0, fold * 90);
                ResidenceShowSurface(deck, fold < .08f, hingeX + length / 2, top - .16f, length, .32f);
                ResidenceShowSurface(guard, fold > .93f, hingeX, top + length / 2, .32f, length);
            });
            Deck("옆집 테라스", 27.4f, 4.15f, 6.45f);
            ResidenceWindow(28.4f, 5.7f, ResGlass);
            ResidenceFind("테라스 창구", 28, 5.15f, "socket.res.fold", "창틀에 맡겨 둔 교체용 전구를 찾았다.");
            Sign("전원을 넣어 세우고 · 회수해 내려선다", 10, 6.25f);
        }

        void SocketSlopedSkylight()
        {
            ResidenceApproach(5, 1, 4);
            var skylight = WallOutlet("채광창 개폐기", 16.3f, 6.9f, -1);
            const float pivotX = 17.35f, pivotY = 6.55f, length = 7.2f;
            var glass = Deck("열리는 경사 채광창", pivotX + length / 2, pivotY, length, .32f);
            Deck("먼 채광 발코니", 31.5f, 4.85f, 6.65f);
            ResidenceWindow(32, 6.25f, ResGlass);
            ResidenceApproach(42.2f, -1, 3);
            ResidenceFind("채광 발코니 편지", 32, 5.8f, "socket.res.light", "채광 발코니  ·  가장 먼저 아침을 받는 집이다.");
            float fraction = 0;
            Step(dt =>
            {
                fraction = Mathf.MoveTowards(fraction, Powered(skylight) ? 1 : 0, dt * 1.45f);
                float a = -21 * fraction * Mathf.Deg2Rad;
                Place(glass, pivotX + .5f * length * Mathf.Cos(a), pivotY + .5f * length * Mathf.Sin(a) - .16f);
                glass.transform.localRotation = Quaternion.Euler(0, 0, -21 * fraction);
                float dx = Hero.x - pivotX;
                float surfaceY = pivotY + dx * Mathf.Sin(a) / Mathf.Cos(a);
                if (fraction > .7f && !RestingAnywhere && !Zipping && Axis > -.1f &&
                    dx > .05f && dx < length * Mathf.Cos(a) && ResidenceStanding(Hero.x, surfaceY, 1))
                    Push(Mathf.Min(13.4f, Mathf.Max(Velocity.x, 8.5f) + 26 * dt), Velocity.y);
            });
            Sign("열린 유리를 미끄러지다 끝에서 도약 · 동쪽은 내부 계단", 8, 6.2f);
        }

        void SocketGardenSluice()
        {
            ResidenceApproach(4.8f, 1, 3);
            WallOutlet("정원 수문 대기점", 14.4f, 5.55f, 1);
            var gate = Box("정원 수문 날개", 17.5f, 4.5f, .50f, 4.1f, ResStone, true, 3);
            var leaf = Deck("떠내려가는 넓은 잎", 16, 2.65f, 2.5f, .25f);
            var stem = Box("잎맥", 16, 2.72f, 2.1f, .07f, ResMoss, false, 4);
            var light = ResidencePathLamp("수문 예고등", 15.7f, 6.0f).GetComponent<SpriteRenderer>();
            Box("정원 물길", 24, 1.9f, 19, .32f, ResWater, false, -3);
            float phase = 0;
            Step(dt =>
            {
                // 잎이 방 끝으로 가면 승객이 내린 뒤에만 상류로 되돌린다.
                float next = phase + dt;
                phase = next >= 8.65f && ResidenceStanding(34, 2.68f, 2.5f) ? 8.65f : next % 8.65f;
                float sail = Mathf.Clamp01((phase - 1.6f) / 5.2f);
                bool open = phase >= 1.0f && phase <= 7.4f;
                float x = Mathf.Lerp(16, 34, sail), y = 2.68f + .16f * Mathf.Sin(sail * 4 * Mathf.PI);
                bool inStream = phase > 1.3f && (phase < 8.2f || ResidenceStanding(x, y, 2.5f));
                light.color = open ? ResIvory : ResBrass;
                ResidenceShowSurface(gate, !open, 17.5f, 4.5f, .50f, 4.1f);
                if (!inStream && leaf.activeSelf) leaf.SetActive(false);
                Place(leaf, x, y - .125f);
                Place(stem, x, y + .07f);
                if (inStream && !leaf.activeSelf) ResidenceShowSurface(leaf, true, x, y - .125f, 2.5f, .25f);
                stem.SetActive(leaf.activeSelf);
            });
            // 낮은 담장 너머 좁은 창턱이 잎 도착 구간에서 한 번의 일반 점프로 닿는다.
            Box("정원 담장", 33.6f, 4.42f, .42f, 1.8f, ResMoss, true);
            Deck("담장 뒤 정원길", 38.3f, 3.95f, 6.1f);
            ResidencePlanter(38.5f, 3.95f, 3.7f);
            ResidenceFind("담장 뒤 화분", 38.5f, 4.9f, "socket.res.leaf", "물길 끝에 버려진 전구를 주웠다.");
            Sign("등이 켜지면 수문이 열린다 · 떠내려오는 잎을 기다린다", 8.8f, 6.45f);
        }
    }
}
