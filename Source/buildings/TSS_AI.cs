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
        public bool bCaptureGenesRegrowing = true;

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

        private void autoEject(){
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
                }
            } else {
                geneExtractQueue.Clear();
            }
        }

        private void autoCapture(){
        }

        public void Work(){
            if( tss.nPawns > 0 ){
                autoEject();
            }
            if( tss.nPawns < tss.MaxSlots ){
                autoCapture();
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
            Scribe_Values.Look(ref bCaptureGenesRegrowing, "bCaptureGenesRegrowing", true);

            Scribe_Values.Look(ref bAutoEjectTendable, "bAutoEjectTendable", true);
            Scribe_Values.Look(ref bOnlyIfEnoughMedBeds, "bOnlyIfEnoughMedBeds", true);

            Scribe_Values.Look(ref bAutoEjectGenesFinishedRegrowing, "bAutoEjectGenesFinishedRegrowing", true);
            Scribe_Values.Look(ref bOnlyIfGeneExtractor, "bOnlyIfGeneExtractor", true);
            Scribe_Values.Look(ref bAutoExtract, "bAutoExtract", true);

            Scribe_Collections.Look(ref geneExtractQueue, "geneExtractQueue", LookMode.Reference);
        }
    }
}
