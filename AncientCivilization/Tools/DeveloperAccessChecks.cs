using System;
using Plugin.AncientCivilization;

static class DeveloperAccessChecks
{
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    public static void Main()
    {
        var access = new AncientDeveloperAccess();
        Check(!access.Unlocked && access.Count == 0, "Opening starts locked");
        access.Backspace();
        access.EnterDigit(-1);
        access.EnterDigit(10);
        Check(access.Count == 0, "Only digits enter the passcode");

        for (int code = 0; code < 100000; code++)
        {
            access.Reset();
            for (int place = 10000; place >= 1; place /= 10)
                access.EnterDigit(code / place % 10);
            Check(access.Unlocked == (code == 17287), "Exactly one five-digit code grants access");
            Check(access.Rejected != access.Unlocked && access.Count == 0, "No secret left after submission");
        }

        access.Reset();
        access.EnterDigit(1);
        access.EnterDigit(0);
        access.Backspace();
        foreach (int d in new[] { 7, 2, 8, 7 }) access.EnterDigit(d);
        Check(access.Unlocked, "Editing works");
        access.Reset();
        access.EnterDigit(1);
        access.Submit();
        Check(!access.Unlocked && access.Rejected, "Early confirm cannot unlock");
        foreach (int d in new[] { 1, 7, 2, 8, 7 }) access.EnterDigit(d);
        Check(access.Unlocked, "Correct retry succeeds");
        access.Reset();
        Check(!access.Unlocked && !access.Rejected, "Reopening requires fresh authentication");

        Check(AncientDeveloperAccess.Matches("  ", "조커", "최상층 통제구", "보스"), "Empty search");
        Check(AncientDeveloperAccess.Matches(" 조커 ", "조커", "최상층 통제구", "보스"), "Search by destination");
        Check(AncientDeveloperAccess.Matches("최상층", "조커", "최상층 통제구", "보스"), "Search by floor");
        Check(AncientDeveloperAccess.Matches("보스", "조커", "최상층 통제구", "보스"), "Search by group");
        Check(!AncientDeveloperAccess.Matches("없는 곳", "조커", "최상층 통제구", "보스"), "No match");
        Console.WriteLine("Developer access checks passed");
    }
}
