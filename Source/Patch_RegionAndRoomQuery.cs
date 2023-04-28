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
    [HarmonyPatch(typeof(RegionAndRoomQuery), nameof(RegionAndRoomQuery.GetDistrict))]
    static class Patch_GetDistrict
    {
        // fix base NullReferenceException in Building_Bed.DeSpawn() because The Pit is a room for itself 
        public static void Postfix(this Thing thing, ref District __result)
        {
            if( thing is Building_ThePit pit && pit.IsDespawning ){
                __result = null;
            }
        }
    }
}
