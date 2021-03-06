﻿using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace DubsAnalyzer
{
    [ProfileMode("Single Tick", UpdateMode.Tick)]
    internal class H_DoSingleTick
    {
        public static bool Active = false;

        public static void Start(object __instance, MethodBase __originalMethod, ref string __state)
        {
            if (!Active || !Analyzer.running) return;

            if (__instance != null)
            {
                __state = __instance.GetType().Name;
            }
            else if (__originalMethod.ReflectedType != null)
            {
                __state = __originalMethod.ReflectedType.Name;
            }
            else
            {
                __state = __originalMethod.GetType().Name;
            }

            Analyzer.Start(__state);
        }

        public static void Stop(string __state)
        {
            if (Active && !string.IsNullOrEmpty(__state))
            {
                Analyzer.Stop(__state);
            }
        }

        public static void ProfilePatch()
        {
            var go = new HarmonyMethod(typeof(H_DoSingleTick), nameof(Start));
            var biff = new HarmonyMethod(typeof(H_DoSingleTick), nameof(Stop));

            void slop(Type e, string s)
            {
                Analyzer.harmony.Patch(AccessTools.Method(e, s), go, biff);
            }
            slop(typeof(GameComponentUtility), nameof(GameComponentUtility.GameComponentTick));
            slop(typeof(ScreenshotTaker), nameof(ScreenshotTaker.QueueSilentScreenshot));
            slop(typeof(FilthMonitor), nameof(FilthMonitor.FilthMonitorTick));
            slop(typeof(Map), nameof(Map.MapPreTick));
            slop(typeof(Map), nameof(Map.MapPostTick));
            slop(typeof(DateNotifier), nameof(DateNotifier.DateNotifierTick));
            slop(typeof(Scenario), nameof(Scenario.TickScenario));
            slop(typeof(World), nameof(World.WorldTick));
            slop(typeof(StoryWatcher), nameof(StoryWatcher.StoryWatcherTick));
            slop(typeof(GameEnder), nameof(GameEnder.GameEndTick));
            slop(typeof(Storyteller), nameof(Storyteller.StorytellerTick));
            slop(typeof(TaleManager), nameof(TaleManager.TaleManagerTick));
            slop(typeof(World), nameof(World.WorldPostTick));
            slop(typeof(History), nameof(History.HistoryTick));
            slop(typeof(LetterStack), nameof(LetterStack.LetterStackTick));
            slop(typeof(Autosaver), nameof(Autosaver.AutosaverTick));
        }
    }
}