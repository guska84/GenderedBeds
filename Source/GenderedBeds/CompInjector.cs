using System;
using System.Collections.Generic;
using Verse;

namespace GenderedBeds
{
    /// <summary>
    /// Adds <see cref="CompGenderedBed"/> to every humanlike bed def once all defs are loaded.
    /// Done in code rather than with an XML patch so it also covers modded beds and beds
    /// that inherit their thingClass from an abstract parent.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class CompInjector
    {
        static CompInjector()
        {
            int count = 0;
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                try
                {
                    if (!def.IsBed || def.building == null || !def.building.bed_humanlike)
                        continue;
                    if (def.thingClass == null || !typeof(ThingWithComps).IsAssignableFrom(def.thingClass))
                        continue;
                    if (def.comps == null)
                        def.comps = new List<CompProperties>();
                    if (def.HasComp(typeof(CompGenderedBed)))
                        continue;

                    def.comps.Add(new CompProperties(typeof(CompGenderedBed)));
                    count++;
                }
                catch (Exception e)
                {
                    Log.Error($"[Gendered Beds] Could not add gender restriction to {def.defName}: {e}");
                }
            }

            if (Prefs.DevMode)
                Log.Message($"[Gendered Beds] Added gender restriction to {count} bed types.");
        }
    }
}
