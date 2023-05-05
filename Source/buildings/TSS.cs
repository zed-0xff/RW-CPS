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

// fixes 'Tried to get a resource "UI/Commands/ForPrisoners" from a different thread. All resources must be loaded in the main thread.'
[StaticConstructorOnStartup]
public partial class Building_TSS : Building_MultiEnterable, IStoreSettingsParent, IThingHolderWithDrawnPawn, IBillTab, IBillGiver {
    public /*override*/ int MaxSlots => 16;

    private int ticksWithoutPower = 0;
    private int curOffset = 0;
    private int nPawns = 0;

//        public CompResource pasteComp = null;
//        public CompResource hemogenComp = null;

    private const float BasePawnConsumedNutritionPerDay = 1.6f;
    public const float NutritionBuffer = BasePawnConsumedNutritionPerDay * 10; // should be MaxSlots here?
    public float restEffectiveness = StatDefOf.BedRestEffectiveness.valueIfMissing;
    public float comfort = 0;
    public float beauty = 0;

    [Unsaved(false)]
    private CompPowerTrader cachedPowerComp;

    private CompPowerTrader PowerTraderComp
    {
        get
        {
            if (cachedPowerComp == null)
            {
                cachedPowerComp = this.TryGetComp<CompPowerTrader>();
            }
            return cachedPowerComp;
        }
    }

    public bool PowerOn => PowerTraderComp.PowerOn;

    ///////////////////////////////////////////////////////////////////////////////////////////
    // <forOwnerType>

    public BedOwnerType ForOwnerType
    {
        get
        {
            return forOwnerType;
        }
        set
        {
            if (value != forOwnerType)
            {
                EjectAll();
                forOwnerType = value;
                cachedTopGraphic = null;
                //Notify_ColorChanged();
                NotifyRoomBedTypeChanged();
            }
        }
    }

    private BedOwnerType forOwnerType;
    public bool ForPrisoners
    {
        get
        {
            return forOwnerType == BedOwnerType.Prisoner;
        }
        set
        {
            if (value == ForPrisoners)
            {
                return;
            }
            if (Current.ProgramState != ProgramState.Playing && Scribe.mode != 0)
            {
                Log.Error("TSS: Tried to set ForPrisoners while game mode was " + Current.ProgramState);
                return;
            }
            EjectAll();
            if (value)
            {
                forOwnerType = BedOwnerType.Prisoner;
            }
            else
            {
                forOwnerType = BedOwnerType.Colonist;
                Log.Error("Bed ForPrisoners=false, but should it be for for colonists or slaves?  Set ForOwnerType instead.");
            }
            cachedTopGraphic = null;
            //Notify_ColorChanged();
            NotifyRoomBedTypeChanged();
        }
    }

    private void NotifyRoomBedTypeChanged()
    {
        this.GetRoom()?.Notify_BedTypeChanged();
    }

    private static int lastBedOwnerSetChangeFrame = -1;
    public void SetBedOwnerTypeByInterface(BedOwnerType ownerType)
    {
        if (lastBedOwnerSetChangeFrame == Time.frameCount) {
            return;
        }
        lastBedOwnerSetChangeFrame = Time.frameCount;

        ((ForOwnerType != ownerType) ? SoundDefOf.Checkbox_TurnedOn : SoundDefOf.Checkbox_TurnedOff).PlayOneShotOnCamera();
        ForOwnerType = ownerType;
    }

    // </forOwnerType>
    ///////////////////////////////////////////////////////////////////////////////////////////

    public override void PostMake()
    {
        base.PostMake();
        allowedNutritionSettings = new StorageSettings(this);
        if (def.building.defaultStorageSettings != null)
        {
            allowedNutritionSettings.CopyFrom(def.building.defaultStorageSettings);
        }
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);

        // from Building_Bed
        Region validRegionAt_NoRebuild = map.regionGrid.GetValidRegionAt_NoRebuild(base.Position);
        if (validRegionAt_NoRebuild != null && validRegionAt_NoRebuild.Room.IsPrisonCell) {
            ForPrisoners = true;
        }

        restEffectiveness = 
            def.statBases.StatListContains(StatDefOf.BedRestEffectiveness)
            ? this.GetStatValue(StatDefOf.BedRestEffectiveness) 
            : StatDefOf.BedRestEffectiveness.valueIfMissing;

        comfort = 
            def.statBases.StatListContains(StatDefOf.Comfort)
            ? this.GetStatValue(StatDefOf.Comfort) 
            : StatDefOf.Comfort.valueIfMissing;

        beauty = 
            def.statBases.StatListContains(StatDefOf.Beauty)
            ? this.GetStatValue(StatDefOf.Beauty) 
            : StatDefOf.Beauty.valueIfMissing;
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        sustainerWorking = null;
        base.DeSpawn(mode);
    }

    public float NutritionNeeded
    {
        get
        {
            return NutritionBuffer - NutritionStored;
        }
    }

    ///////////////////////////////////////////////////////////////////////////////////////////
    // <IThingHolderWithDrawnPawn>

    public float HeldPawnDrawPos_Y => DrawPos.y + 3f / 74f;
    public float HeldPawnBodyAngle => base.Rotation.AsAngle;
    public PawnPosture HeldPawnPosture => PawnPosture.LayingOnGroundFaceUp;

    // </IThingHolderWithDrawnPawn>
    ///////////////////////////////////////////////////////////////////////////////////////////
    // <IStoreSettingsParent>
    
    private StorageSettings allowedNutritionSettings;
    public bool StorageTabVisible => true;

    public bool CanAcceptNutrition(Thing thing)
    {
        return allowedNutritionSettings.AllowedToAccept(thing);
    }

    public StorageSettings GetStoreSettings()
    {
        return allowedNutritionSettings;
    }

    public StorageSettings GetParentStoreSettings()
    {
        return def.building.fixedStorageSettings;
    }

    public void Notify_SettingsChanged()
    {
    }

    // </IStoreSettingsParent>
    ///////////////////////////////////////////////////////////////////////////////////////////
    // <Building_Enterable>

    public override bool IsContentsSuspended => false;

    public override Vector3 PawnDrawOffset => Vector3.zero;

    public override AcceptanceReport CanAcceptPawn(Pawn pawn)
    {
        if (!pawn.IsColonist && !pawn.IsSlaveOfColony && !pawn.IsPrisonerOfColony)
        {
            return false;
        }
        if (!pawn.RaceProps.Humanlike || pawn.IsQuestLodger())
        {
            return false;
        }
        if (nPawns >= MaxSlots)
        {
            return "Occupied".Translate();
        }
        if (!PowerOn)
        {
            return "NoPower".Translate().CapitalizeFirst();
        }
        if( ForPrisoners && !pawn.IsPrisonerOfColony ){
            return "ForPrisonerUse".Translate().CapitalizeFirst();
        }
        if( !ForPrisoners && pawn.IsPrisonerOfColony ){
            return "ForColonistUse".Translate().CapitalizeFirst();
        }
        return true;
    }

    public override void TryAcceptPawn(Pawn pawn)
    {
        if ( !CanAcceptPawn(pawn) ) {
            return;
        }
        selectedPawn = null;
        bool num = pawn.DeSpawnOrDeselect();
        if (innerContainer.TryAddOrTransfer(pawn)) {
            SoundDefOf.GrowthVat_Close.PlayOneShot(SoundInfo.InMap(this));
            SelectedPawns.Remove(pawn); // or don't remove?
        }
        if (num) {
            Find.Selector.Select(pawn, playSound: false, forceDesignatorDeselect: false);
        }
    }

    // </Building_Enterable>
    ///////////////////////////////////////////////////////////////////////////////////////////
    // <Sound>

    [Unsaved(false)]
    private Sustainer sustainerWorking = null;

    // </Sound>
    ///////////////////////////////////////////////////////////////////////////////////////////

    private void rotate(){
       if ( topPawns == null ){
           topPawns = new List<Pawn>();
       } else {
           topPawns.Clear();
       }

       Thing[] allPawns = innerContainer.Where((Thing t) => t is Pawn).ToArray();
       if( allPawns.Length <= 4 ){
           foreach( Thing t in allPawns ){
               topPawns.Add(t as Pawn);
           }
       } else {
           curOffset++;
           if( curOffset >= allPawns.Length )
               curOffset = 0;

           int i = curOffset;
           while( topPawns.Count < 4 ){
               if( i >= allPawns.Length )
                   i = 0;
               topPawns.Add(allPawns[i] as Pawn);
               i++;
           }
       }
    }

    ///////////////////////////////////////////////////////////////////////////////////////////
    // <feeding>

    public bool CanDispenseNow => PowerOn && HasEnoughFeedstockInHoppers();

    public virtual bool CanMakePasteFrom(Thing t){
        return t != null
            && !(t is Pawn)
            && !(t is Corpse)
            && Building_NutrientPasteDispenser.IsAcceptableFeedstock(t.def);
    }

    // from Building_NutrientPasteDispenser
    public virtual bool HasEnoughFeedstockInHoppers()
    {
        float num = 0f;
        foreach( Thing feedStock in innerContainer ){
            if( !CanMakePasteFrom(feedStock) ) continue;

            num += (float)feedStock.stackCount * feedStock.GetStatValue(StatDefOf.Nutrition);
            if (num >= def.building.nutritionCostPerDispense) {
                return true;
            }
        }
        return false;
    }

    // from Building_NutrientPasteDispenser
    public virtual Thing TryDispenseFood()
    {
        if (!CanDispenseNow) {
            return null;
        }
        float num = def.building.nutritionCostPerDispense - 0.0001f;
        List<ThingDef> list = new List<ThingDef>();

        foreach( Thing thing in innerContainer ){
            if( !CanMakePasteFrom(thing) ) continue;

            int num2 = Mathf.Min(thing.stackCount, Mathf.CeilToInt(num / thing.GetStatValue(StatDefOf.Nutrition)));
            num -= (float)num2 * thing.GetStatValue(StatDefOf.Nutrition);
            list.Add(thing.def);
            thing.SplitOff(num2);
            if( num <= 0f )
                break;
        }

        Thing meal = ThingMaker.MakeThing(ThingDefOf.MealNutrientPaste);
        CompIngredients compIngredients = meal.TryGetComp<CompIngredients>();
        for (int i = 0; i < list.Count; i++) {
            compIngredients.RegisterIngredient(list[i]);
        }
        return meal;
    }

    void feedOccupants(){
        if( nPawns == 0 ) return;

        foreach( Thing t in innerContainer ){
            Pawn pawn = t as Pawn;
            if( pawn == null || pawn.Dead || pawn.needs?.food == null )
                continue;
            if (pawn.needs.food.CurLevelPercentage > 0.4)
                continue;

            Thing meal = TryDispenseFood();
            if( meal == null )
                break;

            var ingestedNum = meal.Ingested(pawn, pawn.needs.food.NutritionWanted);
            pawn.needs.food.CurLevel += ingestedNum;
            pawn.records.AddTo(RecordDefOf.NutritionEaten, ingestedNum);
        }
    }

    // </feeding>
    ///////////////////////////////////////////////////////////////////////////////////////////

    // 1. play recipeSound (does not play if called in rare tick)
    // 2. tick contained pawns
    // 3. make them rest
    // 4. do bills
    // 5. tick effects
    // 6. call TickRare()
    public override void Tick()
    {
        if( PowerOn ){
            if ( nPawns > 0 ){
                if (sustainerWorking == null || sustainerWorking.Ended) {
                    sustainerWorking = SoundDefOf.GeneExtractor_Working.TrySpawnSustainer(SoundInfo.InMap(this, MaintenanceType.PerTick));
                } else {
                    sustainerWorking.Maintain();
                }
                if (this.IsHashIntervalTick(10)){
                    if (currentBillReport != null) {

                        currentBillReport.workLeft -= 10f*0.5f; /* ProductionSpeedFactor */
                        if (currentBillReport.workLeft <= 0) {
                            ProduceItems();
                            try {
                                // HACK bc we don't create a virtual pawn as RimFactory does, the record will be as if pawn operated on themselves %)
                                Pawn billDoer = currentBillReport.medBill.GiverPawn;
                                int op0 = billDoer.records.GetAsInt(RecordDefOf.OperationsPerformed);
                                var l = new List<Thing>();
                                // tick workbench bill counters, like 'make 10 things'
                                currentBillReport.bill.Notify_IterationCompleted(billDoer, l);
                                // apply medical bill effects and results
                                currentBillReport.medBill.Notify_IterationCompleted(billDoer, l);
                                if( billDoer.records.GetAsInt(RecordDefOf.OperationsPerformed) != op0 ){
                                    // fix them back
                                    billDoer.records.AddTo(RecordDefOf.OperationsPerformed, -1);
                                }
                            } catch (Exception ex) {
                                Log.Error("[!] TSS: error finishing " + currentBillReport.bill + ": " + ex);
                            }
                            currentBillReport = null;
                        }
                    } else if ( this.IsHashIntervalTick(60) ) {
                        //Start Bill if Possible
                        currentBillReport = TryGetNextBill();
                    }
                }
            }

            // check for ejected pawns, tick resting
            int np = 0;
            foreach( Thing t in innerContainer ) {
                if( t is Pawn pawn && !pawn.Dead && pawn.needs.rest != null) {
                    np++;
                    pawn.needs.rest.TickResting(restEffectiveness);
                }
            }
            if( np != nPawns ){
                // someone was ejected
                topPawns = null;
                nPawns = np;
            }
        }
        base.Tick();

        // effects
        if (currentBillReport != null && PowerOn){
            if (recipeEffecter == null) {
                recipeEffecter = currentBillReport.bill.recipe?.effectWorking?.Spawn();
            }
            if (recipeSound == null) {
                recipeSound = currentBillReport.bill.recipe?.soundWorking?.TrySpawnSustainer(this);
            }
            if( (int)Find.CameraDriver.CurrentZoom == 0 ){
                recipeEffecter?.EffectTick(this, this);
            }
            recipeSound?.SustainerUpdate();
        } else {
            if (recipeEffecter != null) {
                recipeEffecter.Cleanup();
                recipeEffecter = null;
            }
            if (recipeSound != null) {
                recipeSound.End();
                recipeSound = null;
            }
        }

        innerContainer.ThingOwnerTick();

        if (this.IsHashIntervalTick(250) || topPawns == null) {
            TickRare();
        }
    }

    // 1. count pawns
    // 2. eject all on power failure check
    // 3. rotate pawns
    // 4. feed pawns
    // 5. draw blood from pawns
    public override void TickRare()
    {
        nPawns = 0;
        foreach( Thing t in innerContainer ){
            if( t is Pawn pawn ){
                nPawns++;
            }
        }

        if( topPawns == null )
            rotate();

        if( PowerOn ){
            ticksWithoutPower = 0;
            feedOccupants();

            if (this.IsHashIntervalTick(2500)) {
                rotate();
            }
            foreach( Thing t in innerContainer ){
                if( t is Pawn pawn && !pawn.Dead ){
                    pawn.health.AddHediff(HediffDefOf.CryptosleepSickness);
                    pawn.needs?.mood?.thoughts?.memories?.RemoveMemoriesOfDef(ThoughtDefOf.SleptOutside);
                }
            }
        } else {
            ticksWithoutPower += 250; // XXX wrong to assume we had 250 ticks since last call
            if( ticksWithoutPower >= 10000 ){ // 4 ingame hours, ~3 minutes IRL
                EjectAll();
            }
        }
        //base.TickRare(); // don't call base because we call TickRare() from Tick() ourselves (questionable)
    }

    void EjectAll(){
        Thing lastResultingThing;
        List<Thing> things = new List<Thing>( innerContainer ); // original list will be modified in process
        foreach( Thing t in things ){
            if( t is Pawn ){
                innerContainer.TryDrop(t, InteractionCell, Map, ThingPlaceMode.Near, out lastResultingThing);
            }
        }
        topPawns = null;
        nPawns = 0;
        selectedPawn = null;
        SelectedPawns.Clear();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref allowedNutritionSettings, "allowedNutritionSettings");
        Scribe_Values.Look(ref forOwnerType, "forOwnerType", BedOwnerType.Colonist);
        Scribe_Values.Look(ref ticksWithoutPower, "ticksWithoutPower", 0);

        Scribe_Deep.Look(ref billStack, "bills", this);
        Scribe_Deep.Look(ref currentBillReport, "currentBillReport");

        if (allowedNutritionSettings == null)
        {
            allowedNutritionSettings = new StorageSettings(this);
            if (def.building.defaultStorageSettings != null)
            {
                allowedNutritionSettings.CopyFrom(def.building.defaultStorageSettings);
            }
        }
    }
}