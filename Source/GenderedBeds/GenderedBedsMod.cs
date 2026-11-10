using HarmonyLib;
using UnityEngine;
using Verse;

namespace GenderedBeds
{
    public class GenderedBedsSettings : ModSettings
    {
        public bool allowGenderless = true;
        public bool medicalRespectsRestriction = false;
        public bool showOverlay = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref allowGenderless, "allowGenderless", true);
            Scribe_Values.Look(ref medicalRespectsRestriction, "medicalRespectsRestriction", false);
            Scribe_Values.Look(ref showOverlay, "showOverlay", true);
        }
    }

    public class GenderedBedsMod : Mod
    {
        public const string HarmonyId = "guska.GenderedBeds";

        public static GenderedBedsSettings Settings { get; private set; }

        public GenderedBedsMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<GenderedBedsSettings>();
            new Harmony(HarmonyId).PatchAll(typeof(GenderedBedsMod).Assembly);
        }

        public override string SettingsCategory() => "GenderedBeds_SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);
            listing.CheckboxLabeled("GenderedBeds_Setting_AllowGenderless".Translate(), ref Settings.allowGenderless,
                "GenderedBeds_Setting_AllowGenderless_Desc".Translate());
            listing.CheckboxLabeled("GenderedBeds_Setting_MedicalRespects".Translate(), ref Settings.medicalRespectsRestriction,
                "GenderedBeds_Setting_MedicalRespects_Desc".Translate());
            listing.CheckboxLabeled("GenderedBeds_Setting_ShowOverlay".Translate(), ref Settings.showOverlay,
                "GenderedBeds_Setting_ShowOverlay_Desc".Translate());
            listing.End();
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            // A setting such as "genderless pawns may use gendered beds" may have
            // just been turned off; unassign any owners that no longer qualify.
            GenderedBedsUtility.UnassignDisallowedOwnersOnAllMaps();
        }
    }
}
