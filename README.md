<img width="1774" height="887" alt="image" src="https://github.com/user-attachments/assets/4d4b952d-3961-4fe6-a280-2ca88a64e73f" />


In-game mod menu for [SPT](https://sp-tarkov.com/). Built for **SPT 4.1.2** (EFT `0.16.9.5.40743`).

Press **Insert** to open the menu.

> Offline use with SPT only. Do not take this online — BattlEye will ban the account.

---

## Menu

Eleven tabs, each grouped by what it acts on.

### Visuals

Everything that gets drawn. This is the single place that answers "is this shown".

| Group | Contents |
|---|---|
| Players ESP | Boxes, info, skeletons, charms, x-ray, shootable and blocked targets, snap lines |
| Readout | Names, weapon, distance, health value, HP bars — each on its own |
| Chams | Whole-body colouring per role, with a second colour for the parts behind cover, plus opacity and range |
| Loot & World | Loot with container and corpse search, prices, wishlist, containers, extractions, quest markers |
| Other | Grenades, hit markers, crosshair, HUD, radar, map, night and thermal vision, no visor, no flash |
| Filters & Render | Maximum distance, faction colours, line thicknesses, radar range |

ESP is projected through the optic camera while aiming, so it stays aligned inside magnified scopes.

### Aimbot

Hold-to-aim with adjustable distance and smoothness, elevation compensation, and an FOV circle.

- **Silent Aim** — redirects shots and fires on its own, with speed factor and shot delay
- **Magic Bullets** — bends only the shots you fire, never pulls the trigger
- **Wall Shoot** — full penetration, no ricochet or deviation
- **Instant Kill** — any hit you land is lethal, never applied to yourself

### Player

| Group | Contents |
|---|---|
| Survival | God mode with vitals-only and negative-effect options, food and water, unlimited stamina, self heal |
| Movement | Speed boost, no collision, ghost mode, no inertia, silent movement, no fall damage, flight |
| Camera | Free camera with teleport, movement and look speed, FOV changer |

### Weapon

Unlimited ammo, forced full auto with fire rate, recoil reduction by amount, no sway, no malfunctions, no overheating, maximum durability, everything examined, instant research, quick throw, and a handling override for ergonomics and weight.

### World

Air drops, mortar strikes, trains, door and keycard opening, interaction reach, and full time and weather control — hour, clouds, fog, rain, wind and thunder.

### Items

The complete in-game item catalogue: scrollable, searchable by name and filtered by category. Selecting an item shows its details; from there it can be tracked so it is highlighted in the world, or spawned in raid. Tracked items are managed in their own list.

### Bots

Spawn any bot type at a chosen distance and direction, with a report of what actually happened. Live roster of who is alive and how far away. Gather teleports every bot to you.

### Exfils

Extraction points of the running raid with distance and status, each with a teleport.

### Hotspots

Save positions per map and jump back to them. Stored next to `ravenx.ini`, so reinstalling the game does not lose them.

### Misc & Config

Scene dumps for analysis, a readout of what is currently enabled, save and load, and rebindable keys for every feature that has one.

---

## Install

Run the installer against your SPT folder:

```bash
Installer.exe install "E:\Games\SPT"
```

Or copy the built files by hand:

| File | Destination |
|---|---|
| `RavenX.dll` | `EscapeFromTarkov_Data\Managed\` |
| `RavenX.Plugin.dll` | `BepInEx\plugins\` |
| `outline` | `EscapeFromTarkov_Data\` |

`0Harmony.dll` is already provided by BepInEx.

> **Run the game once before installing.** SPT rewrites the game assemblies on first launch, and RavenX has to be compiled against those. Installing on untouched binaries either fails to compile or freezes the game at the startup screen.

---

## Build

Requires the .NET SDK and MSBuild. Point `EFTBasePath` at your SPT installation:

```bash
MSBuild.exe BepInExPlugin/BepInExPlugin.csproj -p:EFTBasePath="E:\Games\SPT" -p:Configuration=Release
```

Clean `bin` and `obj` after upgrading SPT, or stale references will produce type and token errors at startup.

---

## Configuration

Settings live in `Documents\Escape from Tarkov\ravenx.ini` and can be saved and loaded from the Config tab or the in-game console. Every feature is also reachable as a console command, and the menu writes to the same values, so both stay in sync.

---

## License

MIT. See [LICENSE](LICENSE).

---

## Credit

RavenX is maintained by **C1yHAX** — the SPT 4.1.2 port, the Raven interface and everything under that name.
The project was also built using code from sailro https://github.com/sailro/EscapeFromTarkov-Trainer.
