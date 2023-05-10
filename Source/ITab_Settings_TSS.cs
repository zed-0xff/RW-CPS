﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;


namespace zed_0xff.CPS;

class ITab_Settings_TSS : ITab {

    private Vector2 winSize = new Vector2(400f, 0f);

    private Building_TSS tss { get => this.SelThing as Building_TSS; }

    public ITab_Settings_TSS() {
        this.labelKey = "CPS.AI";
    }

    protected override void UpdateSize() {
        winSize.y = 400f;
        winSize.x = 400f;

        this.size = winSize;
        base.UpdateSize();
    }

    public void Checkbox(Listing_Standard l, string label, ref bool checkOn, string tooltip = null, float height = 0f, float labelPct = 1f, bool disabled = false, float level = 0) {
        float height2 = ((height != 0f) ? height : Text.CalcHeight(label, l.ColumnWidth * labelPct));
        Rect rect = l.GetRect(height2, labelPct);
        rect.xMin += level*16;
        //rect.width = Math.Min(rect.width + 24f, l.ColumnWidth);
        if (!l.BoundingRectCached.HasValue || rect.Overlaps(l.BoundingRectCached.Value))
        {
            if (!tooltip.NullOrEmpty())
            {
                if (Mouse.IsOver(rect))
                {
                    Widgets.DrawHighlight(rect);
                }
                TooltipHandler.TipRegion(rect, tooltip);
            }
            Widgets.CheckboxLabeled(rect, label.Translate(), ref checkOn, disabled: disabled);
        }
        l.Gap(l.verticalSpacing);
    }

    private void _Checkbox(Listing_Standard l, string label, ref bool checkOn, string tooltip = null, float height = 0f, float labelPct = 1f, bool disabled = false) {
        Checkbox(l, label, ref checkOn, tooltip, height, labelPct, disabled, level: 1);
    }

    private void __Checkbox(Listing_Standard l, string label, ref bool checkOn, string tooltip = null, float height = 0f, float labelPct = 1f, bool disabled = false) {
        Checkbox(l, label, ref checkOn, tooltip, height, labelPct, disabled, level: 2);
    }

    private void ___Checkbox(Listing_Standard l, string label, ref bool checkOn, string tooltip = null, float height = 0f, float labelPct = 1f, bool disabled = false) {
        Checkbox(l, label, ref checkOn, tooltip, height, labelPct, disabled, level: 3);
    }

    protected override void FillTab() {
        Listing_Standard l = new Listing_Standard();
        Rect inRect = new Rect(0f, 0f, winSize.x, winSize.y).ContractedBy(10f);

        var ai = tss.ai;
        if( ai == null ){
            // should not happen
            return;
        }

        l.Begin(inRect);

        ////////////////////////// capture

        l.Gap(12);
        l.Label("TSS.AutoCapture".Translate());

        _Checkbox(l, "TSS.prisoners", ref ai.bAutoCapturePrisoners, disabled: !tss.ForPrisoners);
        if( ModsConfig.IdeologyActive ){
            _Checkbox(l, "TSS.slaves", ref ai.bAutoCaptureSlaves, disabled: tss.ForPrisoners);
        }
        _Checkbox(l, "TSS.colonists", ref ai.bAutoCaptureColonists, disabled: tss.ForPrisoners);

        l.Gap(10);

        _Checkbox(l, "TSS.tendable",   ref ai.bCaptureTendable);
        if( ModsConfig.BiotechActive ){
            _Checkbox(l, "TSS.genesRegrowing", ref ai.bCaptureOnlyGenesRegrowing);
        }

        ///////////////////////// eject

        l.GapLine(40);
        l.Label("TSS.AutoEject".Translate());

        _Checkbox(l, "TSS.tendable",               ref ai.bAutoEjectTendable);
        __Checkbox(l, "TSS.onlyIfEnoughBeds",      ref ai.bOnlyIfEnoughMedBeds);

        if( ModsConfig.BiotechActive ){
            _Checkbox(l, "TSS.genesFinishedRegrowing", ref ai.bAutoEjectGenesFinishedRegrowing);
            __Checkbox(l, "TSS.onlyIfGeneExtractor",   ref ai.bOnlyIfGeneExtractor);
            ___Checkbox(l, "TSS.autoExtract",          ref ai.bAutoExtract);
        }

        l.Gap();
        l.End();
    }
}
