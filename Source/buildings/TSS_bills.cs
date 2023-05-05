using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using Verse.Sound;

namespace zed_0xff.CPS;

public partial class Building_TSS : Building_MultiEnterable, IStoreSettingsParent, IThingHolderWithDrawnPawn, IBillTab, IBillGiver {

    // from Project RimFactory
    protected class BillReport : IExposable
    {
        public BillReport()
        {
        }
        public BillReport(Bill b, Bill_Medical bm)
        {
            bill = b;
            medBill = bm;
            workTotal = b.recipe.WorkAmountTotal(null);
            workLeft = workTotal;
        }
        public Bill bill;
        public Bill_Medical medBill;
        public float workLeft;
        public float workTotal;
      
        public void ExposeData()
        {
            Scribe_References.Look(ref bill, "bill");
            Scribe_References.Look(ref medBill, "medBill");
            Scribe_Values.Look(ref workLeft, "workLeft");
        }
    }

    protected BillReport currentBillReport;

    private BillStack billStack = null;
    public BillStack BillStack {
        get {
            if( billStack == null )
                billStack = new BillStack(this);
            return billStack;
        }
    }

    public IEnumerable<RecipeDef> AllRecipes => this.def.AllRecipes;
    public IEnumerable<RecipeDef> GetAllRecipes() {
        return this.AllRecipes;
    }

    public bool CurrentlyUsableForBills() {
        return false;
    }

    public bool UsableForBillsAfterFueling() {
        return false;
    }

    public void Notify_BillDeleted(Bill bill) {
    }

    public IEnumerable<IntVec3> IngredientStackCells => Enumerable.Empty<IntVec3>();

    protected virtual void ProduceItems() {
        if( currentBillReport?.medBill?.GiverPawn == null || !innerContainer.Contains(currentBillReport.medBill.GiverPawn) ) return;
    }

    protected IEnumerable<Bill> AllBillsShouldDoNow => from b in BillStack.Bills
                                                       where b.ShouldDoNow()
                                                       select b;

    public static readonly MethodInfo _TryFindBestBillIngredientsInSet =
        typeof(WorkGiver_DoBill).GetMethod("TryFindBestBillIngredientsInSet", BindingFlags.NonPublic | BindingFlags.Static);

    bool TryFindBestBillIngredientsInSet(List<Thing> accessibleThings, Bill b, List<ThingCount> chosen)
    {
        //TryFindBestBillIngredientsInSet Expects a List of Both Avilibale & Allowed Things as "accessibleThings"
        List<IngredientCount> missing = new List<IngredientCount>(); // Needed for 1.4
        return (bool)_TryFindBestBillIngredientsInSet.Invoke(null, new object[] { accessibleThings, b, chosen, new IntVec3(), false, missing });
    }

    // TODO: cache
    Pawn findPawnForBloodDraw( RecipeDef rdef ){
        var r = new Recipe_ExtractHemogen_TSS();
        r.recipe = rdef;

        foreach( Thing t in innerContainer ){
            if( !(t is Pawn pawn) ) continue;

            var bloodLossHediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.BloodLoss);
            if( bloodLossHediff != null) continue;

            if( r.AvailableReport(pawn).Accepted ){
                return pawn;
            }
        }
        return null;
    }

    // TryGetNextBill returns a new BillReport to start if one is available
    protected BillReport TryGetNextBill()
    {
        var allThings = from t in innerContainer where !(t is Pawn) select t;
        IEnumerable<Bill> allBills = AllBillsShouldDoNow;

        foreach (Bill b in allBills)
        {
            if( b.recipe == VDefOf.CPS_DrawBlood_All ){
                Pawn pawn = findPawnForBloodDraw(b.recipe);
                if( pawn == null ) continue;

                Bill_Medical bm = HealthCardUtility.CreateSurgeryBill(pawn, b.recipe, null, null, false);
                return new BillReport(b, bm);
            }
        }
        return null;
    }
}
