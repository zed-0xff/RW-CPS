using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Verse.AI;

namespace zed_0xff.CPS;

// GetBodyPos: only used here for bed head fix (showBody = false + FixSleepingPawnHeadPos when in Building_Base).
// Body hiding for standing Pit prisoners is done only by Patch_PawnRenderNodeWorker_Body_CanDrawNow.
#if RW16
// 1.6: GetBodyPos(Vector3 drawLoc, PawnPosture posture, out bool showBody)
[HarmonyPatch(typeof(PawnRenderer))]
[HarmonyPriority(Priority.Last)]
static class Patch_GetBodyPos_16
{
    static MethodBase TargetMethod()
    {
        foreach (var m in typeof(PawnRenderer).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (m.Name != "GetBodyPos") continue;
            var ps = m.GetParameters();
            // 1.6: (Vector3 drawLoc, PawnPosture posture, out bool showBody) — match by param count and first arg
            if (ps.Length == 3 && ps[0].ParameterType == typeof(Vector3))
                return m;
        }
        throw new InvalidOperationException("CPS: PawnRenderer.GetBodyPos(Vector3, PawnPosture, out bool) not found.");
    }

    static void Postfix(ref bool showBody, ref Vector3 __result, Pawn ___pawn)
    {
        if (___pawn == null || !___pawn.RaceProps.Humanlike) return;
        Building_Base b = Cache.Get(___pawn.Position, ___pawn.Map);
        if (b != null && ___pawn.GetPosture().InBed())
        {
            showBody = false;
            b.FixSleepingPawnHeadPos(ref ___pawn, ref __result);
        }
    }
}
#else
[HarmonyPatch(typeof(PawnRenderer), "GetBodyPos")]
[HarmonyPriority(Priority.Last)]
static class Patch_GetBodyPos
{
    static void Postfix(PawnRenderer __instance, Vector3 drawLoc, ref bool showBody, ref Vector3 __result, Pawn ___pawn)
    {
        if( ___pawn == null || !___pawn.RaceProps.Humanlike ) return;

        Building_Base b = Cache.Get(___pawn.Position, ___pawn.Map);
        if( b == null ) return;

        if( ___pawn.GetPosture().InBed()){
            showBody = false;
            b.FixSleepingPawnHeadPos(ref ___pawn, ref __result);
        }
    }
}
#endif

#if RW16
// 1.6: body hiding only via CanDrawNow; no RenderPawnInternal patch
#else
[HarmonyPatch(typeof(PawnRenderer), "RenderPawnInternal")]
[HarmonyPriority(Priority.Last)]
static class Patch_RenderPawnInternal
{
    static void Prefix(ref PawnRenderer __instance, ref Vector3 rootLoc, ref bool renderBody, Pawn ___pawn){
        if( !___pawn.RaceProps.Humanlike ) return;

        Building_Base b = Cache.Get(___pawn.Position, ___pawn.Map);
        if( b == null ) return;

        if( b is Building_ThePit && ___pawn.IsPrisonerOfColony && !___pawn.GetPosture().InBed() ){
            renderBody = false;
            rootLoc.z -= 0.4f;
        }
    }
}
#endif


// fix pawn head rotation for any Find.CameraDriver.ZoomRootSize
// cached, requires pawn to go out of bed and back to update
[HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.BodyAngle))]
[HarmonyPriority(Priority.Last)] // not sure
static class Patch_BodyAngle
{
    static float Postfix(float __result, ref PawnRenderer __instance, Pawn ___pawn){
        if( __result == 0f ) return __result; // most frequent case

        if( !___pawn.RaceProps.Humanlike ) return __result;

        Building_Bed bed = ___pawn.CurrentBed();
        if( bed is Building_Base ){
            return 0f;
        }
        return __result;
    }
}

// draw no shadow for not sleeping pawns in the pit
#if RW16
// 1.6: shadow is drawn from RenderPawnAt -> DrawShadowInternal (and from DrawTracker -> RenderShadowOnlyAt -> DrawShadowInternal). Patch the single implementation.
[HarmonyPatch(typeof(PawnRenderer), "DrawShadowInternal")]
static class Patch_PawnRenderer_DrawShadowInternal
{
    static readonly FieldInfo f_pawn = typeof(PawnRenderer).GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance);

    static bool Prefix(PawnRenderer __instance)
    {
        if (f_pawn?.GetValue(__instance) is not Pawn pawn) return true;
        if (pawn.IsPrisonerOfColony && Cache.Get(pawn.Position, pawn.Map) is Building_ThePit)
            return false;
        return true;
    }
}

#else
[HarmonyPatch(typeof(PawnRenderer), "DrawInvisibleShadow")]
static class Patch_DrawInvisibleShadow
{
    static bool Prefix(ref PawnRenderer __instance, ref Vector3 drawLoc, Pawn ___pawn){
        if( ___pawn.IsPrisonerOfColony && Cache.Get(___pawn.Position, ___pawn.Map) is Building_ThePit ){
            return false;
        }
        return true;
    }
}
#endif

#if RW16
[HarmonyPatch(typeof(PawnRenderNodeWorker_Body), nameof(PawnRenderNodeWorker_Body.CanDrawNow))]
static class Patch_PawnRenderNodeWorker_Body_CanDrawNow
{
    static bool Prefix(PawnRenderNode node, ref bool __result)
    {
        if (node?.tree?.pawn is not Pawn pawn || !pawn.RaceProps.Humanlike) return true;
        if (!pawn.IsPrisonerOfColony || pawn.GetPosture().InBed()) return true;
        if (Cache.Get(pawn.Position, pawn.Map) is not Building_ThePit) return true;
        __result = false;
        return false;
    }
}
#endif
