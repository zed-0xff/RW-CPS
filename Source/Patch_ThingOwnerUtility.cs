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
    // set comfort temperature for pawns/things inside TSS
    [HarmonyPatch(typeof(ThingOwnerUtility), nameof(ThingOwnerUtility.TryGetFixedTemperature))]
    static class Patch_TryGetFixedTemperature
    {
        static bool Prefix( ref bool __result, ref float temperature, IThingHolder holder, Thing forThing){
            if( holder is Building_TSS b && b.PowerOn ){
                temperature = ( forThing is Pawn ) ? 21f : 0f;
                __result = true;
                return false;
            }
            return true;
        }
    }
}
