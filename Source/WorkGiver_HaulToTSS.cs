using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace zed_0xff.CPS {
    public class WorkGiver_HaulToTSS : WorkGiver_Scanner
    {
        private const float NutritionBuffer = 2.5f;

        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(VThingDefOf.CPS_TSS);

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced))
            {
                return false;
            }
            if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
            {
                return false;
            }
            if (t.IsBurning())
            {
                return false;
            }
            if (!(t is Building_TSS b))
            {
                return false;
            }
            if (b.NutritionNeeded > NutritionBuffer)
            {
                if (FindNutrition(pawn, b).Thing == null)
                {
                    JobFailReason.Is("NoFood".Translate());
                    return false;
                }
                return true;
            }
            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Building_TSS b))
            {
                return null;
            }
            if (b.NutritionNeeded > 0f)
            {
                ThingCount thingCount = FindNutrition(pawn, b);
                if (thingCount.Thing != null)
                {
                    Job job = HaulAIUtility.HaulToContainerJob(pawn, thingCount.Thing, t);
                    job.count = Mathf.Min(job.count, thingCount.Count);
                    return job;
                }
            }
            return null;
        }

        private bool CanHaulSelectedThing(Pawn pawn, Thing selectedThing)
        {
            if (!selectedThing.Spawned || selectedThing.Map != pawn.Map)
            {
                return false;
            }
            if (selectedThing.IsForbidden(pawn) || !pawn.CanReserveAndReach(selectedThing, PathEndMode.OnCell, Danger.Deadly, 1, 1))
            {
                return false;
            }
            return true;
        }

        private ThingCount FindNutrition(Pawn pawn, Building_TSS b)
        {
            Thing thing = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.FoodSourceNotPlantOrTree), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, Validator);
            if (thing == null)
            {
                return default(ThingCount);
            }
            int n = Mathf.CeilToInt(b.NutritionNeeded / thing.GetStatValue(StatDefOf.Nutrition));
            return new ThingCount(thing, Mathf.Min(thing.stackCount, n));

            bool Validator(Thing x)
            {
                if (x.IsForbidden(pawn) || !pawn.CanReserve(x))
                {
                    return false;
                }
                if (!b.CanAcceptNutrition(x))
                {
                    return false;
                }
                if (x.def.GetStatValueAbstract(StatDefOf.Nutrition) > b.NutritionNeeded)
                {
                    return false;
                }
                return true;
            }
        }
    }
}
