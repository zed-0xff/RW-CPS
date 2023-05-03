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

namespace zed_0xff.CPS
{
    // fixes 'Tried to get a resource "UI/Commands/ForPrisoners" from a different thread. All resources must be loaded in the main thread.'
    [StaticConstructorOnStartup]
    public class Building_TSS : Building_MultiEnterable, IStoreSettingsParent, IThingHolderWithDrawnPawn {
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

        public override Vector3 PawnDrawOffset => Vector3.zero; //IntVec3.West.RotatedBy(base.Rotation).ToVector3() / def.size.x;

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
        // <UI>

        static readonly Color TopColorNormal = new Color(0.6313726f, 71f / 85f, 0.7058824f);
        static readonly Color TopColorPrisoner = new Color(1f, 61f / 85f, 11f / 85f);

        private List<Pawn> topPawns = null;

        [Unsaved(false)]
        private Graphic cachedTopGraphic;

        private Graphic TopGraphic {
            get
            {
                if (cachedTopGraphic == null || Prefs.DevMode )
                {
                    cachedTopGraphic = GraphicDatabase.Get<Graphic_Multi>("Things/Building/Misc/GrowthVat/GrowthVatTop",
                            ShaderDatabase.Transparent,
                            new Vector2(1.2f,2.3f),
                            ForPrisoners ? TopColorPrisoner : TopColorNormal );
                }
                return cachedTopGraphic;
            }
        }

        static readonly List<Vector3> PawnDrawOffsets = new List<Vector3> {
            new Vector3(-1.5f, 0, 0),
            new Vector3(-0.5f, 0, 1),
            new Vector3( 0.5f, 0, 1),
            new Vector3( 1.5f, 0, 0),
        };

        public override void Draw()
        {
            base.Draw();

            if( topPawns != null && topPawns.Any() ){
                int i = 0;
                foreach( Pawn pawn in topPawns ){
                    pawn.Drawer.renderer.RenderPawnAt(DrawPos + PawnDrawOffsets[i], null, neverAimWeapon: true);
                    i++;
                    if( i == 4 ){
                        // should not be here, just in case
                        break;
                    }
                }
            }

            for( int i=0; i<4; i++ ){
                TopGraphic.Draw(DrawPos + PawnDrawOffsets[i] + Altitudes.AltIncVect * 2f, base.Rotation, this);
            }
        }

        // draw assigned/total slots count
        public override void DrawGUIOverlay()
        {
            if ((int)Find.CameraDriver.CurrentZoom == 0){
                // don't mix up refilling progress bar and capacity label
                GenMapUI.DrawThingLabel(GenMapUI.LabelDrawPosFor(this, 0), nPawns + "/" + MaxSlots, GenMapUI.DefaultThingLabelColor);
            }
        }

        private static readonly Texture2D InsertIcon = ContentFinder<Texture2D>.Get("UI/Gizmos/InsertPawn");
        private static readonly Texture2D CancelIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos()) {
                yield return gizmo;
            }
            if (base.Faction != Faction.OfPlayer) {
                yield break;
            }

            {
                Command_Toggle ct = new Command_Toggle();
                ct.defaultLabel = "CommandBedSetForPrisonersLabel".Translate();
                ct.defaultDesc = "CommandBedSetForPrisonersDesc".Translate();
                ct.icon = ContentFinder<Texture2D>.Get("UI/Commands/ForPrisoners");
                ct.isActive = () => ForPrisoners;
                ct.toggleAction = delegate
                {
                    SetBedOwnerTypeByInterface((!ForPrisoners) ? BedOwnerType.Prisoner : BedOwnerType.Colonist);
                };
                //            if (!RoomCanBePrisonCell(this.GetRoom()) && !ForPrisoners)
                //            {
                //                ct.Disable("CommandBedSetForPrisonersFailOutdoors".Translate());
                //            }
                ct.hotKey = KeyBindingDefOf.Misc3;
                ct.turnOffSound = null;
                ct.turnOnSound = null;
                yield return ct;
            }

            {
                Command_Action ca = new Command_Action();
                ca.defaultLabel = "InsertPerson".Translate() + "...";
                ca.icon = InsertIcon;
                ca.action = delegate
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    foreach (Pawn item in base.Map.mapPawns.AllPawnsSpawned)
                    {
                        Pawn pawn = item;
                        AcceptanceReport acceptanceReport = CanAcceptPawn(pawn);
                        string text = pawn.LabelShortCap;
                        if (acceptanceReport.Accepted) {
                            list.Add(new FloatMenuOption(text, delegate { SelectPawn(pawn); }, pawn, Color.white));
                        }
                    }
                    if (!list.Any()) {
                        list.Add(new FloatMenuOption("NoViablePawns".Translate(), null)); // Biotech
                    }
                    Find.WindowStack.Add(new FloatMenu(list));
                };
                if (!PowerOn)
                {
                    ca.Disable("NoPower".Translate().CapitalizeFirst());
                }
                yield return ca;
            }

            if( SelectedPawns.Any() ) {
                Command_Action c = new Command_Action();
                c.defaultLabel = "CommandCancelLoad".Translate();
                c.defaultDesc = "CommandCancelLoadDesc".Translate();
                c.icon = CancelIcon;
                c.activateSound = SoundDefOf.Designate_Cancel;
                c.action = delegate
                {
                    foreach( Pawn p in selectedPawns ){
                        if( p.CurJobDef == VThingDefOf.EnterMultiBuilding ){
                            p.jobs.EndCurrentJob(JobCondition.InterruptForced);
                        }
                    }
                    SelectedPawns.Clear();
                };
                yield return c;
            }
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption floatMenuOption in base.GetFloatMenuOptions(selPawn))
            {
                yield return floatMenuOption;
            }
            if (!selPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
            {
                yield return new FloatMenuOption("CannotEnterBuilding".Translate(this) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
                yield break;
            }
            AcceptanceReport acceptanceReport = CanAcceptPawn(selPawn);
            if (acceptanceReport.Accepted)
            {
                yield return new FloatMenuOption("EnterBuilding".Translate(this), delegate { SelectPawn(selPawn); });
            }
            else if (SelectedPawns.Contains(selPawn) && !selPawn.IsPrisonerOfColony)
            {
                yield return new FloatMenuOption("EnterBuilding".Translate(this), delegate {
                        selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(VThingDefOf.EnterMultiBuilding, this), JobTag.Misc);
                        });
            }
            else if (!acceptanceReport.Reason.NullOrEmpty())
            {
                yield return new FloatMenuOption("CannotEnterBuilding".Translate(this) + ": " + acceptanceReport.Reason.CapitalizeFirst(), null);
            }
        }

        private static List<Pawn> tmpQueuedPawns = new List<Pawn>();

        // called only if multiple _colonists_ are selected
        // selected prisoners are just ignored
        public override IEnumerable<FloatMenuOption> GetMultiSelectFloatMenuOptions(List<Pawn> selPawns)
        {
            foreach( Pawn p in selPawns ){
                AcceptanceReport acceptanceReport = CanAcceptPawn(p);
                if( !acceptanceReport.Accepted ){
                    if( !acceptanceReport.Reason.NullOrEmpty() ){
                        yield return new FloatMenuOption("CannotEnterBuilding".Translate(this) + ": " + acceptanceReport.Reason.CapitalizeFirst(), null);
                    }
                    yield break;
                }
            }
            if( nPawns + selPawns.Count > MaxSlots ){
                var reason = "Occupied".Translate();
                yield return new FloatMenuOption("CannotEnterBuilding".Translate(this) + ": " + reason.CapitalizeFirst(), null);
                yield break;
            }

            tmpQueuedPawns.Clear();
            tmpQueuedPawns.AddRange(selPawns);

            yield return new FloatMenuOption("EnterBuilding".Translate(this), delegate {
                    // selPawns list is empty here
                    foreach( Pawn p in tmpQueuedPawns ){
                        p.jobs.TryTakeOrderedJob(JobMaker.MakeJob(VThingDefOf.EnterMultiBuilding, this), JobTag.Misc);
                    }
                    tmpQueuedPawns.Clear();
                    });
        }

        public float NutritionConsumedPerDay
        {
            get
            {
                float num = 0;
                foreach( Thing t in innerContainer ){
                    if( t is Pawn pawn && pawn.needs?.food != null ){
                        num += pawn.needs.food.FoodFallPerTickAssumingCategory(HungerCategory.Fed) * 60000f;
                    }
                }
                return num;
            }
        }

        public float NutritionStored
        {
            get
            {
                float num = 0;
                for (int i = 0; i < innerContainer.Count; i++)
                {
                    Thing thing = innerContainer[i];
                    if( !(thing is Pawn) ){
                        num += (float)thing.stackCount * thing.GetStatValue(StatDefOf.Nutrition);
                    }
                }
                return num;
            }
        }

        public override string GetInspectString(){
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetInspectString());
            if( nPawns > 0 ){
                List <string> names = new List<string>();
                foreach( Thing t in innerContainer ){
                    if( t is Pawn p ){
                        names.Add(p.NameShortColored.Resolve());
                    }
                }
                stringBuilder.AppendLineIfNotEmpty().Append( "CasketContains".Translate().ToString() + ": " + names.ToCommaList() );

                stringBuilder.AppendLineIfNotEmpty().Append("Nutrition".Translate()).Append(": ")
                    .Append(NutritionStored.ToStringByStyle(ToStringStyle.FloatMaxOne));
                stringBuilder.Append(" (-").Append("PerDay".Translate(NutritionConsumedPerDay.ToString("F1"))).Append(")");
            } else {
                stringBuilder.AppendLineIfNotEmpty().Append("Nutrition".Translate()).Append(": ")
                    .Append(NutritionStored.ToStringByStyle(ToStringStyle.FloatMaxOne));
            }
            if( Prefs.DevMode ){
                stringBuilder.AppendLineIfNotEmpty().Append("Ticks without power: " + ticksWithoutPower);
            }
            return stringBuilder.ToString();
        }

        // </UI>
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

        // 1. play sound (does not play if called in rare tick)
        // 2. tick contained pawns
        // 3. make them rest
        // 4. call TickRare()
        public override void Tick()
        {
            if( PowerOn ){
                if ( nPawns > 0 ){
                    if (sustainerWorking == null || sustainerWorking.Ended) {
                        sustainerWorking = SoundDefOf.GeneExtractor_Working.TrySpawnSustainer(SoundInfo.InMap(this, MaintenanceType.PerTick));
                    } else {
                        sustainerWorking.Maintain();
                    }
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
            }
            base.Tick();

            innerContainer.ThingOwnerTick();

            if (this.IsHashIntervalTick(250) || topPawns == null) {
                TickRare();
            }
        }

        // 1. count pawns
        // 2. eject all on power failure check
        // 3. rotate pawns
        // 4. feed pawns
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
}
