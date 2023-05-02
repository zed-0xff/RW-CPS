using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;
using System.Reflection;

namespace zed_0xff.CPS {
    // TSS: make them feel in cramped space
//    [HarmonyPatch(typeof(ThoughtWorker), nameof(ThoughtWorker.CurrentState))]
//    static class Patch__ThoughtWorker__CurrentState
//    {
//        static void Postfix(ref ThoughtState __result, ref ThoughtWorker __instance, Pawn p){
//            if( p.Spawned ) return;
//            if( (__instance is ThoughtWorker_NeedRoomSize) || (__instance is ThoughtWorker_NeedComfort) ){
//                if( __result is ThoughtState ts ){
//                    Log.Warning("[d] " + p + ": " + " | " + __instance + " = " + ts.Active + " " + ts.StageIndex);
//                }
//            }
//        }
//    }
}
