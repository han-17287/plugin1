#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Plugin.PlayerCore;

namespace Plugin.AncientCivilization.EditorChecks
{
    // 실제 플레이어 입력과 물리 연동을 검사한다. Game 뷰에 포커스를 둔다.
    public static class DistrictRouteSmokeChecks
    {
        enum LegStep { Aim, Wind, Dock, Tow }

        static TraversalRoom room;
        static PlayerStateMachine player;
        static PlayerBluePlug plug;
        static Keyboard keyboard;
        static Mouse mouse;
        static BluePlugSocket[] itinerary;
        static Vector2 original;
        static LegStep step;
        static bool sampleOnly, pressedTow;
        static int index, frame, stepFrame, arrival;
        static float deadline;

        [MenuItem("PLUGIN/맵/현재 지역 플러그 경로 실물 검사")]
        static void RunRoute() => Run(false);

        internal static void RunSamples() => Run(true);

        static void Run(bool samples)
        {
            if (!EditorApplication.isPlaying || player)
            {
                Debug.LogError("이동 연습장을 Play하고 다른 검사가 끝난 뒤 실행하세요.");
                return;
            }
            room = UnityEngine.Object.FindAnyObjectByType<TraversalRoom>();
            var actor = UnityEngine.Object.FindAnyObjectByType<PlayerStateMachine>();
            var controller = actor ? actor.GetComponent<PlayerBluePlug>() : null;
            if (!room || room.District == null || !actor || !controller || actor.GetComponent<PlayerInputReader>().Paused)
            {
                Debug.LogError("이동 연습장 설정을 닫고 일시정지를 풀어 주세요.");
                return;
            }
            if (samples && room.precision)
            {
                Debug.LogError("안전 바닥으로 전환하고 검사하세요.");
                return;
            }

            sampleOnly = samples;
            player = actor;
            plug = controller;
            original = player.transform.position;
            try
            {
                var all = room.BluePlugSockets();
                itinerary = samples ? SelectSamples(all) : SelectRoute(all, room.District.hooks.Length);
                if (itinerary.Length == 0) throw new InvalidOperationException("검사할 콘센트가 없습니다.");
                keyboard = InputSystem.AddDevice<Keyboard>();
                mouse = InputSystem.AddDevice<Mouse>();
                plug.ResetLink();
                room.ResetForRetry();
                var start = room.District.stops[0];
                player.Respawn(new Vector2(start.x, start.y + .02f));
                index = 0;
                BeginLeg();
                EditorApplication.update += Poll;
            }
            catch (Exception error) { Finish(false, error.Message); }
        }

        static BluePlugSocket[] SelectSamples(BluePlugSocket[] all)
        {
            var chosen = new List<BluePlugSocket>();
            AddSample<FixedGrappleAnchor>(all, chosen);
            AddSample<OneUseGrappleAnchor>(all, chosen);
            AddSample<MovingGrappleAnchor>(all, chosen);
            return chosen.ToArray();
        }

        static BluePlugSocket[] SelectRoute(BluePlugSocket[] all, int authoredCount)
        {
            var planned = new BluePlugSocket[authoredCount];
            Array.Copy(all, planned, authoredCount);
            return planned;
        }

        static void AddSample<T>(BluePlugSocket[] all, List<BluePlugSocket> chosen) where T : GrappleAnchor
        {
            var socket = Array.Find(all, item => item.BoundAnchor is T);
            if (socket) chosen.Add(socket);
        }

        static void BeginLeg()
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            if (sampleOnly && index > 0)
            {
                room.ResetForRetry();
                player.Respawn(itinerary[index].Point + Vector2.left * 4 + Vector2.up * 2);
            }
            Physics2D.SyncTransforms();
            deadline = Time.realtimeSinceStartup + 8;
            frame = stepFrame = Time.frameCount;
            arrival = plug.ArrivalSequence;
            pressedTow = false;
            step = LegStep.Aim;
        }

        static void Poll()
        {
            if (!EditorApplication.isPlaying || !room || !player || !plug)
            {
                Finish(false, "Play 또는 검사 대상이 해제되었습니다.");
                return;
            }
            try
            {
                if (Time.realtimeSinceStartup > deadline)
                    throw new InvalidOperationException($"진행 시간 초과 · 링크 {index + 1} · {step}. Game 뷰에 포커스를 두세요.");
                if (frame == Time.frameCount) return;
                frame = Time.frameCount;
                var target = itinerary[index];
                if (!target) throw new InvalidOperationException("콘센트가 제거되었습니다.");
                keyboard.MakeCurrent();
                var point = Camera.main.WorldToScreenPoint(target.Point);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
                switch (step)
                {
                    case LegStep.Aim:
                        if (!target.Operational || frame < stepFrame + 2) return;
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.G));
                        step = LegStep.Wind;
                        break;
                    case LegStep.Wind:
                        if (plug.Phase != BluePlugPhase.Winding) return;
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                        step = LegStep.Dock;
                        break;
                    case LegStep.Dock:
                        if (plug.Phase != BluePlugPhase.Connected) return;
                        if (plug.Moving) throw new InvalidOperationException("연결 직후의 자동 이동");
                        stepFrame = frame;
                        step = LegStep.Tow;
                        break;
                    case LegStep.Tow:
                        if (!pressedTow && frame > stepFrame)
                        {
                            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.C));
                            pressedTow = true;
                        }
                        if (plug.ArrivalSequence == arrival) return;
                        if (plug.LastArrival != target)
                            throw new InvalidOperationException($"예상한 콘센트에 도착하지 않았습니다 · 링크 {index + 1}");
                        if (target.BoundAnchor is OneUseGrappleAnchor single && !single.Spent)
                            throw new InvalidOperationException("도착한 일회용 콘센트가 소모되지 않았습니다.");
                        if (++index < itinerary.Length) BeginLeg();
                        else Finish(true, $"실제 G 투척, 연결, C 견인으로 콘센트 {itinerary.Length}개에 도착했습니다.");
                        break;
                }
            }
            catch (Exception error) { Finish(false, error.Message); }
        }

        static void Finish(bool ok, string detail)
        {
            EditorApplication.update -= Poll;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            keyboard = null;
            mouse = null;
            if (plug) plug.ResetLink();
            if (room) room.ResetForRetry();
            if (player) player.Respawn(original);
            player = null;
            room = null;
            plug = null;
            itinerary = null;
            if (ok) Debug.Log("PASS: " + detail);
            else Debug.LogError("플러그 실물 검사: " + detail);
        }
    }
}
#endif
