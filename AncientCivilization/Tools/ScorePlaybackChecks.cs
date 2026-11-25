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

    static void CheckExplorationBeats()
    {
        // Tests use the same fractional sample boundaries as a 136 BPM, 4/4 field loop.
        route = "alley-trail";
        Step();
        var alley = Resources.Finish(route);
        alley.frequency = 1000;
        alley.samples = 42353;
        Step(18);
        Check(Live.clip == alley, "Exploration fixture failed to start");
        var field = Live;
        field.timeSamples = 2800;
        route = "salvage-orbit";
        Step();
        var yard = Resources.Finish(route);
        yard.samples = 41739;
        Step(16);
        int previousHead = field.timeSamples;
        while (Live.clip == alley && field.timeSamples < 3750)
        {
            previousHead = field.timeSamples;
            Step();
        }
        Check(Live.clip == yard, "Ready traversal music failed to take over during the next beat");
        double nearestBeat = Math.Round(previousHead * 136 / 60000d) * 60000d / 136;
        Check(previousHead >= nearestBeat - 1 && previousHead - nearestBeat <= 21,
            "Exploration cue cut off between beats: " + previousHead);
        Step(66);
        route = "alley-trail";
        for (int i = 0; i < 60 && Live.clip != alley; i++) Step();
        Check(Live.clip == alley && Attacks(route) == 2 && Live.timeSamples >= 3529 && Live.timeSamples <= 3560,
            "Re-entering the plug shortcut replayed its intro or resumed between measures");
        Check(Live.volume > 0 && Live.volume < .22f,
            "The first attack on the restored downbeat spent a whole game frame at zero gain");

        // Crossing back onto the audible side before the boundary cancels the scheduled cut immediately.
        field.timeSamples = 2420;
        route = "salvage-orbit";
        Step(16);
        route = "alley-trail";
        Step(20);
        Check(Live.clip == alley && Attacks("salvage-orbit") == 1 && Math.Abs(Live.volume - .22f) < .001f,
            "A plug shortcut briefly played a room the player had already left");

        // An alert or boss must cancel a pending region transition even during the final beat.
        field.timeSamples = 2420;
        route = "salvage-orbit";
        Step(16);
        Check(Live.clip == alley, "Short test seam did not leave the outgoing transport alive");
        route = "iron-suit-3";
        Step();
        Resources.Finish(route);
        Step(5);
        Check(Live.clip.name == route && Attacks("salvage-orbit") == 1,
            "A queued exploration boundary delayed the boss phase");
    }

    static void CheckBossMeasures()
    {
        route = "enforcer-1";
        Step();
        var first = Resources.Finish(route);
        first.samples = 32000;
        Step();
        var transport = Live;
        transport.timeSamples = 19720;
        route = "enforcer-2";
        Step();
        var second = Resources.Finish(route);
        second.samples = 28000;
        Step(10);
        Check(Live.clip == first, "Phase audio chopped the band's outgoing measure");
        int pausedAt = transport.timeSamples;
        Step(2, suspend:true);
        Check(transport.timeSamples == pausedAt, "Pausing advanced the outgoing phrase");
        Step();
        Check(Live.clip == first && transport.timeSamples > pausedAt && Attacks("enforcer-1") == 1,
            "Resuming a pending phase cut away the last notes of the outgoing measure");
        int lastPosition = transport.timeSamples;
        for (int i = 0; i < 12 && Live.clip == first; i++)
        {
            lastPosition = transport.timeSamples;
            Step();
        }
        Check(Live.clip == second && lastPosition >= 20000 && lastPosition <= 20020 &&
            transport.timeSamples == 20 && Attacks(route) == 1, "Phase band missed the downbeat or skipped its attack");

        transport.timeSamples = 27800;
        route = "enforcer-3";
        Step();
        Resources.Finish(route);
        Step(8);
        Check(Live.clip == second, "Wrapped boss score lost the last beat of the loop");
        Step(3);
        Check(Live.clip.name == route && Attacks(route) == 1, "Boss downbeat lost on loop wrap");

        route = "joker-turrets";
        Step();
        var joker = Resources.Finish(route);
        joker.samples = 32000;
        Step(5);
        route = "joker-beam";
        Step();
        Resources.Finish(route);
        Step(5);
        Check(Live.clip.name == route, "The final beam call cannot wait for a musical measure");
    }

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
        // Separate transports mimic a new session and expose the actual clips in the service doubles.
        player.Dispose();
        AudioSource.All.Clear();
        Resources.Pending.Clear();
        player = new AncientScorePlayer(new Transform(), bus);
        CheckExplorationBeats();
        player.Dispose();
        AudioSource.All.Clear();
        Resources.Pending.Clear();
        player = new AncientScorePlayer(new Transform(), bus);
        CheckBossMeasures();
        player.Dispose();
        Check(!AudioSource.All.Any(voice => voice.isPlaying), "Scene teardown left a music voice running");
        Console.WriteLine($"PASS: {frames} director frames; async supersession, load readiness, single active music voice, loop, pause/mute, prioritized stings, resume, one-shots, countdown seek, clip eviction, beat-aligned field changes, bar resumes, boss phase downbeats and immediate final clash override.");
    }
}
