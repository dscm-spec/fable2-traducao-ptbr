# Intermittent crash right after a menu action (guest access violation on the main thread)

**Build:** see `build-info.json` (rexglue 0.10.0.0-dev, built 2026-10-29, win-x64). Windows 10 22H2, `render_thread = true`.
The game is otherwise very playable. Thanks for the work!

## Symptoms

The game crashes at random right after confirming a menu action. Seen 3 times:

| Log | Action | Faulting read |
|---|---|---|
| refii_008 | Real estate → *Adjust Rent* → confirm with **Enter** (1st confirm) | `0x8181818B` |
| refii_011 | Real estate → *Adjust Rent* → confirm with **Enter** (crashed after a few successful confirms) | `0x08000020` |
| refii_012 | Inventory → *use* item | `0x04000000` |

Confirming *Adjust Rent* with the **mouse** went fine in 7 confirms (44 rent screen events, 0 crashes). We are not sure whether that is a real difference or luck. Both paths log the same `ScreenAction:37:root`.

## The common pattern

1. On the guest tick thread, the guest-tick pump runs an injected UI action:
   - Rent: `ScreenAction:37:root`, logged as "*written to Float1 — the guest's own predicate cannot compute it*".
   - Item: `Inventory:use:<id>`, logged as "*no GUI event, no screen*".
2. 5–10 ms later there is an `Unhandled guest access violation` on **another thread**.
3. That thread is always the one that logs `Refii world sprites: N vector entries ... at wim=...`:
   - refii_008: t7048 = crash thread 0x1B88
   - refii_011: t14688 = 0x3960
   - refii_012: t9120 = 0x23A0
   - It is also the thread that parses the config flags at startup, so it looks like the main/Godot thread.
4. The faulting addresses look like garbage: `0x8181818B` looks like a fill pattern, and the other two are round values.

Our guess: this is a race. The main thread walks guest structures (world sprites / HUD vector) while the pump thread is changing them in response to the UI action, so the main thread reads a stale pointer. That would explain why the crash is intermittent and why the faulting address changes every time.

## Also seen (maybe related)

- After finishing a Blacksmith job, the picture froze while audio and simulation kept going (the guest tick counter kept advancing). It recovered by itself after a while. In refii_012, the last `world sprites` line before the freeze shows the vector growing quickly from 3 to 8 entries.

## Attached

- `logs/refii_00X_crash.txt` has the ~40 lines before each crash, with noisy lines (friends/audio/net) filtered out, plus a per-thread count of `world sprites` lines.
- Full logs are available on request.
