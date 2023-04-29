using System.Collections.Generic;
using Verse;
using RimWorld;
using UnityEngine;

namespace zed_0xff.CPS
{
    // fixes 'Tried to get a resource "UI/Commands/ForPrisoners" from a different thread. All resources must be loaded in the main thread.'
    [StaticConstructorOnStartup]
    public abstract class Building_Base : Building_Bed {
        public bool IsDespawning = false;

        public abstract int MaxSlots { get; }

        protected static readonly Texture2D pIcon = ContentFinder<Texture2D>.Get("UI/Commands/ForPrisoners");
        protected static readonly string pLabel = "CommandBedSetForPrisonersLabel".Translate();

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            IsDespawning = false;
            // Cache.Add() should be called before base.SpawnSetup() or newly built buildings will not be rooms until save-load cycle
            // this.Map is not yet set at this point
            Cache.Add(this, map);
            // fix number of sleeping slots
            var p = GetComp<CompAssignableToPawn>();
            p.Props.maxAssignedPawnsCount = MaxSlots;
            base.SpawnSetup(map, respawningAfterLoad);
            p.Props.maxAssignedPawnsCount = MaxSlots;
        }

        // fix base NullReferenceException when CPS is a room for itself 
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Cache.Remove(this);
            if( def.building.isFence ){
                // CPS is a room for itself
                District district = this.GetDistrict();
                IsDespawning = true; // see Patch_RegionAndRoomQuery.cs
                base.DeSpawn(mode);
                if (district != null) {
                    district.Notify_RoomShapeOrContainedBedsChanged();
                    if( district.Room != null ){ // <-- in fact, only this check is important here
                        district.Room.Notify_RoomShapeChanged();
                    }
                }
            } else {
                // regular building
                base.DeSpawn(mode);
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
            for (int i = 0; i < MaxSlots; i++)
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

    }
}
