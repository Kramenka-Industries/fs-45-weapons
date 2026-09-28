# FS-45 Meridian Loadout

A BepInEx 5 plugin that offers a **hand-curated** Meridian Works / Aryx Weaponry
Expansion loadout on the **FS-45 Specter**.

The FS-45 Specter is a Blueprinter asset bundle. Meridian Works and the Aryx
Weaponry Expansion place their stores with Blueprinter ops that only name
specific stock airframes, so the FS-45 never receives them. This plugin adds the
named mounts to the aircraft's internal bay sets at runtime, using the exact
mount jsonKeys configured.

Because the options land in each hardpoint set's `weaponOptions`, Meridian's
already-installed AI substitution also arms AI FS-45s with these stores.

## Load guard

The plugin stays completely inert unless **both** Meridian Works and the FS-45
Specter bundle are present under `BepInEx/plugins/`. If either is missing it
logs one warning and installs no hooks. The Aryx Weaponry Expansion is optional:
without it, `Aryx_*` keys are skipped with a log line while Meridian keys still
load.

## Default curated loadout

`Internal Weapon Bays` (4.1 m), one round per slot unless a rack is named:

| Mount key | Round |
|---|---|
| `MeridianBlackArrow_internal` | Black Arrow |
| `MeridianBlackArrow_internalx2` | 2x Black Arrow |
| `MeridianAGM84_internal` | AGM-84 |
| `MeridianGBP500_internal` | GBP-500 Bodkin |
| `MeridianScreamer_internal` | AAM-90 Estoc |
| `MeridianARAD200_internal` | ARAD-120 (needs Meridian Works 1.2.0) |
| `MeridianSRM8_internal` | SRM-8 Kukri |
| `bomb_cluster1_single_internal` | CBO-400 (stock cluster bomb) |
| `Aryx_FastAGM1_Internal_x2` | 2x AGM-27 Pyre |
| `Aryx_Wep_ARM2_single_internal` | ARAD-45 |
| `Aryx_GlideBomb400_Single_Internal` | PGM-400LR |

`IRM Bays` (1.5 m):

| Mount key | Round |
|---|---|
| `MeridianSRM8_internal` | SRM-8 Kukri |
| `Aryx_SRIRM1_Single_Internal` | IRM-S5 Gladius |

The `Tail Hook` set is ignored. A round whose `MissileDefinition.length` exceeds
its bay length is skipped (and logged) when `EnforceBayLength` is on.

## Configuration (`ki.fs45meridian.cfg`)

```
[Bays]
InternalBayName   = Internal Weapon Bays
IrmBayName        = IRM Bays
InternalBayLength = 4.1
IrmBayLength      = 1.5
EnforceBayLength  = true
IgnoredSetNames   = Tail Hook

[Loadout]
InternalWeapons = MeridianBlackArrow_internal, MeridianBlackArrow_internalx2, ...
IrmWeapons      = MeridianSRM8_internal, Aryx_SRIRM1_Single_Internal

[Sources]
EnableMeridian = true
EnableAryx     = true

[Diagnostics]
LogHardpoints = true
```

Edit `InternalWeapons` / `IrmWeapons` to change the loadout without recompiling.
Keys beginning with `Aryx` are gated by `EnableAryx`; keys beginning with
`Meridian` by `EnableMeridian`. Any other key (e.g. stock mounts) is always
attempted.

## Build and install

Run `just build` or `just install`. The latter copies `bin/Fs45Meridian.dll`
into the game's `BepInEx/plugins/` directory. Set
`NUCLEAR_OPTION_GAME='/path/to/Nuclear Option'` for a nondefault installation
(defaults to `/var/nuclearoption`). Restart the game afterwards.

## Notes

- Only existing mount keys are used, so no new network definitions are created.
  Clients still need the same weapon packs, the FS-45 Specter and this plugin
  for these loadouts to resolve in multiplayer.
- The Meridian Works DLL must be **1.2.0** for `MeridianARAD200_internal`
  (ARAD-120); older versions do not contain that asset and it will be skipped.
