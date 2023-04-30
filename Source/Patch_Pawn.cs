using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace zed_0xff.CPS {
    [HarmonyPatch(typeof(Pawn), "get_DrawPos")]
    static class Patch_DrawPos
    {
        // fix __instance labels & select frame pos
        // XXX does not influence draw position of a sleeping pawn :(
        static void Postfix(ref Pawn __instance, ref Vector3 __result)
        {
            if( !__instance.RaceProps.Humanlike ) return;

            Building_Base b = Cache.Get(__instance.Position, __instance.Map);
            if( b == null ) return;

            if( !__instance.GetPosture().InBed()) return;

            b.FixSleepingPawnFramePos(ref __instance, ref __result);
        }
    }

//    [HarmonyPatch(typeof(Pawn), "CheckForDisturbedSleep")]
//    static class Patch_CheckForDisturbedSleep
//    {
//        static bool Prefix(ref Pawn __instance, Pawn source)
//        {
//            if( !__instance.RaceProps.Humanlike )
//                return true;
//
//            Log.Warning("[d] CheckForDisturbedSleep: " + source + " -> " + __instance);
//            return true;
//        }
//    }

    // cabin has pretty good sound isolation :)
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.HearClamor))]
    static class Patch_HearClamor
    {
        static bool Prefix(ref Pawn __instance, Thing source)
        {
            if( !__instance.RaceProps.Humanlike )
                return true;

            Building_Cabin cabin = Cache.Get(__instance.Position, __instance.Map) as Building_Cabin;
            if( cabin == null )
                return true;

            return cabin.OccupiedRect().Contains(source.Position);
        }
    }
}
