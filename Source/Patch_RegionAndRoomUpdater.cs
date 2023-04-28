using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace zed_0xff.CPS
{
    [HarmonyPatch(typeof(RegionAndRoomUpdater), "ShouldBeInTheSameRoom")]
    static class Patch_ShouldBeInTheSameRoom
    {
        // always mark The Pit as a separate room, even outside w/o walls
        public static void Postfix(ref bool __result, District a, District b)
        {
            if( !__result ) return;

            Building_ThePit aPit = Cache.Get(a.Cells.First(), a.Map);
            Building_ThePit bPit = Cache.Get(b.Cells.First(), b.Map);
            if( aPit != null || bPit != null ){
                __result = false;
            }
        }
    }
}
