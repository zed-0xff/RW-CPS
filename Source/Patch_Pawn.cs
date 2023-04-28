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
        // shift 5th pawn's label
        static void Postfix(ref Pawn __instance, ref Vector3 __result)
        {
            Building_ThePit pit = __instance.CurrentBed() as Building_ThePit;
            if( pit == null ) return;
            if( pit.GetCurOccupant(4) != __instance ) return;

            __result.x += 0.5f;
            __result.z += 0.5f;
        }
    }
}
