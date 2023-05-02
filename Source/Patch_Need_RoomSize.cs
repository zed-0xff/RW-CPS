using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;
using System.Reflection;

namespace zed_0xff.CPS {
    // TSS: make them feel in cramped space, only for inside
//    [HarmonyPatch(typeof(Need_RoomSize), nameof(Need_RoomSize.SpacePerceptibleNow))]
//    static class Patch__Need_RoomSize__SpacePerceptibleNow
//    {
//        static void Postfix(ref float __result, Pawn ___pawn){
//            if( ___pawn.Spawned ) return;
//
//            if( ___pawn.ParentHolder is Building_TSS ){
//                __result = 0.009f; // very cramped
//            }
//        }
//    }
}
