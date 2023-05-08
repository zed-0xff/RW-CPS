using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using UnityEngine;

namespace zed_0xff.CPS;

public class CPSSettings : ModSettings
{
    public class TSSSettings : IExposable {
        public bool sounds = true;
        public bool effects = true;

        public void ExposeData() {
            Scribe_Values.Look(ref sounds, "sounds", true);
            Scribe_Values.Look(ref effects, "effects", true);
        }
    };

//    public class CommonSettings {
//        public bool debugLog = false;
//
//        public virtual void ExposeData(){
//            Scribe_Values.Look(ref debugLog, "debugLog", false);
//        }
//    }

    public TSSSettings tss = new TSSSettings();

    public override void ExposeData()
    {
        Scribe_Deep.Look(ref tss, "TSS");
        base.ExposeData();
    }
}

public class CPSMod : Mod
{
    public override string SettingsCategory() => "CPS";

    public static CPSSettings Settings { get; private set; }

    public static List<Assembly> plugins = new List<Assembly>();

    public CPSMod(ModContentPack content) : base(content) {
        Settings = GetSettings<CPSSettings>();

        plugins.Clear();
        if (ModLister.HasActiveModWithName("Vanilla Nutrient Paste Expanded")) {
            LoadPlugin(content, "VNPE");
        }
        if (ModLister.HasActiveModWithName("Vanilla Races Expanded - Sanguophage")) {
            LoadPlugin(content, "VRES");
        }
        if (ModLister.HasActiveModWithName("Dubs Bad Hygiene")) {
            LoadPlugin(content, "DBH");
        }
//        if( plugins.Any() ){
//            GenTypes.ClearCache();
//        }
    }

    private void LoadPlugin(ModContentPack content, string name){
        try {
            string fname = Path.Combine(content.RootDir, "Plugins", "CPS_" + name + ".dll");
            byte[] rawAssembly = File.ReadAllBytes(fname);
            Assembly assembly = AppDomain.CurrentDomain.Load(rawAssembly);
            Log.Message("[d] CPS loaded plugin " + assembly);
            content.assemblies.loadedAssemblies.Add(assembly);
            plugins.Add(assembly);
        } catch(Exception ex) {
            Log.Error("[!] CPS: plugin " + name + " failed to load: " + ex);
        }
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

        var tabs = new List<TabRecord> {
            new TabRecord("TSS".Translate(), () => {
                PageIndex = 0;
                WriteSettings();
            }, PageIndex == 0),
        };

        TabDrawer.DrawTabs(tabRect, tabs);

        switch (PageIndex)
        {
            case 0:
                draw_TSSSettings(mainRect.ContractedBy(15f));
                break;
            default:
                break;
        }
    }

    private void draw_TSSSettings(Rect inRect){
        Listing_Standard l = new Listing_Standard();
        var viewRect = new Rect(0f, 0f, inRect.width - 60, 200f); // XXX manual height :(
        Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
        l.Begin(viewRect);

        l.CheckboxLabeled("Sounds", ref Settings.tss.sounds);
        l.CheckboxLabeled("Effects", ref Settings.tss.effects);

        l.End();
        Widgets.EndScrollView();
    }
}
