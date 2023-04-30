using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace zed_0xff.CPS {
    // without this (almost) any 1st cabin sleeper is not willing to share the cabin)
    [HarmonyPatch(typeof(RestUtility), nameof(RestUtility.BedOwnerWillShare))]
    public static class Patch_BedOwnerWillShare
    {
        public static void Postfix(Building_Bed bed, ref bool __result)
        {
            if( __result || !(bed is Building_Cabin) ) return;

            __result = bed.AnyUnownedSleepingSlot;
        }
    }
}
