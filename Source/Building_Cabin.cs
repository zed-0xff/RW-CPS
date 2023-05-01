using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;
using UnityEngine;

namespace zed_0xff.CPS
{
    public class Building_Cabin : Building_Base {
        public override int MaxSlots => 10;

        public CompTempControl compTempControl = null;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            compTempControl = GetComp<CompTempControl>();
        }

        public override bool FixSleepingPawnHeadPos(ref Pawn pawn, ref Vector3 pos){
            int slot = GetCurOccupantSlotIndexFast(pawn);
            CellRect r = this.OccupiedRect();
            if( Rotation == Rot4.South || Rotation == Rot4.North ){
                // horizontal
                float slotWidth = 1.0f * r.Width / ((MaxSlots|1)/2);
                pos.x = 0.5f + r.minX + (slot/2)*slotWidth;
                pos.z = r.maxZ + (slot%2)*1.0f - 1.0f;
            } else {
                // vertical
                float slotHeight = 1.0f * r.Height / ((MaxSlots|1)/2);
                pos.x = 0.5f + r.minX + (slot%2)*1.0f;
                pos.z = r.minZ + (slot/2)*slotHeight;
            }
            return true;
        }

        // from Building_Heater, mostly as is
        private void heat(float ambientTemperature){
            if( !IsPowerOn() ) return;

            float num = ((ambientTemperature < 20f) ? 1f : ((!(ambientTemperature > 120f)) ? Mathf.InverseLerp(120f, 20f, ambientTemperature) : 0f));
            float energyLimit = compTempControl.Props.energyPerSecond * num * 4.1666665f;
            float num2 = GenTemperature.ControlTemperatureTempChange(base.Position, base.Map, energyLimit, compTempControl.targetTemperature);
            bool flag = !Mathf.Approximately(num2, 0f);
            CompProperties_Power props = compPowerTrader.Props;
            if (flag) {
                this.GetRoom().Temperature += num2;
                compPowerTrader.PowerOutput = 0f - props.PowerConsumption;
            } else {
                compPowerTrader.PowerOutput = (0f - props.PowerConsumption) * compTempControl.Props.lowPowerConsumptionFactor;
            }
            compTempControl.operatingAtHighPower = flag;
        }

        // from Building_Cooler, adapted
        private void cool(float inTemp){
            if( !IsPowerOn() ) return;
            if( !def.hasInteractionCell ){
                Log.Warning("[!] CPS: cannot cool " + this + " because it has no interaction cell");
                return;
            }

            bool flag = false;
            float outTemp = InteractionCell.GetTemperature(Map);

            float delta = outTemp - inTemp;
            if (inTemp > 40f) {
                delta = outTemp - 40f;
            }
            float num2 = 1f - delta * (1f / 130f);
            if (num2 < 0) {
                num2 = 0;
            }
            float energyLimit = -compTempControl.Props.energyPerSecond * num2 * 4.1666665f; // added minus here
            float num4 = GenTemperature.ControlTemperatureTempChange(Position, Map, energyLimit, compTempControl.targetTemperature);
            flag = !Mathf.Approximately(num4, 0f);
            Room room = Position.GetRoom(Map);

            if (flag)
            {
                this.GetRoom().Temperature += num4;
                GenTemperature.PushHeat(InteractionCell, Map, (0f - energyLimit) * 1.25f);
            }
            
            CompProperties_Power props = compPowerTrader.Props;
            if (flag)
            {
                compPowerTrader.PowerOutput = 0f - props.PowerConsumption;
            }
            else
            {
                compPowerTrader.PowerOutput = (0f - props.PowerConsumption) * compTempControl.Props.lowPowerConsumptionFactor;
            }
            compTempControl.operatingAtHighPower = flag;
        }

        // temperature control
        public override void TickRare()
        {
            compTempControl.operatingAtHighPower = false;

            float inTemp = AmbientTemperature;
            float outTemp = inTemp;
            if( def.hasInteractionCell ){
                outTemp = InteractionCell.GetTemperature(Map);
            }

            // in: 50, out: 27, target: 20

            if( inTemp < compTempControl.targetTemperature ){
                if( outTemp >= inTemp ){
                    // just open the window, works even without power :)
                    inTemp = this.GetRoom().Temperature = Mathf.Min(compTempControl.targetTemperature, outTemp);
                }
                if( (compTempControl.targetTemperature - inTemp) > 1.0f){
                    heat(inTemp);
                }
            } else {
                if( outTemp <= inTemp ){
                    // just open the window, works even without power :)
                    inTemp = this.GetRoom().Temperature = Mathf.Max(compTempControl.targetTemperature, outTemp);
                }
                if( (inTemp - compTempControl.targetTemperature) > 1.0f){
                    cool(inTemp);
                }
            }

            base.TickRare();
        }

        // disable 'set for prisoners' gizmo if we're outside
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos()){
                if( gizmo is Command_Toggle ct && (ct.defaultLabel == pLabel || ct.icon == pIcon) ){
                    foreach (IntVec3 edgeCell in this.OccupiedRect().ExpandedBy(1).EdgeCells){
                        Room room = edgeCell.GetRoom(Map);
                        if( room != null && !RoomCanBePrisonCell(room)){
                            ct.Disable();
                            break;
                        }
                    }
                }
                yield return gizmo;
            }
        }

    }
}
