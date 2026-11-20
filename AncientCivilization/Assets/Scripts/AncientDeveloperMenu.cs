using UnityEngine;

namespace Plugin.AncientCivilization
{
    [DefaultExecutionOrder(-900)]
    public sealed class AncientDeveloperMenu : MonoBehaviour
    {
        const int PanelWidth = 770;
        const int RowHeight = 42;
        static readonly Color Paper = new Color(.045f, .072f, .095f);
        static readonly Color Accent = new Color(.3f, .88f, 1f);
        static readonly Color Muted = new Color(.67f, .78f, .82f);
        static readonly string[] Tabs = { "전체", "층", "구역", "보스" };
        readonly AncientDeveloperAccess access = new AncientDeveloperAccess();
        AncientGame game;
        GUIStyle title, body, small, tab, row, field, code;
        string query = "", travelError = "";
        Vector2 scroll;
        int selectedTab, closedFrame = -1, openedFrame = -1, focusSearchAfter = -1;
        float previousTimeScale;

        public bool Open { get; private set; }
        // 닫기에 사용한 클릭이나 Esc가 같은 프레임에 아래쪽 화면을 조작하지 못하게 한다.
        public bool Capturing => Open || closedFrame == Time.frameCount;

        void Awake() { game = GetComponent<AncientGame>(); }

        void Update()
        {
            if (!Application.isFocused || !game) return;
            if (!Open)
            {
                if (!AncientBootIntro.Active && (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0)))
                {
                    previousTimeScale = Time.timeScale;
                    access.Reset();
                    query = travelError = "";
                    selectedTab = 0;
                    focusSearchAfter = -1;
                    scroll = Vector2.zero;
                    Open = true;
                    openedFrame = Time.frameCount;
                    game.BlockGameplayInput();
                    Time.timeScale = 0;
                }
                return;
            }
            if (Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        void Close()
        {
            Open = false;
            closedFrame = Time.frameCount;
            access.Reset();
            game.BlockGameplayInput();
            Time.timeScale = game.Paused ? 0 : previousTimeScale;
        }

        void Travel(AncientDeveloperDestination destination)
        {
            if (!access.Unlocked) return;
            if (AncientDeveloperTravel.Go(game, destination)) FinishTravel();
            else travelError = "이동할 수 없습니다. 목적지를 다시 선택하세요.";
        }

        void FinishTravel()
        {
            // 이동기가 새 세션의 시간 상태를 결정한다.
            Open = false;
            closedFrame = Time.frameCount;
            access.Reset();
            if (game) game.BlockGameplayInput();
        }

        void Styles()
        {
            if (title != null) return;
            var font = AncientKoreanUI.Font;
            body = new GUIStyle(GUI.skin.label) { font = font, fontSize = 19, alignment = TextAnchor.MiddleLeft };
            body.normal.textColor = Color.white;
            title = new GUIStyle(body) { fontSize = 31 };
            small = new GUIStyle(body) { fontSize = 16 };
            small.normal.textColor = Muted;
            row = new GUIStyle(GUI.skin.button) { font = font, fontSize = 17, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(12, 10, 2, 2) };
            tab = new GUIStyle(GUI.skin.button) { font = font, fontSize = 16 };
            field = new GUIStyle(GUI.skin.textField) { font = font, fontSize = 18, alignment = TextAnchor.MiddleLeft };
            code = new GUIStyle(body) { fontSize = 33, alignment = TextAnchor.MiddleCenter };
        }

        void OnGUI()
        {
            if (!Open) return;
            Styles();
            var oldMatrix = GUI.matrix;
            var oldColor = GUI.color;
            int oldDepth = GUI.depth;
            try
            {
                float scale = Mathf.Max(.01f, Mathf.Min(Screen.width / 820f, Screen.height / 630f));
                float w = Screen.width / scale, h = Screen.height / scale;
                GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
                GUI.depth = -9000;
                GUI.color = Color.white;
                AncientHUD.Panel(new Rect(0, 0, w, h), new Color(.01f, .02f, .03f, .94f));
                int height = access.Unlocked ? 590 : 244;
                float x = (w - PanelWidth) * .5f, y = (h - height) * .5f;
                AncientHUD.Panel(new Rect(x, y, PanelWidth, height), Paper);
                AncientHUD.Panel(new Rect(x, y, 5, height), Accent);
                GUI.Label(new Rect(x + 26, y + 18, 500, 45), access.Unlocked ? "개발자 이동" : "개발자 인증", title);
                if (GUI.Button(new Rect(x + 651, y + 24, 94, 35), "닫기 · Esc", tab)) Close();
                if (!Open) return;
                if (!access.Unlocked) Password(x, y);
                else Destinations(x, y);
            }
            finally
            {
                GUI.matrix = oldMatrix;
                GUI.color = oldColor;
                GUI.depth = oldDepth;
                var e = Event.current;
                if (e.isMouse || e.isKey || e.type == EventType.ScrollWheel) e.Use();
            }
        }

        void Password(float x, float y)
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                int key = (int)e.keyCode;
                int digit = key - (int)KeyCode.Alpha0;
                if (digit < 0 || digit > 9) digit = key - (int)KeyCode.Keypad0;
                if (digit >= 0 && digit <= 9)
                {
                    if (digit != 0 || openedFrame != Time.frameCount) access.EnterDigit(digit);
                }
                else if (e.keyCode == KeyCode.Backspace) access.Backspace();
                else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) access.Submit();
                e.Use();
            }
            string prompt = access.Rejected ? "비밀번호가 일치하지 않습니다. 다시 입력하세요." : "숫자 비밀번호 다섯 자리를 입력하세요.";
            GUI.Label(new Rect(x + 26, y + 72, 690, 30), prompt, small);
            var inputBox = new Rect(x + 26, y + 115, 542, 70);
            AncientHUD.Panel(inputBox, new Color(.02f, .04f, .055f));
            GUI.Label(inputBox, new string('●', access.Count) + new string('·', access.Length - access.Count), code);
            if (GUI.Button(new Rect(x + 584, y + 115, 159, 70), "확인", tab)) access.Submit();
            if (access.Unlocked) focusSearchAfter = Time.frameCount;
        }

        static int TabFor(AncientDeveloperGroup group)
        {
            if (group == AncientDeveloperGroup.Floor) return 1;
            if (group == AncientDeveloperGroup.Area) return 2;
            return 3;
        }

        void Destinations(float x, float y)
        {
            GUI.Label(new Rect(x + 26, y + 70, 64, 33), "검색", body);
            GUI.SetNextControlName("DeveloperDestinationSearch");
            string next = GUI.TextField(new Rect(x + 93, y + 72, 650, 33), query, 48, field);
            if (focusSearchAfter >= 0 && Time.frameCount > focusSearchAfter && Event.current.type == EventType.Repaint)
            {
                GUI.FocusControl("DeveloperDestinationSearch");
                focusSearchAfter = -1;
            }
            if (next != query) { query = next; scroll = Vector2.zero; }
            for (int i = 0; i < Tabs.Length; i++)
            {
                var r = new Rect(x + 26 + i * 180, y + 117, 174, 34);
                if (GUI.Button(r, Tabs[i], tab)) { selectedTab = i; scroll = Vector2.zero; }
                if (selectedTab == i) AncientHUD.Panel(new Rect(r.x, r.yMax, r.width, 2), Accent);
            }

            var entries = AncientDeveloperTravel.Destinations;
            int count = 0;
            for (int i = 0; i < entries.Count; i++) if (Visible(entries[i])) count++;
            var viewport = new Rect(x + 26, y + 174, 717, 346);
            scroll = GUI.BeginScrollView(viewport, scroll, new Rect(0, 0, 693, Mathf.Max(viewport.height, count * RowHeight)));
            AncientDeveloperDestination chosen = default;
            bool hasChoice = false;
            int shown = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (!Visible(entry)) continue;
                string floor = AncientCityLayout.Name(entry.Floor);
                string route = entry.Group == AncientDeveloperGroup.Floor ? entry.Name : floor + "   /   " + entry.Name;
                string label = selectedTab == 0 ? Tabs[TabFor(entry.Group)] + "     " + route : route;
                if (GUI.Button(new Rect(0, shown * RowHeight, 689, RowHeight - 4), label, row))
                { chosen = entry; hasChoice = true; }
                shown++;
            }
            if (count == 0) GUI.Label(new Rect(12, 8, 660, 32), "검색 결과가 없습니다.", small);
            GUI.EndScrollView();
            if (hasChoice) { Travel(chosen); return; }
            GUI.Label(new Rect(x + 26, y + 539, 510, 34), travelError.Length > 0 ? travelError : count + "개 목적지", small);
            if (AncientDeveloperTravel.IsVisiting &&
                GUI.Button(new Rect(x + 547, y + 535, 195, 37), "원래 플레이로 복귀", tab))
            {
                if (AncientDeveloperTravel.Return(game)) FinishTravel();
                else travelError = "원래 플레이로 복귀할 수 없습니다.";
            }
        }

        bool Visible(AncientDeveloperDestination entry)
        {
            int group = TabFor(entry.Group);
            return (selectedTab == 0 || group == selectedTab) &&
                AncientDeveloperAccess.Matches(query, entry.Name, AncientCityLayout.Name(entry.Floor), Tabs[group]);
        }
    }
}
