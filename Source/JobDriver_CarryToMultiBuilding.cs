using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace zed_0xff.CPS
{
    public class JobDriver_CarryToMultiBuilding : JobDriver
    {
        private const TargetIndex BuildingInd = TargetIndex.A;

        private const TargetIndex TakeeInd = TargetIndex.B;

        private Pawn Takee => (Pawn)job.GetTarget(TargetIndex.B).Thing;

        private Building_MultiEnterable Building => (Building_MultiEnterable)job.GetTarget(TargetIndex.A).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => !Building.CanAcceptPawn(Takee));
            yield return Toils_General.Do(delegate
                    {
                    Building.SelectedPawns.Add(Takee);
                    });
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.OnCell);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return Toils_General.WaitWith(TargetIndex.A, 60, useProgressBar: true);
            yield return Toils_General.Do(delegate
                    {
                    Building.TryAcceptPawn(Takee);
                    });
        }
    }
}
