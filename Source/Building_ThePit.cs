using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace zed_0xff.CPS
{
    // fixes 'Tried to get a resource "UI/Commands/ForPrisoners" from a different thread. All resources must be loaded in the main thread.'
    [StaticConstructorOnStartup]
    public class Building_ThePit : Building_Bed
    {
        public const int maxSlots = 5;
        public bool IsDespawning = false;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            IsDespawning = false;
            // always for prisoners
            ForPrisoners = true;
            // fix number of sleeping slots
            var p = GetComp<CompAssignableToPawn>();
            p.Props.maxAssignedPawnsCount = maxSlots;
            base.SpawnSetup(map, respawningAfterLoad);
            p.Props.maxAssignedPawnsCount = maxSlots;
            Cache.Add(this);
        }

        private static readonly MethodInfo m_despawn = AccessTools.Method(typeof(Building), "DeSpawn");
        private static readonly FastInvokeHandler building_despawn = MethodInvoker.GetHandler(m_despawn);

        private static readonly MethodInfo m_RemoveAllOwners = AccessTools.Method(typeof(Building_Bed), "RemoveAllOwners");
        private static readonly FastInvokeHandler bed_RemoveAllOwners = MethodInvoker.GetHandler(m_RemoveAllOwners);

        // fix base NullReferenceException because The Pit is a room for itself 
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Cache.Remove(this);
            District district = this.GetDistrict();
            IsDespawning = true;
            base.DeSpawn(mode);
            if (district != null) {
                district.Notify_RoomShapeOrContainedBedsChanged();
                if( district.Room != null ){ // <-- in fact, only this check is important here
                    district.Room.Notify_RoomShapeChanged();
                }
            }
        }

        private static readonly Texture2D pIcon = ContentFinder<Texture2D>.Get("UI/Commands/ForPrisoners");
        private static readonly string pLabel = "CommandBedSetForPrisonersLabel".Translate();

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

        // fix pawn in 5th slot unable to lay down
        new public Pawn GetCurOccupant(int slotIndex)
        {
            if (!base.Spawned) return null;

            IntVec3 sleepingSlotPos = GetSleepingSlotPos(slotIndex);
            List<Thing> list = Map.thingGrid.ThingsListAt(sleepingSlotPos);
            if( slotIndex >= OwnersForReading.Count )
                return null;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is Pawn pawn && pawn.CurJob != null && pawn.GetPosture().InBed() && OwnersForReading[slotIndex] == pawn)
                {
                    return pawn;
                }
            }
            return null;
        }

        // fix pawn in 5th slot unable to lay down
        new public Pawn GetCurOccupantAt(IntVec3 pos)
        {
            for (int i = 0; i < SleepingSlotsCount; i++)
            {
                if (GetSleepingSlotPos(i) == pos)
                {
                    Pawn pawn = GetCurOccupant(i);
                    if( pawn != null )
                        return pawn;
                }
            }
            return null;
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
                if( Cache.Get(pawn.Position, pawn.Map) != null ) continue; // pawn is in another pit nearby

                // prisoner is on some edge cell - let's throw them back into the pit
                Log.Message("[d] CPS: teleporting " + pawn + " back into the pit");
                pawn.Position = inner.RandomCell;
                pawn.Notify_Teleported(endCurrentJob: true, resetTweenedPos: false);
            }
        }
    }
}
