using Plugin.AncientCivilization;
using UnityEngine;

namespace Plugin.PlayerCore
{
    [DefaultExecutionOrder(150)]
    public sealed class PatrolExpedition : MonoBehaviour
    {
        enum Panel { None, Pause, Password, Map, CampaignMap, Result, Observatory }
        static readonly string[] CampaignTabs = { "전체", "층", "구역", "보스" };
        const int CampaignPageSize = 6;
        const float Width = 48;
        const int RoomCount = PatrolCatalog.Count;
        static readonly Color Ink = new Color(.035f, .08f, .105f, .97f);
        static readonly Color Cyan = new Color(.32f, .91f, 1f);
        public PlayerStateMachine Player { get; private set; }
        public PlayerGrapple Grapple { get; private set; }
        public PlayerInputReader InputReader { get; private set; }
        public bool TrialActive { get; private set; }
        public int RoomId => roomId;
        public bool Paused => leaving || panel != Panel.None || !Application.isFocused || (InputReader && InputReader.Paused);
        public int Charge { get; private set; }
        int roomId, mapFloor, completedTime, previousBest, pinFrame, arrivalFrame, justFinished;
        int campaignPage, campaignTab;
        string campaignSearch = "", campaignError = "";
        float noticeEnd, fixedStepBefore;
        string notice = "", roomKey, saveRoot;
        bool recordForward, dirty, passed, badge, leaving;
        Panel panel, returnPanel;
        Vector2 entrance, checkpoint;
        Camera view;
        PatrolRoom room;
        SpriteRenderer ghost;
        DistrictPlayerPresentation presentation;
        PracticeMusic music;
        GUIStyle title, label, small, button, roomButton, searchField;
        readonly AncientDeveloperAccess access = new AncientDeveloperAccess();
        readonly PatrolHistory recording = new PatrolHistory(), previous = new PatrolHistory();

        void Awake()
        {
            fixedStepBefore = Time.fixedDeltaTime;
            Time.timeScale = 1;
            Time.fixedDeltaTime = 1f / 60;
            saveRoot = "patrol." + PatrolEntry.Read("patrol.generation") + ".";
            Charge = Mathf.Clamp(PatrolEntry.Read(saveRoot + "charge"), 0, 99);
            view = Camera.main;
            if (!view) view = new GameObject("순찰 카메라", typeof(Camera)).GetComponent<Camera>();
            view.tag = "MainCamera";
            view.orthographic = true;
            view.orthographicSize = 6.35f;
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = new Color(.025f, .052f, .078f);
            music = new PracticeMusic(transform, view);
            var officer = new GameObject("백 경위");
            officer.SetActive(false);
            officer.layer = 9;
            officer.AddComponent<Rigidbody2D>();
            officer.AddComponent<CapsuleCollider2D>().size = new Vector2(.72f, 1.8f);
            officer.AddComponent<PlayerModeController>();
            InputReader = officer.AddComponent<PlayerInputReader>();
            InputReader.data = Resources.Load<PlayerMovementData>("PlayerCore/DefaultMovement");
            Player = officer.AddComponent<PlayerStateMachine>();
            Player.data = InputReader.data;
            Grapple = officer.AddComponent<PlayerGrapple>();
            Grapple.data = Resources.Load<GrappleData>("Traversal/DefaultGrapple");
            presentation = officer.AddComponent<DistrictPlayerPresentation>();
            officer.SetActive(true);
            ghost = AncientArt.Display("직전 순찰의 잔상", AncientArt.Hero(0), Vector2.zero, 11, transform);
            ghost.transform.localScale = Vector3.one * (1.95f / Mathf.Max(.1f, ghost.sprite.bounds.size.y));
            ghost.color = new Color(.35f, .89f, 1f, .42f);
            ghost.enabled = false;
            Travel(PatrolEntry.ConsumeRoom());
            Notice("A/D 이동  ·  Space 점프  ·  좌클릭 와이어  ·  E 조사  ·  Esc 일시정지  ·  P 본편 복귀");
        }

        public static int LastRoom()
        {
            string prefix = "patrol." + PatrolEntry.Read("patrol.generation") + ".";
            return Mathf.Clamp(PatrolEntry.Read(prefix + "lastRoom", 1), 1, RoomCount);
        }

        public static void ResetSavedProgress()
        {
            AncientGame.SetSaved("patrol.generation", AncientGame.Saved("patrol.generation") + 1);
        }

        public bool Flag(string key) => !string.IsNullOrEmpty(key) && PatrolEntry.Read(saveRoot + "flag." + key) != 0;
        public void SetFlag(string key, bool value = true)
        {
            if (string.IsNullOrEmpty(key) || Flag(key) == value) return;
            Write("flag." + key, value ? 1 : 0);
        }
        public void AddCharge(int n)
        {
            if (n <= 0) return;
            Charge = Mathf.Min(99, Charge + Mathf.Min(99, n));
            Write("charge", Charge);
        }
        public bool SpendCharge(int n)
        {
            if (n < 0 || Charge < n) return false;
            Charge -= n;
            Write("charge", Charge);
            return true;
        }
        public void Notice(string text) { notice = text; noticeEnd = Time.unscaledTime + 3.4f; }
        void Write(string key, int value) { PatrolEntry.Write(saveRoot + key, value); dirty = true; }
        void Flush()
        {
            if (!dirty) return;
            if (!PatrolEntry.Preview && !AncientDeveloperTravel.IsVisiting) PlayerPrefs.Save();
            dirty = false;
        }

        public void Travel(int id, bool fromRight = false)
        {
            LoadRoom(Mathf.Clamp(id, 1, RoomCount), fromRight, false, Vector2.zero);
        }

        public void TravelDoor(int destination)
        {
            int source = roomId;
            Travel(destination);
            if (!room.TryArrivalFrom(source, out Vector2 otherSide)) return;
            // 옆길을 타고 도착한 지점은 정규 시험 주행의 시작선이 아니다.
            entrance = checkpoint = otherSide;
            recordForward = false;
            PositionPlayer(otherSide);
        }

        void LoadRoom(int id, bool fromRight, bool trial, Vector2 velocity)
        {
            int oldFloor = roomId == 0 ? -1 : (roomId - 1) / 10;
            bool changedMusic = oldFloor != (id - 1) / 10 || TrialActive != trial;
            Flush();
            Show(Panel.None);
            Grapple.ResetForRetry();
            if (room) { room.gameObject.SetActive(false); Destroy(room.gameObject); }
            TrialActive = trial;
            recordForward = !fromRight;
            roomId = id;
            justFinished = 0;
            roomKey = saveRoot + "room." + id;
            passed = PatrolEntry.Read(roomKey + ".passed") != 0;
            badge = PatrolEntry.Read(roomKey + ".badge") != 0;
            previous.Load(roomKey + ".ghost", PatrolEntry.Read);
            entrance = checkpoint = new Vector2(fromRight ? Width - 2 : 2, 1.1f);
            var go = new GameObject("순찰로 " + id + " · " + PatrolRoom.NameOf(id));
            go.transform.SetParent(transform, false);
            room = go.AddComponent<PatrolRoom>();
            room.Build(this, id);
            if (PatrolEntry.Read(saveRoot + "lastRoom") != id) Write("lastRoom", id);
            PositionPlayer(entrance);
            if (Mathf.Abs(velocity.x) > .1f)
                Player.FinishExternal(new Vector2(velocity.x, Mathf.Max(0, velocity.y)), false);
            recording.Begin(entrance.x, entrance.y, fromRight);
            ghost.enabled = false;
            arrivalFrame = Time.frameCount;
            notice = "";
            if (changedMusic) music.EnterRoom();
        }

        void PositionPlayer(Vector2 position)
        {
            Player.Respawn(position);
            Player.transform.position = position;
            view.transform.position = CameraTarget(position);
            Physics2D.SyncTransforms();
        }
        Vector3 CameraTarget(Vector2 position)
        {
            float half = view.orthographicSize, halfWidth = Mathf.Min(Width / 2, half * view.aspect);
            return new Vector3(Mathf.Clamp(position.x + Player.Facing * 1.7f, halfWidth, Width - halfWidth),
                Mathf.Clamp(position.y + 2.2f, half - 2.6f, 16 - half + .5f), -10);
        }
        public void SetCheckpoint(Vector2 position) { if (!TrialActive) checkpoint = position; }
        public void Retry()
        {
            if (!room) return;
            music.Retry();
            if (TrialActive) { LoadRoom(roomId, false, true, Vector2.zero); return; }
            Grapple.ResetForRetry();
            PositionPlayer(checkpoint);
            // 입구 재시도는 새 기록으로 시작한다. 중간 체크포인트에서는 실제 이동 궤적을 이어 간다.
            if ((checkpoint - entrance).sqrMagnitude < .01f)
                recording.Begin(checkpoint.x, checkpoint.y, Player.Facing < 0);
            arrivalFrame = Time.frameCount;
        }

        void Show(Panel next)
        {
            panel = next;
            InputReader.Resume();
            if (next != Panel.None) InputReader.SetInterfaceOpen(true);
            Time.timeScale = next == Panel.None && Application.isFocused ? 1 : 0;
        }

        void Update()
        {
            if (leaving || !InputReader || !Application.isFocused) return;
            if (panel == Panel.Password || panel == Panel.Map || panel == Panel.CampaignMap)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) { access.Reset(); Show(returnPanel); }
                return;
            }
            if (Time.frameCount != arrivalFrame && Input.GetKeyDown(KeyCode.P))
            {
                ReturnToCampaign();
                return;
            }
            if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
            {
                access.Reset();
                returnPanel = panel;
                mapFloor = (roomId - 1) / 10;
                pinFrame = Time.frameCount;
                Show(Panel.Password);
                return;
            }
            if (panel == Panel.None && InputReader.Paused) { Show(Panel.Pause); return; }
            if (panel == Panel.Pause && Input.GetKeyDown(KeyCode.Escape)) { Show(Panel.None); return; }
            if (panel == Panel.Result && Input.GetKeyDown(KeyCode.T)) { BeginTrial(); return; }
            if (panel != Panel.None) return;
            if (Input.GetKeyDown(KeyCode.T))
            {
                if (passed) BeginTrial();
                else if (justFinished > 0) LoadRoom(justFinished, false, true, Vector2.zero);
                else Notice("반대편 출구까지 순찰하면 이 길에서 시험 주행을 시작할 수 있습니다.");
            }
        }

        public bool BeginTrial()
        {
            if (!passed) return false;
            LoadRoom(roomId, false, true, Vector2.zero);
            return true;
        }
        void LateUpdate()
        {
            if (!room || !Player) return;
            bool wantsRoomMusic = room.WantsRoomMusic();
            music.Tick((roomId - 1) / 10, TrialActive, Paused, wantsRoomMusic);
            room.TickSpeaker(Paused, music.Muted, music.AllowsRoomMusic);
            if (Paused || Time.frameCount == arrivalFrame) return;
            Vector2 pos = Player.transform.position;
            if (justFinished > 0 && pos.x > 9) justFinished = 0;
            if (pos.y < -7) { Retry(); return; }
            if (recordForward) recording.Record(Time.deltaTime, pos.x, pos.y, Player.Facing < 0, presentation.BodyFrame);
            if (TrialActive)
            {
                bool show = previous.Sample(recording.Duration, out float x, out float y, out bool left, out int frame);
                ghost.enabled = show;
                if (show)
                {
                    ghost.sprite = AncientArt.Hero(frame);
                    ghost.transform.position = new Vector2(x, y - .9f);
                    ghost.flipX = left;
                }
            }
            view.transform.position = Vector3.Lerp(view.transform.position, CameraTarget(pos), 1 - Mathf.Exp(-8 * Time.deltaTime));
            if (pos.x >= Width - .75f)
            {
                if (recordForward) FinishRecording();
                if (TrialActive) { music.Checkpoint(); Show(Panel.Result); }
                else if (roomId == RoomCount) { music.Checkpoint(); Show(Panel.Observatory); }
                else
                {
                    int last = recordForward ? roomId : 0;
                    LoadRoom(roomId + 1, false, false, Player.Velocity);
                    justFinished = last;
                }
            }
            else if (pos.x <= .75f)
            {
                if (TrialActive) { Retry(); return; }
                if (roomId > 1) LoadRoom(roomId - 1, true, false, Player.Velocity);
                else { PositionPlayer(entrance); Show(Panel.Pause); }
            }
        }

        void FinishRecording()
        {
            passed = true;
            PatrolEntry.Write(roomKey + ".passed", 1);
            completedTime = Mathf.Max(1, Mathf.RoundToInt(recording.Duration * 1000));
            previousBest = PatrolEntry.Read(roomKey + ".best");
            if (TrialActive)
            {
                if (previousBest <= 0 || completedTime < previousBest) PatrolEntry.Write(roomKey + ".best", completedTime);
                badge = true;
                PatrolEntry.Write(roomKey + ".badge", 1);
            }
            recording.Save(roomKey + ".ghost", PatrolEntry.Write);
            dirty = true;
            Flush();
        }

        public void ReturnToCampaign()
        {
            if (leaving) return;
            leaving = true;
            InputReader.SetInterfaceOpen(true);
            Time.timeScale = 0;
            Flush();
            PatrolEntry.Return();
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                music?.Suspend();
                if (room) room.TickSpeaker(true, false, false);
                Flush();
            }
            Time.timeScale = focused && !leaving && panel == Panel.None ? 1 : 0;
        }
        void OnApplicationPause(bool paused) { if (paused) Flush(); }
        void OnDestroy()
        {
            Flush();
            music?.Dispose();
            Time.fixedDeltaTime = fixedStepBefore;
            Time.timeScale = 1;
        }

        static string Clock(int ms)
        {
            return (ms / 60000).ToString("00") + ":" + (ms / 1000 % 60).ToString("00") + "." + (ms % 1000 / 10).ToString("00");
        }
        void Styles()
        {
            if (title != null) return;
            label = new GUIStyle(GUI.skin.label) { font = AncientKoreanUI.Font, fontSize = 17, alignment = TextAnchor.MiddleLeft };
            title = new GUIStyle(label) { fontSize = 25 };
            small = new GUIStyle(label) { fontSize = 15, wordWrap = true };
            button = new GUIStyle(GUI.skin.button) { font = AncientKoreanUI.Font, fontSize = 17 };
            searchField = new GUIStyle(GUI.skin.textField) { font = AncientKoreanUI.Font, fontSize = 17, alignment = TextAnchor.MiddleLeft };
            roomButton = new GUIStyle(button) { fontSize = 15, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(13, 5, 0, 0) };
        }
        void OnGUI()
        {
            if (leaving || !room) return;
            Styles();
            float scale = Mathf.Max(.05f, Mathf.Min(Screen.width / 1060f, Screen.height / 640f));
            float w = Screen.width / scale, h = Screen.height / scale;
            var matrix = GUI.matrix; var color = GUI.color; int depth = GUI.depth;
            try
            {
                GUI.matrix = Matrix4x4.Scale(Vector3.one * scale); GUI.color = Color.white; GUI.depth = -105;
                if (panel == Panel.None)
                {
                    AncientHUD.Panel(new Rect(18, 16, 365, 72), Ink);
                    GUI.Label(new Rect(31, 18, 340, 34), room.Title, title);
                    GUI.Label(new Rect(32, 52, 340, 26), TrialActive ? "시험 주행    " + Clock((int)(recording.Duration * 1000)) :
                        ((roomId - 1) / 10 + 1) + "층    ·    입자 " + Charge + "    ·    " + (passed ? (badge ? "배지 · T 재도전" : "T  시험 주행") : justFinished > 0 ? "T  방금 지난 길 시험" : "자유 순찰"), small);
                    string hint = Time.unscaledTime < noticeEnd && notice.Length > 0 ? notice : room.Prompt;
                    if (!string.IsNullOrEmpty(hint))
                    {
                        float width = Mathf.Min(w - 62, 710);
                        AncientHUD.Panel(new Rect((w - width) / 2, h - 68, width, 50), Ink);
                        GUI.Label(new Rect((w - width) / 2 + 14, h - 65, width - 28, 42), hint, small);
                    }
                    return;
                }

                AncientHUD.Panel(new Rect(0, 0, w, h), new Color(.008f, .02f, .03f, .91f));
                bool map = panel == Panel.Map || panel == Panel.CampaignMap;
                float panelWidth = map ? 820 : 580, panelHeight = panel == Panel.CampaignMap ? 536 : map ? 445 : 270;
                float x = (w - panelWidth) * .5f, y = (h - panelHeight) * .5f;
                AncientHUD.Panel(new Rect(x, y, panelWidth, panelHeight), Ink);
                AncientHUD.Panel(new Rect(x, y, panelWidth, 2), Cyan);
                if (panel == Panel.Password) DrawPassword(x, y);
                else if (panel == Panel.CampaignMap) DrawCampaignMap(x, y);
                else if (map) DrawMap(x, y);
                else DrawStop(x, y);
            }
            finally { GUI.color = color; GUI.depth = depth; GUI.matrix = matrix; }
        }

        void DrawStop(float x, float y)
        {
            string heading = panel == Panel.Pause ? "순찰 일시정지" : panel == Panel.Result ? "시험 주행 완료" : "관측교 도착";
            GUI.Label(new Rect(x + 25, y + 15, 520, 45), heading, title);
            string message = panel == Panel.Result ? Clock(completedTime) + "  ·  " + (previousBest <= 0 || completedTime < previousBest ? "개인 최고 기록" : "최고 " + Clock(previousBest)) + "  ·  순찰 배지 획득" :
                panel == Panel.Observatory ? "관측교에서 다른 길을 더 둘러보거나 본편으로 복귀할 수 있습니다." :
                room.Title + "   ·   A/D 이동   Space 점프   좌클릭 직선 와이어   Shift 대시";
            GUI.Label(new Rect(x + 26, y + 70, 528, 48), message, small);
            if (panel == Panel.Pause)
            {
                if (GUI.Button(new Rect(x + 26, y + 130, 162, 42), "계속 순찰", button)) Show(Panel.None);
                if (GUI.Button(new Rect(x + 206, y + 130, 162, 42), "재시도", button)) { Show(Panel.None); Retry(); }
            }
            else if (panel == Panel.Result)
            {
                if (GUI.Button(new Rect(x + 26, y + 130, 162, 42), "다시 도전 · T", button)) BeginTrial();
                if (GUI.Button(new Rect(x + 206, y + 130, 162, 42), "다음 순찰로", button))
                    Travel(roomId == RoomCount ? roomId : roomId + 1, roomId == RoomCount);
            }
            else if (GUI.Button(new Rect(x + 26, y + 130, 162, 42), "더 둘러보기", button)) Travel(roomId, true);
            if (GUI.Button(new Rect(x + 386, y + 130, 162, 42), "본편 복귀 · P", button)) ReturnToCampaign();
        }

        void DrawPassword(float x, float y)
        {
            GUI.Label(new Rect(x + 25, y + 15, 505, 45), "개발자 이동", title);
            var e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                int k = (int)e.keyCode, digit = k - (int)KeyCode.Alpha0;
                if (digit < 0 || digit > 9) digit = k - (int)KeyCode.Keypad0;
                if (digit >= 0 && digit <= 9) { if (digit != 0 || Time.frameCount != pinFrame) access.EnterDigit(digit); }
                else if (e.keyCode == KeyCode.Backspace) access.Backspace();
                else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) access.Submit();
                e.Use();
            }
            GUI.Label(new Rect(x + 26, y + 70, 510, 36), access.Rejected ? "비밀번호를 다시 입력하세요." : "숫자 비밀번호 다섯 자리를 입력하세요.", label);
            GUI.Label(new Rect(x + 26, y + 114, 330, 42), new string('●', access.Count) + new string('·', access.Length - access.Count), title);
            if (GUI.Button(new Rect(x + 26, y + 189, 155, 42), "닫기 · Esc", button)) Show(returnPanel);
            if (access.Unlocked) Show(Panel.Map);
        }

        void DrawMap(float x, float y)
        {
            GUI.Label(new Rect(x + 25, y + 15, 540, 45), "개발자 이동 · 순찰로", title);
            if (GUI.Button(new Rect(x + 655, y + 18, 140, 36), "닫기 · Esc", button)) { Show(returnPanel); return; }
            for (int i = 0; i < 5; i++)
                if (GUI.Button(new Rect(x + 25 + i * 156, y + 75, 149, 38), (i + 1) + "층 " + AncientCityLayout.Name(i), button)) mapFloor = i;
            for (int i = 0; i < 10; i++)
            {
                int id = mapFloor * 10 + i + 1;
                if (GUI.Button(new Rect(x + 25 + i % 2 * 390, y + 135 + i / 2 * 53, 382, 45), PatrolRoom.NameOf(id) + "\n" + PatrolCatalog.Device(id), roomButton))
                {
                    access.Reset();
                    Flush();
                    PatrolEntry.BeginPreview();
                    saveRoot = "patrol." + PatrolEntry.Read("patrol.generation") + ".";
                    Charge = Mathf.Clamp(PatrolEntry.Read(saveRoot + "charge"), 0, 99);
                    Travel(id);
                    return;
                }
            }
            if (GUI.Button(new Rect(x + 25, y + 396, 250, 35), "본편 층 · 구역 · 보스", button))
            {
                campaignPage = campaignTab = 0;
                campaignSearch = campaignError = "";
                Show(Panel.CampaignMap);
            }
        }

        void DrawCampaignMap(float x, float y)
        {
            GUI.Label(new Rect(x + 25, y + 14, 375, 47), "개발자 이동 · 본편", title);
            if (GUI.Button(new Rect(x + 458, y + 20, 179, 34), "순찰로 지도", button))
            { Show(Panel.Map); return; }
            if (GUI.Button(new Rect(x + 654, y + 20, 141, 34), "닫기 · Esc", button))
            { access.Reset(); Show(returnPanel); return; }
            GUI.Label(new Rect(x + 26, y + 72, 83, 35), "목적지 검색", small);
            string value = GUI.TextField(new Rect(x + 122, y + 72, 673, 35), campaignSearch, 48, searchField);
            if (value != campaignSearch) { campaignPage = 0; campaignSearch = value; }
            for (int i = 0; i < CampaignTabs.Length; i++)
            {
                var rect = new Rect(x + 25 + i * 194, y + 122, 187, 34);
                if (GUI.Button(rect, CampaignTabs[i], button)) { campaignTab = i; campaignPage = 0; }
                if (campaignTab == i) AncientHUD.Panel(new Rect(rect.x, rect.yMax, rect.width, 2), Cyan);
            }

            int matches = 0;
            AncientDeveloperDestination selected = null;
            var entries = AncientDeveloperTravel.Destinations;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                int category = entry.Group == AncientDeveloperGroup.Floor ? 1 : entry.Group == AncientDeveloperGroup.Area ? 2 : 3;
                string floor = AncientCityLayout.Name(entry.Floor);
                if ((campaignTab != 0 && campaignTab != category) ||
                    !AncientDeveloperAccess.Matches(campaignSearch, entry.Name, floor, CampaignTabs[category])) continue;
                int index = matches++ - campaignPage * CampaignPageSize;
                if (index < 0 || index >= CampaignPageSize) continue;
                string path = category == 1 ? entry.Name : floor + "   /   " + entry.Name;
                if (GUI.Button(new Rect(x + 25, y + 175 + index * 45, 770, 40), CampaignTabs[category] + "     " + path, roomButton)) selected = entry;
            }
            if (selected != null)
            {
                if (!access.Unlocked || leaving) return;
                Flush();
                if (PatrolEntry.TravelCampaign(selected))
                {
                    leaving = true;
                    TrialActive = recordForward = false;
                    access.Reset();
                    return;
                }
                campaignError = "이동할 수 없습니다. 다른 목적지를 선택하거나 다시 시도하세요.";
            }
            if (matches == 0) GUI.Label(new Rect(x + 32, y + 187, 710, 34), "검색 결과가 없습니다.", small);
            int pages = Mathf.Max(1, (matches + CampaignPageSize - 1) / CampaignPageSize);
            if (campaignPage > 0 && GUI.Button(new Rect(x + 25, y + 461, 120, 36), "이전", button)) campaignPage--;
            GUI.Label(new Rect(x + 158, y + 461, 147, 36), (campaignPage + 1) + " / " + pages + "    ·    " + matches + "곳", small);
            if (campaignPage + 1 < pages && GUI.Button(new Rect(x + 309, y + 461, 120, 36), "다음", button)) campaignPage++;
            if (campaignError.Length > 0) GUI.Label(new Rect(x + 25, y + 500, 770, 26), campaignError, small);
        }
    }
}
