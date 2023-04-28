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
    [HarmonyPatch(typeof(Room), "get_ProperRoom")]
    static class Patch_ProperRoom
    {
        // always mark The Pit as a separate room, even outside w/o walls
        public static void Postfix(ref bool __result, ref Room __instance)
        {
            if( __result ) return;

            Building_ThePit aPit = Cache.Get(__instance.Cells.First(), __instance.Map);
            if( aPit != null ){
                __result = true;
            }
        }
    }
}
