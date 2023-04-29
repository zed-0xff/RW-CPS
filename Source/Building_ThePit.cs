using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;
using HarmonyLib;

namespace zed_0xff.CPS
{
    public class Building_ThePit : Building_Base
    {
        public override int MaxSlots => 5;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            // always for prisoners
            ForPrisoners = true;
            base.SpawnSetup(map, respawningAfterLoad);
        }

        // disable 'set for prisoners' gizmo
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos()){
                if( gizmo is Command_Toggle ct && (ct.defaultLabel == pLabel || ct.icon == pIcon) ){
                    ct.Disable();
                }
                yield return gizmo;
            }
        }

        // return pawns back in pit if they've been kicked out f.ex. by fighting each other
        public override void TickRare()
        {
            var inner = this.OccupiedRect();
            var outer = inner.ExpandedBy(1);
            foreach( Pawn pawn in OwnersForReading ){
                if( inner.Contains(pawn.Position) ) continue;
                if( !outer.Contains(pawn.Position) ) continue;
                if( !pawn.IsPrisonerOfColony || pawn.Dead || pawn.CarriedBy != null ) continue;
                if( Cache.Get(pawn.Position, pawn.Map) is Building_ThePit ) continue; // pawn is in another pit nearby

                // prisoner is on some edge cell - let's throw them back into the pit
                Log.Message("[d] CPS: teleporting " + pawn + " back into the pit");
                pawn.Position = inner.RandomCell;
                pawn.Notify_Teleported(endCurrentJob: true, resetTweenedPos: false);
            }
            base.TickRare();
        }
    }
}
