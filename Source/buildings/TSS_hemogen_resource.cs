using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse;

namespace zed_0xff.CPS;

// a bunch of spaghetti to link hemogen indicator with CompRefuelable %)
public partial class Building_TSS : IResourceStore {
    public float MaxLevelOffset => 0f;

    public int ValueForDisplay => PostProcessValue(RefuelableComp.Fuel);
    public int MaxForDisplay => PostProcessValue(Max);

    public List<float> resourceGizmoThresholds => null;

    [Unsaved(false)]
    private CompRefuelable cachedRefuelableComp;
    public CompRefuelable RefuelableComp
    {
        get
        {
            if (cachedRefuelableComp == null)
            {
                cachedRefuelableComp = this.TryGetComp<CompRefuelable>();
            }
            return cachedRefuelableComp;
        }
    }

    public string ResourceLabel => RefuelableComp.Props.FuelLabel;
    public float TargetValue    => RefuelableComp.TargetFuelLevel;
    public float Max            => RefuelableComp.Props.fuelCapacity;
    public float ValuePercent   => RefuelableComp.FuelPercentOfMax;
    public float Value          => RefuelableComp.Fuel;

    public int PostProcessValue(float value) {
        return (int)value;
    }

    public bool ResourceAllowed {
        get { return RefuelableComp.allowAutoRefuel; }
        set { RefuelableComp.allowAutoRefuel = value; }
    }

    public void SetTargetValuePct(float val){
        RefuelableComp.TargetFuelLevel = val * Max;
    }
}

