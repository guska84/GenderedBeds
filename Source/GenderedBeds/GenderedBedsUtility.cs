using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace GenderedBeds
{
    public static class GenderedBedsUtility
    {
        public static bool Allows(this BedGenderRestriction restriction, Pawn pawn)
        {
            if (restriction == BedGenderRestriction.Any || pawn == null)
                return true;

            switch (pawn.gender)
            {
                case Gender.Male:
                    return restriction == BedGenderRestriction.Male;
                case Gender.Female:
                    return restriction == BedGenderRestriction.Female;
                default:
                    return GenderedBedsMod.Settings?.allowGenderless ?? true;
            }
        }

        public static string Label(this BedGenderRestriction restriction)
        {
            return ("GenderedBeds_Restriction_" + restriction).Translate();
        }

        public static Texture2D Icon(this BedGenderRestriction restriction)
        {
            switch (restriction)
            {
                case BedGenderRestriction.Male:
                    return Gender.Male.GetIcon();
                case BedGenderRestriction.Female:
                    return Gender.Female.GetIcon();
                default:
                    return null;
            }
        }

        public static CompGenderedBed GenderComp(this Thing thing)
        {
            return (thing as ThingWithComps)?.GetComp<CompGenderedBed>();
        }

        public static void UnassignDisallowedOwnersOnAllMaps()
        {
            List<Map> maps = Current.Game?.Maps;
            if (maps == null)
                return;

            foreach (Map map in maps)
            {
                foreach (Building_Bed bed in map.listerBuildings.AllBuildingsColonistOfClass<Building_Bed>().ToList())
                    bed.GenderComp()?.UnassignDisallowedOwners();
            }
        }
    }
}
