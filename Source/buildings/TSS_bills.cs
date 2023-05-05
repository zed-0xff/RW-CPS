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

    enum DonorType {
        HemogenFarmPrisoner, Prisoner, Slave, Any
    };

    static readonly Dictionary<RecipeDef, DonorType> recipeMap  = new Dictionary<RecipeDef, DonorType>()
    {
        { VDefOf.CPS_DrawBlood_HemogenFarmPrisoners, DonorType.HemogenFarmPrisoner },
        { VDefOf.CPS_DrawBlood_AllPrisoners,         DonorType.Prisoner },
        { VDefOf.CPS_DrawBlood_Slaves,               DonorType.Slave },
        { VDefOf.CPS_DrawBlood_All,                  DonorType.Any },
    };

    // TryGetNextBill returns a new BillReport to start if one is available
    protected BillReport TryGetNextBill()
    {
        IEnumerable<Bill> allBills = AllBillsShouldDoNow;
        if( !allBills.Any() ) return null;

        var dict = new Dictionary<DonorType, Pawn>();
        foreach( Thing t in innerContainer ){
            if( t is Pawn pawn ){
                if( pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.BloodLoss) != null) continue;
                if( pawn.genes != null && pawn.genes.HasGene(GeneDefOf.Hemogenic)) continue;

                if( pawn.IsPrisonerOfColony ){
                    dict[DonorType.Prisoner] = pawn;
                    if( pawn?.guest?.interactionMode == PrisonerInteractionModeDefOf.HemogenFarm ){
                        dict[DonorType.HemogenFarmPrisoner] = pawn;
                    }
                } else if( pawn.IsSlaveOfColony ){
                    dict[DonorType.Slave] = pawn;
                }
                dict[DonorType.Any] = pawn;
            }
        }

        foreach (Bill b in allBills) {
            DonorType dtype;
            if( recipeMap.TryGetValue(b.recipe, out dtype) ){
                Pawn pawn;
                if(dict.TryGetValue(dtype, out pawn)){
                    var r = new Recipe_ExtractHemogen_TSS();
                    r.recipe = b.recipe;
                    if( r.AvailableReport(pawn).Accepted ){
                        Bill_Medical bm = HealthCardUtility.CreateSurgeryBill(pawn, b.recipe, null, null, false);
                        return new BillReport(b, bm);
                    }
                }
            }
        }
        return null;
    }
}
