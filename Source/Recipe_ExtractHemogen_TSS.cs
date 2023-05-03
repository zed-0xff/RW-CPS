using System.Collections.Generic;
using RimWorld;
using Verse;

namespace zed_0xff.CPS;

public class Recipe_ExtractHemogen_TSS : Recipe_ExtractHemogen {

    public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
    {
        if (!ModLister.CheckBiotech("Hemogen extraction"))
        {
            return;
        }
        if (!PawnHasEnoughBloodForExtraction(pawn))
        {
            Messages.Message("MessagePawnHadNotEnoughBloodToProduceHemogenPack".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.NeutralEvent);
            return;
        }
        Hediff hediff = HediffMaker.MakeHediff(HediffDefOf.BloodLoss, pawn);
        hediff.Severity = 0.59f; // 0.6 pops up unwanted health alert
        pawn.health.AddHediff(hediff);
        OnSurgerySuccess(pawn, part, billDoer, ingredients, bill);
        if (IsViolationOnPawn(pawn, part, Faction.OfPlayer))
        {
            ReportViolation(pawn, /*billDoer,*/ pawn.HomeFaction, -1, HistoryEventDefOf.ExtractedHemogenPack);
        }
    }

    // skip billDoer from vanilla RecipeWorker.ReportViolation
    void ReportViolation(Pawn pawn, Faction factionToInform, int goodwillImpact, HistoryEventDef overrideEventDef = null)
    {
        if (factionToInform != null )
        {
            Faction.OfPlayer.TryAffectGoodwillWith(factionToInform, goodwillImpact, canSendMessage: true, !factionToInform.temporary, overrideEventDef ?? HistoryEventDefOf.PerformedHarmfulSurgery);
            QuestUtility.SendQuestTargetSignals(pawn.questTags, "SurgeryViolation", pawn.Named("SUBJECT"));
        }
    }

    private bool PawnHasEnoughBloodForExtraction(Pawn pawn)
    {
        Hediff firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.BloodLoss);
        if (firstHediffOfDef != null)
        {
            return firstHediffOfDef.Severity < 0.45f;
        }
        return true;
    }
}
