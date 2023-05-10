using RimWorld;
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
        }
    }
}
