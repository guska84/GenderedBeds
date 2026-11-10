# Gendered Beds: Plan

A small RimWorld mod that lets you restrict any humanlike bed to **Male only** or **Female only**, so you can set up male and female dorms. Every bed starts as **Any**, so nothing changes until you choose a setting.

- **Targets:** RimWorld 1.7 (the main target) and 1.6.
- **Dependencies:** Harmony only. No DLC is needed.

---

## 1. What the research found

### RimWorld 1.7 is a big platform change

- 1.7 was announced on 12 Oct 2026 and is currently **only on Steam's `unstable` branch**. 1.6 is still the public release.
- 1.7 no longer runs on Unity/Mono. It runs on Ludeon's own **Waylith** engine and **.NET 10**. A shim (`UnityStubs`, `Waylith.UnityCompat`) keeps the old `UnityEngine` API working, and a `RimWorld.dll` sits next to `Assembly-CSharp.dll`.
- Ludeon says about 99% of 1.6 mods work in 1.7 without changes. **Harmony mods are the exception:** 1.7 needs **Harmony 3.0**. Harmony 2.x cannot patch on 1.7 at all (it throws `PlatformNotSupportedException`). Harmony 3 also checks the types of `__instance` and `__result` strictly.
- 1.7 mod assemblies should target **`net10.0`**. 1.6 assemblies stay on `net472`.

### Checked against the game's reference assemblies

I downloaded `Krafs.Rimworld.Ref` 1.6.4871 and 1.7.5000-unstable and decompiled the types this mod touches: `Building_Bed`, `RestUtility`, `CompAssignableToPawn`, `CompAssignableToPawn_Bed`, `Pawn_Ownership`, `Dialog_AssignBuildingOwner` and `BedUtility`.

- **Their public and private signatures are identical in 1.6 and 1.7**, so the same C# source works for both versions.
- The hook points are:
  - `RestUtility.IsValidBedFor(Thing bedThing, Pawn sleeper, Pawn traveler, bool checkSocialProperness, bool allowMedBedEvenIfSetToNoCare, bool ignoreOtherReservations, GuestStatus? guestStatus)`. AI bed searches go through this, including `FindBedFor`, rescuing and taking prisoners or patients to bed.
  - `CompAssignableToPawn_Bed.CanAssignTo(Pawn) : AcceptanceReport` (virtual). The "Assign owner" dialog uses this, and it can show a reason for each pawn it rejects.
  - `CompAssignableToPawn_Bed.AssigningCandidates` (virtual). This lists the pawns shown in that dialog.
- Reference assemblies contain only signatures, not method bodies. So I could confirm that these methods exist, but **not every internal call path** (for example, whether `CanUseBedNow` goes through `IsValidBedFor`). The in-game test checklist in section 6 covers these gaps.

### I built a test project for both versions

I built a throwaway project that compiles **one source file** for both versions:

| Version | Target framework | Packages |
|---|---|---|
| 1.6 | `net472` | `Krafs.Rimworld.Ref 1.6.*`, `Lib.Harmony 2.4.x` |
| 1.7 | `net10.0` | `Krafs.Rimworld.Ref 1.7.*-*`, `Lib.Harmony 3.0.0-rc.5` |

The output goes to `1.6/Assemblies` and `1.7/Assemblies`. It included a ThingComp, a postfix on `IsValidBedFor` and a postfix on `CanAssignTo`, and **both versions built cleanly** on Linux with the .NET 10 SDK. The game does not need to be installed to build.

### Is someone else already doing this?

A quick search found no existing mod that restricts beds by gender. The closest are Ideology's precept-based bed rules (`IdeoligionForbids`) and various dorm or polyamory mods.

---

## 2. Design

### Data: one ThingComp per bed
- `CompGenderedBed` stores a `BedGenderRestriction { Any, Male, Female }` value and saves it with `Scribe_Values`. The default is `Any`.
- The comp is **added in code at startup** (`[StaticConstructorOnStartup]`) to every `ThingDef` where `IsBed && building.bed_humanlike`. I'm doing this instead of with an XML patch because it also covers beds from other mods and beds that inherit `thingClass` from an abstract parent, without any XPath. Animal beds are left out.
- The setting is kept when a bed is minified (uninstalled) and reinstalled, because comps save with the thing.

### Enforcement (Harmony postfixes, no transpilers)
1. **`RestUtility.IsValidBedFor`, postfix:** if the result is `true` and the bed's restriction doesn't allow the sleeper, set it to `false`. This stops pawns from auto-claiming, sleeping in or being carried to a bed they aren't allowed in.
2. **`CompAssignableToPawn_Bed.CanAssignTo`, postfix:** reject pawns who don't match, with a reason such as "Bed is set to female only". The assign dialog then shows them greyed out with that reason.
3. **Changing the restriction:** current owners who no longer match are unassigned with `TryUnassignPawn`, and a message is shown. Pawns who are asleep in the bed are left alone. Once unassigned, the AI won't choose that bed again.
4. Postfixes only, kept as simple as possible, so they get along with other bed mods and with Harmony 3's stricter checks.

### Who is allowed
- `Any`: everyone, the same as vanilla.
- `Male` / `Female`: only pawns with that `Pawn.gender`.
- **Pawns with `Gender.None`** (some modded races, androids and so on): they are allowed in gendered beds by default, with a mod setting to change this (see question 2).
- **Medical beds:** by default the restriction **does not apply** while a bed is set to Medical, because a gendered sick bay shouldn't let someone die untreated. A mod setting can change this.
- Restrictions apply equally to colonist, prisoner and slave beds, which gives you gendered cells too.

### UI
- **Gizmo** on selected beds: one button that shows the current state and opens a small menu with Any, Male only and Female only. Choosing an option **applies it to every selected bed**, so you can box-select a whole dorm and set it in one click.
- **Icons** reuse the game's own gender icons (`GenderUtility.GetIcon`), so the mod ships no custom art.
- **Inspect pane** gets an extra line on restricted beds, such as "Restricted to: Female".
- **Optional overlay:** a small gender icon drawn over restricted beds, which can be turned off in settings.
- All text goes in `Languages/English/Keyed` so the mod can be translated.

### Mod settings (`Mod` + `ModSettings`)
- Medical beds ignore the restriction (default **on**).
- Genderless pawns may use gendered beds (default **on**).
- Show gender icon overlay on beds (default **on**).

---

## 3. Mod and repo layout

```
About/
  About.xml            packageId, supportedVersions 1.6 & 1.7, modDependencies: brrainz.harmony
  Preview.png          (placeholder; replace with a real screenshot later)
loadFolders.xml        v1.6 -> /, Common, 1.6 ; v1.7 -> /, Common, 1.7
Common/
  Languages/English/Keyed/GenderedBeds.xml
1.6/Assemblies/GenderedBeds.dll    (net472, built against Harmony 2.x)
1.7/Assemblies/GenderedBeds.dll    (net10.0, built against Harmony 3.x)
Source/GenderedBeds/
  GenderedBeds.csproj   multi-target net472;net10.0, outputs to the folders above
  GenderedBedsMod.cs    Mod + settings window, Harmony bootstrap
  BedGenderRestriction.cs
  CompGenderedBed.cs    data, gizmo, inspect string, overlay
  CompInjector.cs       [StaticConstructorOnStartup] adds the comp to bed defs
  Patches.cs            the two postfixes
.github/workflows/build.yml   CI: build both DLLs on every push
README.md
```

- **About.xml dependency:** `brrainz.harmony` (Steam Workshop id 2009463077), plus `loadAfter` set to the same. The Harmony mod ships Harmony 2.x for 1.6 and Harmony 3.x for 1.7, so each of our DLLs runs against the right one.
- **Harmony reference:** `Lib.Harmony` is referenced with `ExcludeAssets="runtime"`, so we **never ship `0Harmony.dll`** ourselves.
- **Committed DLLs:** should the built DLLs be committed so the repo folder can be dropped straight into `Mods/`? See question 5.

---

## 4. Out of scope for v1 (possible later additions)
- Room-level "male dorm / female dorm" roles. Per-bed settings with multi-select cover the same need more simply.
- Support for the Multiplayer mod. Its gizmo actions would need `[SyncMethod]` registration, which means a soft dependency through reflection. This could be added later without a hard dependency.
- Hospitality guest beds. These probably work automatically through `IsValidBedFor`, but haven't been tested.
- Restricting by ideology role or xenotype.

---

## 5. Risks
- **1.7 is still unstable.** Ludeon may still change the API, and the Harmony 3 package is a release candidate (rc.5). Rebuilding against newer references is a one-line version change.
- **Call paths I couldn't see:** because there were no method bodies, some code paths that hand out a bed might skip `IsValidBedFor`, for example a player-forced "Sleep here" order. If testing finds one, the fix is one more small postfix (likely candidates are `RestUtility.CanUseBedNow` and `Pawn_Ownership.ClaimBedIfNonMedical`).
- **Removing the mod mid-save:** the saved comp value should be ignored when the save loads. I expect this to be safe, but it needs checking.

---

## 6. In-game test checklist (needs a real install)
1. Set a bed to Female only, then check that male colonists don't auto-claim it and that the assign dialog greys them out with a reason.
2. Restrict a bed that a man currently owns, then check that he is unassigned and finds another bed.
3. Multi-select 10 beds and set them all in one click.
4. Down a pawn, then rescue them and capture them as a prisoner: check they are only taken to allowed beds, and that medical beds are exempt by default.
5. Use a player-forced "sleep here" order on a bed the pawn isn't allowed in.
6. Save and reload, then minify and reinstall the bed: the setting should be kept.
7. Do all of the above on **both 1.6 and 1.7 unstable**.
8. Remove the mod from a save that uses it and load the save.

---

## 7. Decisions (agreed in review)
1. Harmony is an accepted dependency.
2. Genderless pawns: a checkbox in Mod Options, **allowed by default**. A later optional RJW / Sized Apparel integration that checks genital type is out of scope for v1.
3. Medical beds ignore the restriction by default, with a checkbox to make them respect it.
4. Per-bed setting with multi-select. There's no room-wide button, so you can, for example, set different sides of one room.
5. Built DLLs are committed so the repo can be dropped straight into `Mods/`.
6. Name: **Gendered Beds**. Author: **guska**. packageId: **`guska.GenderedBeds`**.
