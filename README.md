# Gendered Beds

A RimWorld mod that lets you restrict any humanlike bed to **male only** or **female only**, so you can set up male and female dorms, barracks or prison cells.

- **Supports:** RimWorld **1.7** and **1.6**
- **Requires:** [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077). No DLC is needed.
- **packageId:** `guska.GenderedBeds`

## Using it
Select one or more beds and click **Gender: …**. Choose *Any gender*, *Male only* or *Female only*. The choice applies to every selected bed, so you can box-select a whole room, or just one side of it.

Every bed starts as *Any gender*, so nothing changes until you set a restriction.

A restricted bed:
- won't be claimed by, or assigned to, pawns of the wrong gender. The "Assign owner" dialog greys them out and shows why.
- won't be used when rescuing, capturing or carrying a pawn to bed.
- unassigns any current owners who no longer match.
- shows its restriction in the inspect pane. It also shows a small gender icon on the bed when you're zoomed in (this can be turned off).

### Mod options
| Option | Default | Effect |
|---|---|---|
| Genderless pawns may use gendered beds | On | If off, pawns with no gender can only use *Any gender* beds. |
| Medical beds respect the restriction | Off | By default a bed's restriction is ignored while the bed is set to medical, so patients are never left untreated. |
| Show gender icon on restricted beds | On | Turns the map icon on or off. |

You can add the mod to an existing save.

## Installing
Copy or clone this repository into your `RimWorld/Mods/` folder. The built DLLs are committed, so it works as-is.

`loadFolders.xml` picks the right assembly for your game version:
- `1.6/Assemblies`: built for .NET Framework 4.7.2 against Harmony 2.x
- `1.7/Assemblies`: built for .NET 10 against Harmony 3.x

## Building
You need the [.NET 10 SDK](https://dotnet.microsoft.com/download). The game doesn't need to be installed, because game references come from [`Krafs.Rimworld.Ref`](https://github.com/krafs/RimRef).

```bash
dotnet build Source/GenderedBeds/GenderedBeds.csproj -c Release
```

This one command builds both `1.6/Assemblies/GenderedBeds.dll` and `1.7/Assemblies/GenderedBeds.dll`. Commit the rebuilt DLLs along with any source changes.

## How it works
- A `ThingComp` (`CompGenderedBed`) is added in code at startup to every def with `IsBed && building.bed_humanlike`, so beds from other mods are covered too. The comp holds the setting and provides the gizmo, inspect text and icon overlay.
- It uses two Harmony **postfixes**, and no transpilers:
  - `RestUtility.IsValidBedFor`: all AI bed searches go through this check.
  - `CompAssignableToPawn_Bed.CanAssignTo`: this is what the assign-owner dialog uses.

## Possible later additions
- A soft integration with RJW / Sized Apparel that decides genderless pawns by genital type.
- Sync support for the Multiplayer mod.
