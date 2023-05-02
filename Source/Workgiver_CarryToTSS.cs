using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace zed_0xff.CPS {
    public class WorkGiver_CarryToTSS : WorkGiver_CarryToBuilding {
        public override ThingRequest ThingRequest => ThingRequest.ForDef(VThingDefOf.CPS_TSS);
    }
}
