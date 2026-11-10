using HarmonyLib;
using RimWorld;
using Verse;

namespace GenderedBeds
{
    /// <summary>
    /// Every AI bed search goes through this check: finding a bed to sleep in or
    /// claim, rescuing or capturing a pawn, and taking patients or prisoners to bed.
    /// </summary>
    [HarmonyPatch(typeof(RestUtility), nameof(RestUtility.IsValidBedFor))]
    public static class Patch_RestUtility_IsValidBedFor
    {
        public static void Postfix(Thing bedThing, Pawn sleeper, ref bool __result)
        {
            if (!__result || sleeper == null)
                return;
            CompGenderedBed comp = bedThing.GenderComp();
            if (comp != null && !comp.AllowsPawn(sleeper))
                __result = false;
        }
    }

    /// <summary>The "Assign owner" dialog: greys out pawns who don't match, with a reason.</summary>
    [HarmonyPatch(typeof(CompAssignableToPawn_Bed), nameof(CompAssignableToPawn_Bed.CanAssignTo))]
    public static class Patch_CompAssignableToPawn_Bed_CanAssignTo
    {
        public static void Postfix(CompAssignableToPawn_Bed __instance, Pawn pawn, ref AcceptanceReport __result)
        {
            if (!__result.Accepted || pawn == null)
                return;
            CompGenderedBed comp = __instance.parent.GenderComp();
            if (comp != null && !comp.AllowsPawn(pawn))
                __result = new AcceptanceReport("GenderedBeds_CannotAssign".Translate(comp.Restriction.Label()));
        }
    }
}
