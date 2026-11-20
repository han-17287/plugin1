using System;
using System.Linq;
using Plugin.AncientCivilization;
using UnityEngine;
using UnityEngine.Audio;

static class ScorePlaybackChecks
{
    static AncientScorePlayer player;
    static AudioMixerGroup bus;
    static string route;
    static int frames;
    const float Frame = .02f;

    static void Check(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
    }

    static void Step(int count = 1, bool suspend = false, bool cancel = false, float entry = 0, bool roomMusic = false)
    {
        for (int i = 0; i < count; i++)
        {
            Time.unscaledDeltaTime = Frame;
            player.Tick(route, suspend, cancel, .22f, false, entry, roomMusic);
            Check(AudioSource.All.Count(voice => voice.isPlaying) <= 1, "Two pieces playing after update "+frames);
            foreach (var voice in AudioSource.All) voice.Advance(Frame);
            frames++;
        }
    }

    static AudioSource Live => AudioSource.All.Single(voice => voice.isPlaying);
    static int Attacks(string cue) => AudioSource.All.Sum(voice => voice.Attacks.Count(note => note == cue));

    static void Main()
    {
        bus = new AudioMixerGroup();
        player = new AncientScorePlayer(new Transform(), bus);
        route = "ignition-title";
        Step();
        route = "truck-pursuit";
        Step();
        Resources.Finish("ignition-title");
        Step(5);
        Check(Attacks("ignition-title") == 0, "A stale title load started after the chase was selected");
        var truck = Resources.Finish(route);
        truck.loadState = AudioDataLoadState.Unloaded;
        Step(3);
        Check(Attacks(route) == 0 && truck.loadState == AudioDataLoadState.Loading, "Voice started before sample data was ready");
        truck.loadState = AudioDataLoadState.Loaded;
        Step(220);
        Check(Live.clip == truck && Live.loop && Attacks(route) == 1, "Loop restored its attack or failed to roll over");
        Check(Live.outputAudioMixerGroup == bus, "Song bypassed the music mixer");

        Step(1, suspend:true);
        Check(!AudioSource.All.Any(voice => voice.isPlaying), "A paused voice still plays");
        Step(3);
        Check(Live.clip == truck && Attacks(route) == 1, "Resume restarted the loop");
        player.SetMuted(true);
        Check(Live.mute, "Music ignored mute");
        player.SetMuted(false);

        player.PlayEvent("checkpoint");
        Step();
        player.PlayEvent("death");
        Step();
        Resources.Finish("checkpoint");
        Resources.Finish("death").samples = 100;
        Step(5);
        Check(Live.clip.name == "death" && Live.outputAudioMixerGroup == bus, "Ranked accent not on the music bus");
        Check(Attacks("checkpoint") == 0 && !Live.loop, "Stale or looping checkpoint/death music");
        var held = AudioSource.All.Single(voice => voice.clip == truck);
        int heldPosition = held.timeSamples;
        Step(1, suspend:true);
        Step(1, suspend:true);
        Check(held.timeSamples == heldPosition, "Background song moved during an event");
        Step(6);
        Check(Live.clip == truck && Live.timeSamples >= heldPosition && Attacks(route) == 1, "Death sting restarted the background");

        route = "breach-alarm";
        Step();
        Resources.Finish(route).samples = 80;
        Step(24);
        Check(Attacks(route) == 1 && !AudioSource.All.Any(voice => voice.isPlaying), "Completed wall-breach cue repeated while the scene held");
        route = null;
        Step(3);
        route = "breach-alarm";
        Step(16);
        Check(Attacks(route) == 2, "Re-entered wall breach had no second entrance");

        route = "sixty-to-recovery";
        Step();
        var countdown = Resources.Finish(route);
        countdown.samples = 60000;
        Step(entry:15);
        Check(Live.timeSamples >= 15000 && Live.timeSamples < 15040, "Reloaded countdown lost its place");

        foreach (var next in new[] {"iron-suit-1", "iron-suit-2", "enforcer-1", "enforcer-2", "enforcer-3", "clock-secretary"})
        {
            route = next;
            Step();
            Resources.Finish(next);
            Step(8);
            Check(Live.clip.name == next && Attacks(next) == 1, "Phase remained on stale music: "+next);
        }
        Check(Resources.Discarded.Count > 0, "No streamed clip released after leaving multiple encounters");
        route = "alley-trail";
        Step();
        Resources.Finish(route);
        Step(6);
        route = "clock-secretary";
        Step();
        Check(Attacks("alley-trail") == 0 && Live.clip.name == route, "Briefly crossing an exploration seam restarted the score");

        route = "spotlight-grid";
        Step(24);
        Check(Live.clip.name == "clock-secretary", "Music fell silent while the new region streamed");
        Resources.Finish(route);
        Step(8);
        Check(Live.clip.name == route && Attacks(route) == 1, "Stable region failed to take over after streaming");

        player.PlayEvent("checkpoint");
        Step(1, cancel:true);
        Check(Attacks("checkpoint") == 0, "A discarded accent leaked into a new screen");
        var region = Live;
        Step(roomMusic:true);
        Check(region.isPlaying && !player.AllowsRoomMusic && region.volume < .22f, "Room music overlapped the region fade");
        Step(5, roomMusic:true);
        int regionPosition = region.timeSamples;
        Step(5, roomMusic:true);
        Check(player.AllowsRoomMusic && !region.isPlaying && region.timeSamples == regionPosition,
            "A local melody failed to hold the score transport");
        player.PlayEvent("death");
        Step(roomMusic:true);
        Resources.Finish("death").samples = 100;
        Step(roomMusic:true);
        Check(!player.AllowsRoomMusic && Live.clip.name == "death", "A local melody masked a retry sting");
        Step(7, roomMusic:true);
        Check(player.AllowsRoomMusic && region.timeSamples == regionPosition, "Score resumed before the local melody ended");
        Step();
        Check(region.isPlaying && region.timeSamples > regionPosition && Attacks(route) == 1, "Leaving the speaker restarted the region");
        player.Dispose();
        Check(!AudioSource.All.Any(voice => voice.isPlaying), "Scene teardown left a music voice running");
        Console.WriteLine($"PASS: {frames} director frames; async supersession, load readiness, single active music voice, loop, pause/mute, prioritized stings, resume, one-shots, countdown seek and clip eviction.");
    }
}
