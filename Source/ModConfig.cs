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

        public class CommonSettings {
            public float maxFillRate = 0.8f;
            public bool debugLog = false;

            public virtual void ExposeData(){
                Scribe_Values.Look(ref maxFillRate, "maxFillRate", 0.8f);
                Scribe_Values.Look(ref debugLog, "debugLog", false);
            }
        }

        public CommonSettings general = new CommonSettings();
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

        private void drawBlock(Listing_Standard l, string title, ref CPSSettings.CommonSettings s){
            Text.Font = GameFont.Medium;
            l.Label(title);
            Text.Font = GameFont.Small;
            l.GapLine();

            l.Label("Stop drawing blood if storage is " + Math.Round(s.maxFillRate*100) + "% full");
            s.maxFillRate = l.Slider(s.maxFillRate, 0.1f, 1.0f);

            l.Gap();
        }

        private void drawBlock(Listing_Standard l, string title, ref CPSSettings.TypeSettings s){
            Text.Font = GameFont.Medium;
            l.Label(title);
            Text.Font = GameFont.Small;
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
        private int PageIndex = 0;

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var tabRect = new Rect(inRect) {
                y = inRect.y + 40f
            };
            var mainRect = new Rect(inRect) {
                height = inRect.height - 40f,
                y = inRect.y + 40f
            };

            Widgets.DrawMenuSection(mainRect);

            var tabs = new List<TabRecord>
            {
                new TabRecord("TSS".Translate(), () =>
                {
                    PageIndex = 0;
                    WriteSettings();

                }, PageIndex == 0),
            };

            TabDrawer.DrawTabs(tabRect, tabs);

            switch (PageIndex)
            {
                case 0:
                    TSS_Settings(mainRect.ContractedBy(15f));
                    break;
                default:
                    break;
            }
        }

        private void TSS_Settings(Rect inRect){
            Listing_Standard l = new Listing_Standard();
            var viewRect = new Rect(0f, 0f, inRect.width - 60, 1000f); // XXX manual height :(
            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
            l.Begin(viewRect);

            drawBlock(l, "Common", ref Settings.general);
            drawBlock(l, "Prisoners", ref Settings.prisoners);
            drawBlock(l, "Colonists", ref Settings.colonists);
            drawBlock(l, "Slaves", ref Settings.slaves);
            drawBlock(l, "Others", ref Settings.others);

            l.Label("(*) one hemogen pack adds 35% so it will not be exact amount");
            l.Label("(**) prisoner interaction type should be set to \"Hemogen farm\"");

            l.End();
            Widgets.EndScrollView();
        }

        public override string SettingsCategory() => "CPS";
    }
}
