﻿using HarmonyLib;
using RimWorld;

namespace DubsAnalyzer
{
    [PerformancePatch]
    internal class H_MusicUpdate
    {
        public static void PerformancePatch()
        {
            var skiff = AccessTools.Method(typeof(MusicManagerPlay), nameof(MusicManagerPlay.MusicUpdate));
            Analyzer.harmony.Patch(skiff, new HarmonyMethod(typeof(H_MusicUpdate), nameof(Prefix)));
        }

        public static bool Prefix()
        {
            if (Analyzer.Settings.KillMusicMan)
            {
                return false;
            }
            return true;
        }
    }
}