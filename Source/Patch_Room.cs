using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace zed_0xff.CPS
{
    // always mark CPS as a separate room, even outside w/o walls
    [HarmonyPatch(typeof(Room), "get_ProperRoom")]
    static class Patch_ProperRoom
    {
        public static void Postfix(ref bool __result, ref Room __instance)
        {
            if( __result ) return;

            Building_Base b = Cache.Get(__instance.Cells.First(), __instance.Map);
            if( b != null ){
                __result = true;
            }
        }
    }

    // always use indoor temp, even if unroofed
    [HarmonyPatch(typeof(Room), "get_UsesOutdoorTemperature")]
    static class Patch_UsesOutdoorTemperature
    {
        public static void Postfix(ref bool __result, ref Room __instance)
        {
            if( !__result ) return;

            if( Cache.Get(__instance.Cells.First(), __instance.Map) is Building_Cabin c ){
                if( __instance.CellCount == c.def.size.x * c.def.size.z ){
                    __result = !c.IsPowerOn();
                }
            }
        }
    }
}
