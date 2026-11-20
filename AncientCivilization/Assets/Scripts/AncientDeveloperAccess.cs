namespace Plugin.AncientCivilization
{
    // 인증은 창을 닫을 때마다 지운다. 저장 파일에는 남기지 않는다.
    public sealed class AncientDeveloperAccess
    {
        const string Passcode = "17287";
        string digits = "";

        public bool Unlocked { get; private set; }
        public bool Rejected { get; private set; }
        public int Count => digits.Length;
        public int Length => Passcode.Length;

        public void Reset()
        {
            digits = "";
            Rejected = Unlocked = false;
        }

        public void EnterDigit(int digit)
        {
            if (Unlocked || digit < 0 || digit > 9) return;
            Rejected = false;
            digits += (char)('0' + digit);
            if (Count == Length) Submit();
        }

        public void Backspace()
        {
            if (Unlocked) return;
            if (Count > 0) digits = digits.Substring(0, Count - 1);
            Rejected = false;
        }

        public void Submit()
        {
            if (Unlocked) return;
            Unlocked = digits == Passcode;
            Rejected = !Unlocked;
            digits = "";
        }

        public static bool Matches(string query, string name, string floor, string group)
        {
            if (string.IsNullOrWhiteSpace(query)) return true;
            string text = query.Trim();
            return Contains(name, text) || Contains(floor, text) || Contains(group, text);
        }

        static bool Contains(string source, string text)
        {
            return source.IndexOf(text, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
