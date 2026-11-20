using UnityEngine;

namespace Plugin.AncientCivilization
{
    // 충돌과 예고를 같은 월드 좌표에 그린다. 거울의 마지막 반사는 발사 전 고정된다.
    public sealed class AncientJokerAttacks
    {
        enum Attack { None, Mirror, Club, Ring, Laser }
        const float MirrorLock = 1.50f, MirrorFire = 2f, MirrorEnd = 2.16f;
        const float SlashA = .52f, SlashB = 1.48f, SlashTime = .16f;
        const float RingTell = .48f, RingTime = .84f;
        const float LaserTell = 1.22f, LaserTime = 1.58f;
        const float BodyX = .36f, BodyY = 1.05f, RayWidth = .095f, LaserWidth = .82f;
        const int ArcSteps = 24;
        static readonly Color Gold = new Color(1f,.77f,.25f);
        static readonly Color Ice = new Color(.36f,.95f,1f);
        static readonly Color Ruby = new Color(1f,.19f,.39f);
        static readonly Color Blade = new Color(.80f,.59f,1f);

        readonly AncientGame game;
        readonly GameObject root;
        readonly float left, right;
        readonly LineRenderer body, glow, core;
        readonly LineRenderer[] marks = new LineRenderer[3];
        readonly Vector2[] ray = new Vector2[4];
        readonly Vector2[] candidate = new Vector2[4];
        AncientJokerArena arena;
        AncientAudio audio;
        Attack attack;
        Vector2 origin, previousHero;
        float time, duration;
        int firstMirror, bounceCount, lit, facing, safeLane, hits, revision;
        bool descending, disposed, haveHero, locked, fired, secondSlash;

        public int ClubFrame
        {
            get
            {
                if (attack != Attack.Club || time < SlashA) return 0;
                if (time < SlashA + SlashTime) return 1;
                return time < SlashB ? 2 : 3;
            }
        }
        public bool Busy => attack != Attack.None;
        public bool StartedFiring => Busy && fired;
        public bool Telegraphing => Busy && (!fired || (attack == Attack.Club && time >= SlashA+SlashTime && time < SlashB));
        public float Duration => duration;
        public float Progress => duration > 0 ? Mathf.Clamp01(time/duration) : 0;
        public float WindupProgress => attack == Attack.Club && fired
            ? Mathf.InverseLerp(SlashA+SlashTime, SlashB, time) : Mathf.Clamp01(time / Tell);
        public string Hint => attack == Attack.Mirror ? (bounceCount == 2 ? "거울 두 장 · 고정된 사선에서 이탈" : "거울 점등 · 반사선 회피") :
            attack == Attack.Club ? "장막 결투 · 두 번째 베기 뒤 반격" :
            attack == Attack.Ring ? "거울 교대 · 고리를 뛰어넘기" :
            attack == Attack.Laser ? (descending ? "직선 레이저 ▼ · 지나간 높이로" : "직선 레이저 ▲ · 하단 통로") : "";
        float Tell => attack == Attack.Mirror ? MirrorFire : attack == Attack.Club ? SlashA :
            attack == Attack.Ring ? RingTell : LaserTell;

        public AncientJokerAttacks(AncientGame game, Transform parent, float left, float right)
        {
            this.game = game; this.left = left; this.right = right;
            root = new GameObject("조커 · 탄도와 전조");
            root.transform.SetParent(parent, false);
            glow = MakeLine("잔광", 17); body = MakeLine("판정", 19); core = MakeLine("필라멘트", 20);
            for (int i=0; i<marks.Length; i++) marks[i] = MakeLine("광학 표식 " + i, 21);
            audio = game.gameObject.GetComponent<AncientAudio>();
        }

        LineRenderer MakeLine(string name, int order)
        {
            var line = game.Line(name, Color.white, .05f, order);
            line.transform.SetParent(root.transform, false);
            line.useWorldSpace = true;
            line.enabled = false;
            return line;
        }

        public void SetArena(AncientJokerArena value) { arena = value; }
        public void Mirror(Vector2 muzzle, Vector2 target, int bounces, int safeLane = -1)
        {
            if (!Begin(Attack.Mirror, MirrorEnd)) return;
            origin = muzzle;
            this.safeLane = safeLane;
            bounceCount = bounces >= 2 ? 2 : 1;
            firstMirror = First(muzzle);
            BuildRay(ray, muzzle, target, firstMirror, bounceCount);
            if (safeLane >= 0 && !LaneClear(ray, bounceCount, safeLane))
            {
                // 움직이는 표적이 피난처에 들어오면 마지막으로 안전했던 조준을 유지한다.
                Vector2 alternative = new Vector2(muzzle.x < (left+right)*.5f ? right : left, 6.85f);
                BuildRay(candidate, muzzle, alternative, firstMirror, bounceCount);
                if (LaneClear(candidate, bounceCount, safeLane)) CopyRay();
            }
            LightMirrors(1);
            RenderMirror();
        }

        public int MirrorSafeLanes(Vector2 muzzle, Vector2 target, int bounces = 2)
        {
            bounces = bounces >= 2 ? 2 : 1;
            BuildRay(candidate, muzzle, target, First(muzzle), bounces);
            int mask = 0;
            for (int i=0; i<3; i++) if (LaneClear(candidate, bounces, i)) mask |= 1 << i;
            return mask;
        }

        int First(Vector2 muzzle) => muzzle.x < (left+right)*.5f ? 1 : 0;
        Vector2 MirrorPoint(int index) => arena != null ? arena.MirrorPoint(index) :
            new Vector2(index == 0 ? left+1.35f : right-1.35f, 5.55f);
        float EscapeHeight => arena != null ? arena.EscapeHeight : 1.08f;

        void BuildRay(Vector2[] points, Vector2 muzzle, Vector2 target, int first, int count)
        {
            points[0] = muzzle;
            points[1] = MirrorPoint(first);
            if (count == 2) points[2] = MirrorPoint(1-first);
            Vector2 from = points[count];
            Vector2 direction = (target-from).normalized;
            if (direction.sqrMagnitude < .01f) direction = Vector2.down;
            float length = right-left+12;
            if (Mathf.Abs(direction.x) > .0001f)
                length = Mathf.Min(length, ((direction.x > 0 ? right : left)-from.x)/direction.x);
            if (Mathf.Abs(direction.y) > .0001f)
                length = Mathf.Min(length, ((direction.y > 0 ? 9f : 0f)-from.y)/direction.y);
            points[count+1] = from+direction*Mathf.Max(0,length);
        }

        bool LaneClear(Vector2[] points, int count, int lane)
        {
            Vector2 standing = new Vector2(Mathf.Lerp(left,right,(lane+.5f)/3f), EscapeHeight);
            for (int i=0; i<=count; i++)
                if (Touches(points[i],points[i+1],standing,RayWidth,.65f,BodyY+.06f)) return false;
            return true;
        }

        void CopyRay() { for (int i=0; i<bounceCount+2; i++) ray[i] = candidate[i]; }
        public void Aim(Vector2 target) { Aim(origin, target); }
        public void Aim(Vector2 muzzle, Vector2 target)
        {
            if (attack != Attack.Mirror || locked) return;
            BuildRay(candidate, muzzle, target, firstMirror, bounceCount);
            if (safeLane >= 0 && !LaneClear(candidate,bounceCount,safeLane)) return;
            origin = muzzle;
            CopyRay();
        }

        public void Club(Vector2 bladeOrigin, int facing)
        {
            if (!Begin(Attack.Club,SlashB+SlashTime+.16f)) return;
            this.facing = facing < 0 ? -1 : 1;
            origin = bladeOrigin;
            game.Sound(AncientEquipmentCue.Forge);
            RenderClub();
        }

        public void Ring(Vector2 vanishedClone)
        {
            if (!Begin(Attack.Ring,RingTell+RingTime)) return;
            origin = vanishedClone;
            game.Sound(AncientEquipmentCue.ShortCircuit);
            RenderRing();
        }

        public void Laser(bool descending, int safeLane = -1)
        {
            if (!Begin(Attack.Laser,LaserTell+LaserTime)) return;
            this.descending = descending;
            this.safeLane = safeLane;
            game.Sound(AncientEquipmentCue.JokerCharge);
            RenderLaser();
        }

        bool Begin(Attack next, float length)
        {
            if (disposed) return false;
            Clear();
            attack = next; duration = length;
            if (game.hero) { previousHero = game.hero.transform.position; haveHero = true; }
            return true;
        }

        public void Tick(float dt, Vector2 hero)
        {
            if (!Busy || dt <= 0 || disposed) return;
            float before = time;
            float next = Mathf.Min(duration, time+dt);
            if (!haveHero) { previousHero = hero; haveHero = true; }

            // 긴 프레임에서도 두 거울의 점등과 고정된 사선이 순서대로 화면에 보인다.
            if (attack == Attack.Mirror && !locked)
            {
                Aim(hero);
                if (bounceCount == 2 && lit < 2 && next >= .72f) { next = .72f; LightMirrors(2); }
                else if (next >= MirrorLock) { next = MirrorLock; locked = true; game.Sound(AncientEquipmentCue.Counter); }
            }
            time = next;
            if (!fired && time >= Tell)
            {
                fired = true;
                game.Sound(attack == Attack.Mirror ? AncientEquipmentCue.Contact : AncientEquipmentCue.Sweep);
                if (attack == Attack.Mirror) game.Burst(ray[bounceCount+1], Gold, 5);
            }
            if (attack == Attack.Club && !secondSlash && time >= SlashB)
            {
                secondSlash = true;
                game.Sound(AncientEquipmentCue.Sweep);
            }
            int stamp = revision;
            if (fired && game.hero)
            {
                // 빠른 베기와 고리가 프레임 사이에서 플레이어를 통과하는 경우도 판정한다.
                int slices = Mathf.Max(1, Mathf.CeilToInt((next-before)*100));
                for (int i=0; i<=slices; i++)
                {
                    float p = (float)i/slices;
                    Contact(Mathf.Lerp(before,next,p),Vector2.Lerp(previousHero,hero,p));
                    if (revision != stamp) return;
                }
            }
            previousHero = hero;
            if (time >= duration) { Clear(); return; }
            if (attack == Attack.Mirror) RenderMirror();
            else if (attack == Attack.Club) RenderClub();
            else if (attack == Attack.Ring) RenderRing();
            else RenderLaser();
        }

        static bool Touches(Vector2 a, Vector2 b, Vector2 target, float width, float hx = BodyX, float hy = BodyY) =>
            AncientBeamRules.IntersectsBox(b.x-a.x,b.y-a.y,target.x-a.x,target.y-a.y,width*.5f,hx,hy);

        void Contact(float t, Vector2 hero)
        {
            if (t < Tell) return;
            int slot = 1;
            bool hit = false;
            float damage = 18;
            if (attack == Attack.Club)
            {
                bool second = t >= SlashB;
                float p = (t-(second ? SlashB : SlashA))/SlashTime;
                if (p < 0 || p > 1) return;
                slot = second ? 2 : 1;
                hit = Touches(origin,SlashTip(second,p),hero,.22f);
                damage = second ? 17 : 12;
            }
            else if ((hits & slot) != 0) return;
            else if (attack == Attack.Mirror)
            {
                for (int i=0; i<=bounceCount && !hit; i++) hit = Touches(ray[i],ray[i+1],hero,RayWidth);
            }
            else if (attack == Attack.Laser)
            {
                float y = SweepY(t);
                hit = Touches(new Vector2(LaserLeft,y),new Vector2(LaserRight,y),hero,LaserWidth);
                damage = 24;
            }
            else if (attack == Attack.Ring)
            {
                // 낮은 타원 고리의 테두리만 닿는다. 중앙이 지나간 뒤에는 착지할 수 있다.
                float radius = RingRadius(t);
                Vector2 last = RingPoint(radius,0);
                for (int i=1; i<=ArcSteps && !hit; i++)
                {
                    Vector2 point = RingPoint(radius,i);
                    hit = Touches(last,point,hero,.12f);
                    last = point;
                }
                damage = 13;
            }
            if (hit && (hits & slot) == 0)
            {
                hits |= slot;
                game.hero.Hurt(damage);
            }
        }

        void LightMirrors(int count)
        {
            lit = count;
            arena?.SetMirrors(firstMirror,count);
            game.Sound(AncientEquipmentCue.Lens);
        }

        void RenderMirror()
        {
            // 고정 전에는 밝혀진 거울까지만 선을 그리고 최종 반사 방향은 얇게 추적한다.
            int points = bounceCount+2;
            int shown = !locked && lit < bounceCount ? lit+1 : points;
            Path(body,ray,shown, fired ? RayWidth : locked ? .055f : .022f,
                fired ? Color.white : locked ? Gold : new Color(.39f,.79f,.87f,.72f));
            if (locked) Path(glow,ray,points,fired ? .31f : .14f,
                fired ? new Color(1,.63f,.17f,.59f) : new Color(.91f,.56f,.16f,.28f));
            for (int i=1; i<=lit; i++)
            {
                Vector2 a = (ray[i]-ray[i-1]).normalized;
                Vector2 b = (ray[i+1]-ray[i]).normalized;
                Vector2 tangent = (a+b).normalized;
                if (tangent.sqrMagnitude < .01f) tangent = new Vector2(-a.y,a.x);
                Segment(marks[i-1],ray[i]-tangent*.50f,ray[i]+tangent*.50f,locked ? .12f : .075f,
                    fired ? Color.white : i == 1 ? Ice : Gold);
            }
        }

        Vector2 SlashTip(bool second, float p)
        {
            float degrees = second ? Mathf.Lerp(-58,83,p) : Mathf.Lerp(70,-48,p);
            float angle = degrees*Mathf.Deg2Rad;
            return origin+new Vector2(facing*Mathf.Cos(angle),Mathf.Sin(angle))*(second ? 2.90f : 2.35f);
        }

        void RenderClub()
        {
            if (time > SlashB+SlashTime)
            {
                body.enabled = glow.enabled = false;
                return;
            }
            bool second = time >= SlashA+SlashTime;
            float start = second ? SlashB : SlashA;
            float p = Mathf.Clamp01((time-start)/SlashTime);
            bool live = time >= start && time <= start+SlashTime;
            // 앞쪽의 칼끝이 자세를 유지했다가 반대 대각선으로 두 번째 호를 그린다.
            Segment(body,origin,SlashTip(second,p),live ? .22f : .08f,live ? Color.white : Blade);
            Stroke(glow, ArcSteps+1, live ? .21f : .045f,live ? Blade : new Color(.66f,.38f,.95f,.42f));
            for (int i=0; i<=ArcSteps; i++)
                glow.SetPosition(i,SlashTip(second,live ? Mathf.Lerp(Mathf.Max(0,p-.25f),p,(float)i/ArcSteps) : (float)i/ArcSteps));
        }

        float RingRadius(float t) => Mathf.Lerp(.20f,4.85f,Mathf.InverseLerp(RingTell,RingTell+RingTime,t));
        Vector2 RingPoint(float radius, int i)
        {
            float angle = i*(Mathf.PI*2/ArcSteps);
            return origin+new Vector2(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius*.105f);
        }

        void RenderRing()
        {
            float radius = fired ? RingRadius(time) : .44f+.15f*Mathf.Sin(time*18);
            Stroke(body,ArcSteps+1,fired ? .12f : .07f,fired ? Ice : Blade);
            Stroke(glow,ArcSteps+1,fired ? .30f : .15f,new Color(.25f,.69f,1f,.32f));
            for (int i=0; i<=ArcSteps; i++)
            {
                Vector2 point = RingPoint(radius,i);
                body.SetPosition(i,point); glow.SetPosition(i,point);
            }
        }

        // 낮은 통로로 피하거나, 광선이 이미 지나간 높이로 건널 수 있다.
        float LaserLeft => safeLane == 0 ? Mathf.Lerp(left,right,.22f) : left;
        float LaserRight => safeLane == 2 ? Mathf.Lerp(left,right,.78f) : right;
        float SweepY(float t)
        {
            float p = Mathf.SmoothStep(0,1,Mathf.InverseLerp(LaserTell,LaserTell+LaserTime,t));
            float low = EscapeHeight+BodyY+LaserWidth*.5f+.18f;
            return Mathf.Lerp(low,6.55f,descending ? 1-p : p);
        }

        void RenderLaser()
        {
            float y = SweepY(time);
            Vector2 a = new Vector2(LaserLeft,y), b = new Vector2(LaserRight,y);
            Segment(body,a,b,fired ? LaserWidth : .23f,fired ? Ruby : new Color(.98f,.23f,.35f,.85f));
            Segment(glow,a,b,fired ? 1.38f : .47f,new Color(.98f,.12f,.24f,fired ? .34f : .18f));
            Segment(core,a,b,fired ? .24f : .045f,fired ? Color.white : new Color(1f,.79f,.62f));
            float dir = descending ? -1 : 1;
            for (int i=0; i<marks.Length; i++)
            {
                float x = Mathf.Lerp(a.x,b.x,(i+.5f)/marks.Length);
                float h = y+dir*(.62f+.11f*Mathf.Sin(time*9));
                var arrow = marks[i];
                Stroke(arrow,3,.085f,fired ? Color.white : Ruby);
                arrow.SetPosition(0,new Vector2(x-.34f,h-dir*.28f));
                arrow.SetPosition(1,new Vector2(x,h));
                arrow.SetPosition(2,new Vector2(x+.34f,h-dir*.28f));
            }
        }

        static void Stroke(LineRenderer line, int count, float width, Color color)
        {
            line.enabled = true;
            if (line.positionCount != count) line.positionCount = count;
            line.startWidth = line.endWidth = width; line.startColor = line.endColor = color;
        }
        static void Segment(LineRenderer line, Vector2 a, Vector2 b, float width, Color color)
        {
            Stroke(line,2,width,color);
            line.SetPosition(0,a); line.SetPosition(1,b);
        }
        static void Path(LineRenderer line, Vector2[] points, int count, float width, Color color)
        {
            Stroke(line,count,width,color);
            for (int i=0; i<count; i++) line.SetPosition(i,points[i]);
        }

        public void Clear()
        {
            if (disposed) return;
            if (attack == Attack.Laser && audio) audio.StopJokerCharge();
            if (attack == Attack.Mirror && root) arena?.SetMirrors(firstMirror,0);
            revision++;
            attack = Attack.None; time = duration = 0; safeLane = -1; hits = lit = 0;
            locked = fired = secondSlash = haveHero = false;
            if (glow) glow.enabled = false;
            if (body) body.enabled = false;
            if (core) core.enabled = false;
            foreach (var line in marks) if (line) line.enabled = false;
        }

        public void Dispose()
        {
            if (disposed) return;
            Clear();
            disposed = true;
            if (root)
            {
                root.SetActive(false);
                Object.Destroy(root);
            }
        }
    }
}
