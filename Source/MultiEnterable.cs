using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace zed_0xff.CPS;

// same as Building_Enterable, but multiple pawns can enter simultaneously
public abstract class Building_MultiEnterable : Building_Enterable {
    protected HashSet<Pawn> selectedPawns = new HashSet<Pawn>();

    public HashSet<Pawn> SelectedPawns
    {
        get
        {
            return selectedPawns;
        }
    }

    public void SelectPawn2(Pawn pawn)
    {
        selectedPawns.Add(pawn);
        if (!pawn.IsPrisonerOfColony && !pawn.Downed)
        {
            pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(VDefOf.EnterMultiBuilding, this), JobTag.Misc);
        }
    }

    protected override void SelectPawn(Pawn pawn)
    {
        SelectPawn2(pawn);
    }

    public override void DrawExtraSelectionOverlays()
    {
        //base.DrawExtraSelectionOverlays();
        foreach( Pawn p in selectedPawns ){
            if (p != null && p.Map == base.Map) {
                GenDraw.DrawLineBetween(this.TrueCenter(), p.TrueCenter());
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref selectedPawns, "selectedPawns", LookMode.Reference);
    }
}
