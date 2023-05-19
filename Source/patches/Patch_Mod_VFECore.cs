using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using System;
using UnityEngine;
using Verse;
using Verse.AI;

namespace zed_0xff.CPS;

// TSS: patch VFECore mod gene spawners to continue spawining items when pawn is inside the TSS
[HarmonyPatch]
static class Patch_Mod_VFECore {
    public static MethodInfo origSpawned = AccessTools.Method(typeof(Thing), "get_Spawned");
    public static MethodInfo mySpawned   = AccessTools.Method(typeof(Patch_Mod_VFECore), "Spawned");

    public static MethodInfo origMap     = AccessTools.Method(typeof(Thing), "get_Map");
    public static MethodInfo myMap       = AccessTools.Method(typeof(Patch_Mod_VFECore), "Map");

    public static MethodInfo orig8Way    = AccessTools.Method(typeof(GenAdj), "CellsAdjacent8Way", new[]{ typeof(Thing) });
    public static MethodInfo my8Way      = AccessTools.Method(typeof(Patch_Mod_VFECore), "CellsAdjacent8Way", new[]{ typeof(Thing) });

    // it is different from Patch_MentalStateWorker.cs !
    public static Map Map(Thing thing) {
        Map result = thing.Map;
        if( result == null && thing is Pawn pawn && pawn.ParentHolder is Building_TSS tss && !tss.IsContentsSuspended ){
            result = tss.Map;
        }
        return result;
    }

    // it is different from Patch_MentalStateWorker.cs !
    public static bool Spawned(Thing thing) {
        bool result = thing.Spawned;
        if( !result && thing is Pawn pawn && pawn.ParentHolder is Building_TSS tss && !tss.IsContentsSuspended ){
            result = true;
        }
        return result;
    }

    public static IEnumerable<IntVec3> CellsAdjacent8Way(Thing thing) {
        if( thing is Pawn pawn && pawn.ParentHolder is Building_TSS tss && !tss.IsContentsSuspended ){
            return GenAdj.CellsAdjacent8Way(tss);
        } else {
            return GenAdj.CellsAdjacent8Way(thing);
        }
    }

    static IEnumerable<MethodBase> TargetMethods() {
        Type t = AccessTools.TypeByName("AnimalBehaviours.HediffComp_Spawner");
        if( t != null ){
            MethodInfo m;

            m = AccessTools.Method(t, "TickInterval");
            if( m != null ){
                yield return m;
            }

            m = AccessTools.Method(t, "TryDoSpawn");
            if( m != null ){
                yield return m;
            }

            m = AccessTools.Method(t, "TryFindSpawnCell");
            if( m != null ){
                yield return m;
            }
        }
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
        foreach (var code in instructions){
            if( code.opcode == OpCodes.Callvirt ){
               if( (MethodInfo)code.operand == origSpawned ){
                   code.operand = mySpawned;
               } else if( (MethodInfo)code.operand == origMap ){
                   code.operand = myMap;
               }
            } else if( code.opcode == OpCodes.Call ){
               if( (MethodInfo)code.operand == orig8Way ){
                   code.operand = my8Way;
               }
            }
            yield return code;
        }
    }
}
