using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.AI;

namespace zed_0xff.CPS;

public partial class Building_TSS {

    public AI ai;

    public class AI : IExposable {
        public bool bAutoCapturePrisoners = false;
        public bool bAutoCaptureColonists = false;
        public bool bAutoCaptureSlaves = false;

        public bool bCaptureTendable = false;
        public bool bCaptureOnlyGenesRegrowing = false;

        public bool bAutoEjectTendable = true;
        public bool bOnlyIfEnoughMedBeds = true;

        public bool bAutoEjectGenesFinishedRegrowing = true;
        public bool bOnlyIfGeneExtractor = true;
        public bool bAutoExtract = true;

        private List<Pawn> geneExtractQueue = new List<Pawn>();

        public void NotifyGenesFinishedRegrowing(Pawn pawn){
            if( !bAutoEjectGenesFinishedRegrowing )
                return;

            geneExtractQueue.Add(pawn);
        }

        private static readonly MethodInfo m_selectPawn = AccessTools.Method(typeof(Building_GeneExtractor), "SelectPawn");
        private static readonly FastInvokeHandler selectPawn = MethodInvoker.GetHandler(m_selectPawn);

        // will eject only 1 pawn at each iteration
        private void autoEject(){
            if( bAutoEjectTendable ){
                foreach( Thing t in tss.innerContainer ){
                    if( !(t is Pawn pawn) ) continue;
                    if( !HealthAIUtility.ShouldEverReceiveMedicalCareFromPlayer(pawn) ) continue; // healthcare is set to 'no medical care'

                    if( pawn.health.HasHediffsNeedingTend() ){
                        if( bOnlyIfEnoughMedBeds ){
                            if( !tss.Map.listerBuildings.allBuildingsColonist.Any((Building x) => x is Building_Bed b && b.Medical && RestUtility.CanUseBedNow(b, pawn, true))){
                                continue; // no medical beds
                            }
                        }
                        tss.Eject(pawn);
                        return;
                    }
                }
            }

            if( bAutoEjectGenesFinishedRegrowing ){
                foreach( Pawn pawn in geneExtractQueue ){
                    if( bOnlyIfGeneExtractor ){ 
                        foreach (var extractor in tss.Map.listerBuildings.AllBuildingsColonistOfClass<Building_GeneExtractor>()) {
                            if (extractor.CanAcceptPawn(pawn)){
                                tss.Eject(pawn);
                                geneExtractQueue.Remove(pawn);
                                if( bAutoExtract ){
                                    selectPawn(extractor, pawn);
                                }
                                return;
                            }
                        }
                    } else {
                        tss.Eject(pawn);
                        geneExtractQueue.Remove(pawn);
                        return;
                    }
                    if( !tss.innerContainer.Contains(pawn) ){
                        // cleanup
                        geneExtractQueue.Remove(pawn);
                        return; // cannot iterate further
                    }
                }
            } else {
                geneExtractQueue.Clear();
            }
        }

        // captures upto MaxSlots pawns each iteration
        private void autoCapture(int n){
            if( tss.ForPrisoners && !bAutoCapturePrisoners ) return;
            if( !tss.ForPrisoners && !bAutoCaptureColonists && !bAutoCaptureSlaves ) return;

            HashSet<Pawn> allSelectedPawns = new HashSet<Pawn>();
            foreach (var b in tss.Map.listerBuildings.AllBuildingsColonistOfClass<Building_TSS>()) {
                allSelectedPawns.AddRange( b.SelectedPawns );
            }

            foreach (Pawn pawn in tss.Map.mapPawns.AllPawnsSpawned) {
                if( allSelectedPawns.Contains(pawn) ) continue;

                AcceptanceReport acceptanceReport = tss.CanAcceptPawn(pawn);
                if( !acceptanceReport.Accepted ) continue;

                if( pawn.IsPrisonerOfColony && !bAutoCapturePrisoners ) continue;
                if( pawn.IsColonistPlayerControlled ){
                    if( pawn.IsSlave && !bAutoCaptureSlaves ) continue;
                    if( !pawn.IsSlave && !bAutoCaptureColonists ) continue;
                }
                if( !pawn.CanReach(tss, PathEndMode.InteractionCell, Danger.Deadly, mode: TraverseMode.PassDoors) ) continue;

                if( !bCaptureTendable && HealthAIUtility.ShouldEverReceiveMedicalCareFromPlayer(pawn) && pawn.health.HasHediffsNeedingTend() )
                    continue;

                if( bCaptureOnlyGenesRegrowing && !pawn.health.hediffSet.HasHediff(HediffDefOf.XenogermReplicating) )
                    continue;

                tss.SelectPawn(pawn);
                n--;
                if( n <= 0 ) break;
            }
        }

        public void Work(){
            if( tss.nPawns > 0 ){
                autoEject();
            }
            int nAvailSlots = tss.MaxSlots - (tss.nPawns + tss.SelectedPawns.Count);
            if( nAvailSlots > 0 ){
                autoCapture(nAvailSlots);
            }
        }

        private Building_TSS tss;

        public AI(Building_TSS tss){
            this.tss = tss;
        }

        public void ExposeData() {
            Scribe_Values.Look(ref bAutoCapturePrisoners, "bAutoCapturePrisoners", false);
            Scribe_Values.Look(ref bAutoCaptureSlaves, "bAutoCaptureSlaves", false);
            Scribe_Values.Look(ref bAutoCaptureColonists, "bAutoCaptureColonists", false);

            Scribe_Values.Look(ref bCaptureTendable, "bCaptureTendable", false);
            Scribe_Values.Look(ref bCaptureOnlyGenesRegrowing, "bCaptureOnlyGenesRegrowing", false);

            Scribe_Values.Look(ref bAutoEjectTendable, "bAutoEjectTendable", true);
            Scribe_Values.Look(ref bOnlyIfEnoughMedBeds, "bOnlyIfEnoughMedBeds", true);

            Scribe_Values.Look(ref bAutoEjectGenesFinishedRegrowing, "bAutoEjectGenesFinishedRegrowing", true);
            Scribe_Values.Look(ref bOnlyIfGeneExtractor, "bOnlyIfGeneExtractor", true);
            Scribe_Values.Look(ref bAutoExtract, "bAutoExtract", true);

            Scribe_Collections.Look(ref geneExtractQueue, "geneExtractQueue", LookMode.Reference);
        }
    }
}
