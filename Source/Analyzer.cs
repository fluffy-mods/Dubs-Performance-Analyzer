﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace DubsAnalyzer
{

    [StaticConstructorOnStartup]
    public static class Harmy
    {
        static Harmy()
        {
            Analyzer.harmony = new Harmony("Dubwise.DubsOptimizer");

            H_KeyPresses.PatchMe();

            var modes = GenTypes.AllTypes.Where(m => m.TryGetAttribute<PerformancePatch>(out _));

            foreach (var mode in modes)
            {
                //AccessTools.Method(mode, "ProfilePatch")?.Invoke(null, null);
                AccessTools.Method(mode, "PerformancePatch")?.Invoke(null, null);
            }

            //foreach (var workGiverDef in DefDatabase<WorkGiverDef>.AllDefsListForReading)
            //{
            //    if (workGiverDef.giverClass == typeof(WorkGiver_DoBill))
            //    {
            //        workGiverDef.giverClass = typeof(WorkGiver_DoBillFixed);
            //    }
            //}

           // GenCommandLine.Restart();
        }
    }

    public class Analyzer : Mod
    {
        public static Harmony harmony;
        public static readonly int MaxHistoryEntries = 3000;
        public static readonly int AveragingTime = 3000;
        public static readonly int UpdateInterval = 60;
        public static ProfileMode SelectedMode;
        public static Dictionary<string, Profiler> Profiles = new Dictionary<string, Profiler>();
        public static List<ProfileLog> Logs = new List<ProfileLog>();
        public static double AverageSum;
        public static PerfAnalSettings Settings;
        public static bool running;
        private static bool RequestStop;

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoSettings(inRect);
        }
        public override string SettingsCategory()
        {
            return "Dubs Optimizer";
        }
        private static int biff;

        public static string SortBy = "Usage";
        public static Thread t1;
        public Analyzer(ModContentPack content) : base(content)
        {
            Settings = GetSettings<PerfAnalSettings>();
        }

        public static object sync = new object();
        private static void ThreadStart(Dictionary<string, Profiler> Profiles)
        {
            Thread.CurrentThread.IsBackground = true;

            var newLogs = new List<ProfileLog>();
            foreach (var key in Profiles.Keys)
            {
                var value = Profiles[key];
                var av = value.History.GetAverageTime(AveragingTime);
                newLogs.Add(new ProfileLog(value.label, string.Empty, av, null, key, string.Empty, 0, value.type));
            }

            var dd = newLogs.Sum(x => x.Average);

            for (var index = 0; index < newLogs.Count; index++)
            {
                var k = newLogs[index];
                var pc = (float)(k.Average / dd);
                var Log = new ProfileLog(k.Label, pc.ToStringPercent(), pc, k.Def, k.Key, k.Mod, pc, k.Type);
                newLogs[index] = Log;
            }

            newLogs.SortByDescending(x => x.Average);

            lock (sync)
            {
                Logs = newLogs;
            }
        }


        public static void Reset()
        {
            Dialog_Graph.reset();
            foreach (var profiler in Profiles)
            {
                profiler.Value.Stop(false);
            }

            Profiles.Clear();
            Logs.Clear();
        }

        public static void StartProfiling()
        {
            running = true;


        }

        public static void StopProfiling()
        {
            RequestStop = true;
        }

        public static bool LogOpen=false;
        public static float delta = 0;
        public static void UpdateEnd()
        {
            if (!running)
            {
                return;
            }

            if (SelectedMode == null)
            {
                return;
            }

            LogOpen =  Find.WindowStack.IsOpen(typeof(EditWindow_Log));

            foreach (Profiler profiler in Profiles.Values)
            {
                profiler.RecordMeasurement();
            }

            var ShouldUpdate = false;
            if (SelectedMode.mode == UpdateMode.Update || SelectedMode.mode == UpdateMode.GUI)
            {
                delta += Time.deltaTime;
                if (delta >= 1f)
                {
                    ShouldUpdate = true;
                    delta -= 1f;
                }
            }
            else if (SelectedMode.mode == UpdateMode.Tick)
            {
                ShouldUpdate = GenTicks.TicksGame % UpdateInterval == 0;
            }

            if (ShouldUpdate)
            {
                foreach (Profiler profiler in Profiles.Values)
                {
                    profiler.MemRiseUpdate();
                }
                ShouldUpdate = false;
                t1 = new Thread(() => ThreadStart(Profiles));
                t1.Start();
            }

            if (RequestStop)
            {
                running = false;
                RequestStop = false;
            }
        }

        public static void HotStart(string key, UpdateMode mode)
        {
            if (SelectedMode?.mode == mode)
            {
                Start(key);
            }
        }
        public static void HotStop(string key, UpdateMode mode)
        {
            if (SelectedMode?.mode == mode)
            {
                Stop(key);
            }
        }

        public static bool LogStack = false;

        public static void Start(string key, Func<string> GetLabel = null, Type ty = null, Def def = null, Thing thing = null)
        {
            if (!running)
            {
                return;
            }

            try
            {
                Profiles[key].Start();
            }
            catch (Exception e)
            {
                if (!Profiles.ContainsKey(key))
                {
                    if (GetLabel != null)
                    {
                        Profiles[key] = new Profiler(key, GetLabel(), ty, def, thing);
                    }
                    else
                    {
                        Profiles[key] = new Profiler(key, key, ty, def, thing);
                    }
                }
                Profiles[key].Start();
            }
        }

        public static void Stop(string key)
        {
            if (!running)
            {
                return;
            }

            try
            {
                if (LogOpen && Dialog_Graph.key == key)
                {
                    Profiles[key].Stop(true);
                }
                else
                {
                    Profiles[key].Stop(false);
                }
            }
            catch (Exception e)
            {
                // ignored
            }
        }

        //public static void DisplayTimerProperties()
        //{
        //    // Display the timer frequency and resolution.
        //    if (Stopwatch.IsHighResolution)
        //    {
        //        Log.Warning("Operations timed using the system's high-resolution performance counter.");
        //    }
        //    else
        //    {
        //        Log.Warning("Operations timed using the DateTime class.");
        //    }

        //    long frequency = Stopwatch.Frequency;
        //    Log.Warning($"  Timer frequency in ticks per second = {frequency}");
        //    long nanosecPerTick = (1000L * 1000L * 1000L) / frequency;
        //    Log.Warning($"  Timer is accurate within {nanosecPerTick} nanoseconds");
        //}
    }
}