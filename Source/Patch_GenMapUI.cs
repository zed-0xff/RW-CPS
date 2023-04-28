using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;
using System.Reflection;

namespace zed_0xff.CPS {
//    [HarmonyPatch(typeof(GenMapUI), nameof(GenMapUI.LabelDrawPosFor), new[] {typeof(Thing), typeof(float)})]
//    static class Patch_LabelDrawPosFor
//    {
//        // shift 5th pawn name label
//        static void Postfix(ref Vector2 __result, Thing thing, float worldOffsetZ)
//        {
//            if (thing is Pawn pawn){
//                Building_ThePit pit = pawn.CurrentBed() as Building_ThePit;
//                if( pit == null ) return;
//                if( pit.GetCurOccupant(4) != pawn ) return;
//
//                __result.x += 0.5f;
//                __result.y += 0.5f;
//            }
//        }
//    }
}
