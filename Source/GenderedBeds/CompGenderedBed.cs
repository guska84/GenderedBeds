using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace GenderedBeds
{
    public class CompGenderedBed : ThingComp
    {
        private const float OverlayIconSize = 16f;

        private static int lastMenuFrame = -1;

        private BedGenderRestriction restriction = BedGenderRestriction.Any;

        public BedGenderRestriction Restriction => restriction;

        public Building_Bed Bed => parent as Building_Bed;

        /// <summary>True when the restriction is set and currently in force.</summary>
        public bool RestrictionActive
        {
            get
            {
                if (restriction == BedGenderRestriction.Any)
                    return false;
                Building_Bed bed = Bed;
                if (bed != null && bed.Medical && !(GenderedBedsMod.Settings?.medicalRespectsRestriction ?? false))
                    return false;
                return true;
            }
        }

        public bool AllowsPawn(Pawn pawn)
        {
            return !RestrictionActive || restriction.Allows(pawn);
        }

        public void SetRestriction(BedGenderRestriction value)
        {
            if (restriction == value)
                return;
            restriction = value;
            UnassignDisallowedOwners();
        }

        public void UnassignDisallowedOwners()
        {
            CompAssignableToPawn assignable = Bed?.CompAssignableToPawn;
            if (assignable == null || !RestrictionActive)
                return;

            List<Pawn> removed = assignable.AssignedPawnsForReading.Where(p => !restriction.Allows(p)).ToList();
            if (removed.Count == 0)
                return;

            foreach (Pawn pawn in removed)
                assignable.TryUnassignPawn(pawn);

            if (parent.Faction == Faction.OfPlayer)
            {
                Messages.Message(
                    "GenderedBeds_OwnersUnassigned".Translate(parent.LabelShort, removed.Select(p => p.LabelShort).ToCommaList()),
                    parent, MessageTypeDefOf.NeutralEvent, historical: false);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref restriction, "genderedBedsRestriction", BedGenderRestriction.Any);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
                yield return gizmo;

            if (parent.Faction != Faction.OfPlayer)
                yield break;

            yield return new Command_Action
            {
                defaultLabel = "GenderedBeds_GizmoLabel".Translate(restriction.Label()),
                defaultDesc = "GenderedBeds_GizmoDesc".Translate(),
                icon = restriction.Icon() ?? parent.def.uiIcon,
                action = OpenRestrictionMenu,
            };
        }

        private static void OpenRestrictionMenu()
        {
            // Identical gizmos on several selected beds are merged and may each receive
            // the click; open just one menu per frame.
            if (lastMenuFrame == RealTime.frameCount)
                return;
            lastMenuFrame = RealTime.frameCount;

            var options = new List<FloatMenuOption>();
            foreach (BedGenderRestriction value in new[] { BedGenderRestriction.Any, BedGenderRestriction.Male, BedGenderRestriction.Female })
            {
                BedGenderRestriction captured = value;
                options.Add(new FloatMenuOption(value.Label(), () => ApplyToSelected(captured), value.Icon() ?? BaseContent.ClearTex, Color.white));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void ApplyToSelected(BedGenderRestriction value)
        {
            foreach (object selected in Find.Selector.SelectedObjectsListForReading.ToList())
            {
                if (selected is Thing thing && thing.Faction == Faction.OfPlayer)
                    thing.GenderComp()?.SetRestriction(value);
            }
        }

        public override string CompInspectStringExtra()
        {
            if (restriction == BedGenderRestriction.Any)
                return null;

            string text = "GenderedBeds_InspectRestriction".Translate(restriction.Label());
            if (!RestrictionActive)
                text += " " + "GenderedBeds_InspectMedicalIgnored".Translate();
            return text;
        }

        public override void DrawGUIOverlay()
        {
            base.DrawGUIOverlay();

            if (restriction == BedGenderRestriction.Any || !(GenderedBedsMod.Settings?.showOverlay ?? true))
                return;
            if (parent.Faction != Faction.OfPlayer || Find.CameraDriver.CurrentZoom > CameraZoomRange.Close)
                return;

            Texture2D icon = restriction.Icon();
            if (icon == null)
                return;

            Vector2 pos = parent.TrueCenter().MapToUIPosition();
            var rect = new Rect(pos.x - OverlayIconSize / 2f, pos.y - OverlayIconSize / 2f, OverlayIconSize, OverlayIconSize);
            GUI.color = RestrictionActive ? Color.white : new Color(1f, 1f, 1f, 0.4f);
            GUI.DrawTexture(rect, icon);
            GUI.color = Color.white;
        }
    }
}
