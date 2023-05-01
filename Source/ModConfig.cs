using System;
using System.Collections.Generic;
using Verse;
using UnityEngine;

namespace zed_0xff.CPS
{
    public class CPSSettings : ModSettings
    {
        public class TypeSettings {
            public bool draw = true;
            public bool transfuse = true;
            public float transfuseIfLess = 0.2f;
            public float fillUpTo;

            public TypeSettings(float defaultFillUpTo = 0.5f){
                fillUpTo = defaultFillUpTo;
            }

            public virtual void ExposeData(string prefix, float defaultFillUpTo = 0.5f){
                Scribe_Values.Look(ref draw, prefix + ".draw", true);
                Scribe_Values.Look(ref transfuse, prefix + ".transfuse", true);
                Scribe_Values.Look(ref transfuseIfLess, prefix + ".transfuseIfLess", 0.2f);
                Scribe_Values.Look(ref fillUpTo, prefix + ".fillUpTo", defaultFillUpTo);
            }
        };

        public class GeneralSettings {
            public float maxFillRate = 0.8f;
            public bool debugLog = false;

            public float cr, cg, cb, ca;

            public virtual void ExposeData(){
                Scribe_Values.Look(ref maxFillRate, "maxFillRate", 0.8f);
                Scribe_Values.Look(ref debugLog, "debugLog", false);
                Scribe_Values.Look(ref cr, "cr", 1.0f);
                Scribe_Values.Look(ref cg, "cg", 1.0f);
                Scribe_Values.Look(ref cb, "cb", 1.0f);
                Scribe_Values.Look(ref ca, "ca", 1.0f);
            }
        }

        public GeneralSettings general = new GeneralSettings();
        public TypeSettings colonists = new TypeSettings(0.9f);
        public TypeSettings prisoners = new TypeSettings();
        public TypeSettings slaves = new TypeSettings();
        public TypeSettings others = new TypeSettings();

        public override void ExposeData()
        {
            general.ExposeData();
            colonists.ExposeData("colonists", 0.9f);
            prisoners.ExposeData("prisoners");
            slaves.ExposeData("slaves");
            others.ExposeData("others");
            base.ExposeData();
        }
    }

    public class ModConfig : Mod
    {
        public static CPSSettings Settings { get; private set; }

        public ModConfig(ModContentPack content) : base(content)
        {
            Settings = GetSettings<CPSSettings>();
        }

        private void drawBlock(Listing_Standard l, string title, ref CPSSettings.GeneralSettings s){
            l.Label(title);
            l.GapLine();

            l.Label("Stop drawing if storage is " + Math.Round(s.maxFillRate*100) + "% full");
            s.maxFillRate = l.Slider(s.maxFillRate, 0.1f, 1.0f);

            s.cr = l.Slider(s.cr, 0, 1.0f);
            s.cg = l.Slider(s.cg, 0, 1.0f);
            s.cb = l.Slider(s.cb, 0, 1.0f);
            s.ca = l.Slider(s.ca, 0, 1.0f);

            l.Gap();
        }

        private void drawBlock(Listing_Standard l, string title, ref CPSSettings.TypeSettings s){
            l.Label(title);
            l.GapLine();

            if( title == "Prisoners" ){
                l.CheckboxLabeled("Automatically draw blood **", ref s.draw);
            }

            l.CheckboxLabeled("Transfuse blood", ref s.transfuse);

            l.Label("Transfuse if pawn has " + Math.Round(s.transfuseIfLess*100) + "% of blood or less");
            s.transfuseIfLess = l.Slider(s.transfuseIfLess, 0.1f, 0.99f);

            l.Label("Fill them upto " + Math.Round(s.fillUpTo*100) + "%*");
            s.fillUpTo = l.Slider(s.fillUpTo, 0.4f, 1.0f);

            l.Gap();
        }

        private static Vector2 scrollPosition = Vector2.zero;

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard l = new Listing_Standard();

            var scrollContainer = inRect.ContractedBy(10);
            scrollContainer.height -= l.CurHeight;
            scrollContainer.y += l.CurHeight;
            Widgets.DrawBoxSolid(scrollContainer, Color.grey);
            var innerContainer = scrollContainer.ContractedBy(1);
            Widgets.DrawBoxSolid(innerContainer, new ColorInt(42, 43, 44).ToColor);
            var frameRect = innerContainer.ContractedBy(5);
            frameRect.y += 15;
            frameRect.height -= 15;
            var contentRect = frameRect;
            contentRect.x = 0;
            contentRect.y = 0;
            contentRect.width -= 20;
            contentRect.height = 950f;

            Widgets.BeginScrollView(frameRect, ref scrollPosition, contentRect, true);
            l.Begin(contentRect.AtZero());

            drawBlock(l, "General", ref Settings.general);
            drawBlock(l, "Prisoners", ref Settings.prisoners);
            drawBlock(l, "Colonists", ref Settings.colonists);
            drawBlock(l, "Slaves", ref Settings.slaves);
            drawBlock(l, "Others", ref Settings.others);

            l.Label("(*) one hemogen pack adds 35% so it will not be exact amount");
            l.Label("(**) prisoner interaction type should be set to \"Hemogen farm\"");

            l.End();
            Widgets.EndScrollView();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory() => "CPS";
    }
}
