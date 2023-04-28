using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace zed_0xff.CPS {
//    [HarmonyPatch(typeof(PawnRenderer), "GetBodyPos")]
//    [HarmonyPriority(Priority.Last)] // make it last to fix any offsets by other mods, fixes Yayo's animations
//    static class Patch_GetBodyPos
//    {
//        private static readonly AccessTools.FieldRef<PawnRenderer, Pawn> _pawn = AccessTools.FieldRefAccess<PawnRenderer, Pawn>("pawn");
//
//        static void Postfix(PawnRenderer __instance, Vector3 drawLoc, ref bool showBody, ref Vector3 __result)
//        {
//            Pawn pawn = _pawn(__instance);
//            if( pawn == null ) return;
//
//            if( pawn.IsPrisonerOfColony ){
//                // draw only pawn heads while in the pit, only effective for sleeping pawns
//                showBody = false;
//            }
//
//            Building_ThePit pit = pawn.CurrentBed() as Building_ThePit;
//            if( pit == null ) return;
//            if( pit.GetCurOccupant(4) != pawn ) return;
//
//            // shift 5th pawn's head while asleep
//            __result.x += 0.5f;
//            __result.z += 0.5f;
//        }
//    }

    [HarmonyPatch(typeof(PawnRenderer), "RenderPawnInternal")]
    [HarmonyPriority(Priority.Last)] // TODO: check with Yayo's animations
    static class Patch_RenderPawnInternal
    {
        private static readonly AccessTools.FieldRef<PawnRenderer, Pawn> _pawn = AccessTools.FieldRefAccess<PawnRenderer, Pawn>("pawn");

        static void Prefix(ref PawnRenderer __instance, ref Vector3 rootLoc, ref bool renderBody){
            Pawn pawn = _pawn(__instance);
            if( pawn.IsPrisonerOfColony ){
                Building_ThePit pit = Cache.Get(pawn.Position, pawn.Map);
                if( pit != null ){
                    if( pawn.GetPosture().InBed() ){
                        // shift 5th pawn's head while asleep
                        if( pit.GetCurOccupant(4) == pawn ){
                            rootLoc.x += 0.5f;
                            rootLoc.z += 0.5f;
                        }
                    } else {
                        // hide body of not sleeping pawn
                        renderBody = false;
                        rootLoc.z -= 0.4f;
                    }
                }
            }
        }
    }

    // draw no shadow for not sleeping pawns in the pit
    [HarmonyPatch(typeof(PawnRenderer), "DrawInvisibleShadow")]
    static class Patch_DrawInvisibleShadow
    {
        private static readonly AccessTools.FieldRef<PawnRenderer, Pawn> _pawn = AccessTools.FieldRefAccess<PawnRenderer, Pawn>("pawn");

        static bool Prefix(ref PawnRenderer __instance, ref Vector3 drawLoc){
            Pawn pawn = _pawn(__instance);
            if( pawn.IsPrisonerOfColony ){
                return false;
            }
            return true;
        }
    }

//    [HarmonyPatch(typeof(PawnRenderer), "GetBlitMeshUpdatedFrame")]
//    [HarmonyPriority(Priority.Last)] // not sure
//    static class Patch_GetBlitMeshUpdatedFrame
//    {
//        private static readonly AccessTools.FieldRef<PawnRenderer, Pawn> _pawn = AccessTools.FieldRefAccess<PawnRenderer, Pawn>("pawn");
//
//        static void Prefix(ref PawnRenderer __instance, ref PawnDrawMode drawMode){
//            Pawn pawn = _pawn(__instance);
//            if( pawn.IsPrisonerOfColony ){
//                drawMode = PawnDrawMode.HeadOnly;
//            }
//        }
//    }
}
