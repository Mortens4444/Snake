# Snake Reloaded

A modern take on classic Snake: a shared C#/.NET engine (`Snake.Core`) driving a console client
(`Snake`) and a .NET MAUI client (`Snake.Maui`), with LAN co-op, a roguelike perk system, smart
enemy AI, and daily challenges layered on top of the original game.

See `ROADMAP.md` for the phased build history (in Hungarian) and `ToDo.txt` for the original
feature wishlist this project grew from.

## Architecture

```text
┌────────────────────────────────────────────────────────┐
│                       Snake.Core                        │
│   Engine, grid, entities, AI, perks, save system,       │
│   settings, background model, color math, multiplayer   │
│   protocol - platform-agnostic, no console/UI calls.    │
└───────────────────────────┬────────────────────────────┘
                            │
             ┌──────────────┴──────────────┐
             ▼                             ▼
┌─────────────────────────┐   ┌──────────────────────────┐
│ Console Client (Snake)  │   │ Snake.Maui Client        │
│ ANSI/ASCII rendering    │   │ GraphicsView (Canvas)     │
│ Keyboard input          │   │ Swipe / virtual D-Pad     │
│ Console.Beep() sound    │   │ Haptic feedback           │
│ LAN host/join (TCP)     │   │ Bluetooth host/join (BLE) │
└─────────────────────────┘   └──────────────────────────┘
```

## Gameplay

- Configurable map size, snake speed (with per-body-part speed-up), and points required per
  level-up and to win (`Settings`, in-game Settings menu).
- Visible border wall; snakes die on colliding with a wall, themselves, another snake, or (for
  enemies) getting fully boxed in - their corpse turns into food.
- Enemy snakes eat and grow too.
- Six selectable AI difficulty levels, each a distinct `ISnakeBrain`:

  | Level | Behavior |
  | --- | --- |
  | Random | Turns randomly |
  | Easy | Beelines for food (BFS/A*) |
  | Normal | Also avoids one-step collisions |
  | Hard | Sets traps by cutting off your path |
  | Expert | Multi-step lookahead, flood-fill area scoring, dead-end avoidance |
  | Nightmare | Enemies coordinate with each other to box you in |

- Five named, persistent enemy personalities (Viper, Ghost, Titan, Fang, Hydra), each with its own
  color, AI tuning, and favorite perk.

## Visuals

- A fresh background is generated per game (grass, lakes, trees, bushes).
- Subtle ambient animation: water shimmers, leaves rustle, tree canopies pulse.
- Animated rainbow-color title screen; the menu's snake art gently slithers side to side.
- *Not implemented, by design*: drifting decorative clouds and ambient (non-gameplay) birds were
  deliberately skipped - the one bird already in the game is the gameplay mechanic below.

## Birds (golden-cookie mechanic)

A blinking, chirping bird occasionally flies across the map (frequency configurable, 0 = never).
Catching it with your head opens an instant bonus perk-choice card, outside the normal level-up
flow - a risk/reward chase, since crashing while chasing it is easy.

## Perks

All 15 planned perks are implemented (`Snake.Core/Perks`): Iron Head, Metabolism, Double Harvest,
Handbrake, Berserk, Spiky Tail, Poison Trail, Time Warp, EMP, Apple Magnet, Ghost Phase, Tail Whip,
Amphibious, Woodpecker, Chameleon.

Leveling up (or catching the bird) opens a roguelike choice card offering N random perks you don't
already own (N configurable); the card shows each perk's activation key. Active perks' keys and
cooldowns are shown live in the status bar. Enemy snakes collect and keep perks too - survivors
carry theirs into the next game, a death wipes them.

Lucky Food variants: red (+1), gold (+3), purple (perk choice), blue (shield charge), rainbow
(random bonus effect).

## Multiplayer

Host-authoritative co-op for two players (`Snake.Core/Multiplayer`: `NetworkProtocol`, `Messages`,
`SnapshotBuilder`). The host runs the full simulation (AI, perks, obstacles) and streams a compact
per-tick snapshot to the guest; the guest only sends steering input and renders what it's told, so
there's no lockstep desync risk. Two transports share this exact protocol unchanged:

- **LAN (console client)**: TCP, `LanHost`/`LanClient`, `Snake/MultiplayerEngine.cs`.
- **Bluetooth LE (MAUI client)**: `BluetoothHost`/`BluetoothClient` (`Snake.Maui/Multiplayer`),
  wrapping a chunked BLE GATT link (Shiny.BluetoothLE/.Hosting, Nordic UART Service UUIDs) in a
  `Stream` adapter so the exact same `NetworkProtocol` framing code runs over it unmodified.
  **Compile-verified for Android/iOS/MacCatalyst/Windows only - not yet tested on real Bluetooth
  hardware**, since this environment has no Bluetooth radio or paired devices to test against.
  Expect to find and fix real-device bugs (pairing, GATT timing, throughput) on first use; BLE's
  low throughput in particular may require sending snapshots less often or trimming their size if
  the current per-tick full-entity payload can't keep up on real hardware.

Both the host and the guest can level up, choose perks, and catch the bird independently on either
transport - the snake currently choosing a perk freezes for that slice of the tick (no
move/eat/collide), while everything else in the world (the other snake, enemies, food, the bird)
keeps running in real time. Four perks (Amphibious, Berserk, Handbrake, Chameleon) are host-only,
since they hook into mechanisms (the shared tick cadence, hunter-AI targeting) the guest doesn't
have a stake in yet.

## Progression, stats & leaderboard

- Perks persist between single-player games (`playerprogress.json`); optionally lost on death
  (Settings toggle), with a full-reset option.
- Snake length also persists between games the same way, growing your starting length game to
  game - separately toggleable ("Lose snake length on death", default on so nobody's starting
  length changes unless they opt out of the reset).
- Enemy career stats persist in `profiles.json`: deaths, survivals, wins (survived a round the
  player did *not*), kills against the player, and currently-held perks.
- The Leaderboard screen shows top player scores, plus a two-row-per-enemy breakdown (stats row,
  then an active-perks row when it has any).
- An end-of-game ranking screen lists every snake (player + enemies, dead or alive) by survival
  and length.

## Cheats & Easter eggs

Typed while playing (toggle in Settings): `god` (permanent Ghost Phase), `grow` (instant level-up),
`shrink` (reset to length 3, keep score), `perk` (open a perk choice), `spawnbird` (force a bird).

Separately, purely cosmetic easter eggs (own Settings toggle, since they grant no gameplay
advantage): typing `ghost` toggles a permanent pale ghost-colored skin, typing `rainbow` toggles a
faster whole-body color cycle - both console-only, like the cheats, since they're triggered by
typing a secret word during play and the MAUI client has no keyboard/typed input.

## Screenshots & replay

- F12 (PrtScn is often swallowed by the console) saves the current game state to a JSON file,
  including an embedded ASCII-art render of the field.
- On death, the last ~10 seconds can be replayed in slow motion (press R on the Game Over screen).

## Daily challenges

Three challenges a day, seeded by date so everyone gets the same set: survive 5 minutes, wall-crash
3 enemy snakes, eat 100 food, win without using an active perk. Progress persists across sessions
(`dailychallenge.json`); newly-completed challenges are called out on the post-game screen.

## Bug reporting

An unhandled exception opens your default email client with a pre-filled bug report (subject,
truncated stack trace) instead of just crashing silently.

## Not implemented / out of scope

- **MAUI Bluetooth multiplayer real-hardware verification** - implemented and compile-verified for
  every MAUI target framework, but not yet run on an actual paired device (see Multiplayer above).
- **Decorative clouds / ambient background birds** - a conscious style decision, not a gap.
- **"Keyboard combos"** - `ToDo.txt` lists this as its own, otherwise-unspecified item; the cheat
  codes and perk activation keys above are the only keyboard-shortcut systems actually built, and
  no separate "combo" mechanic (e.g. simultaneous key chords) exists beyond those.
- **Automated YouTube upload** - non-code task; recording/uploading by hand needs no API key, only
  automated upload would need a YouTube Data API v3 key.
