> **Status:** Evergreen (standing contract, adopted by reference). **Last reviewed:** 2026-10-11.

# Working requirements & methodology — Shroom and Gloom accessibility

This project adopts, **in full and by reference**, the tree-wide standards. Explicitly open and read both at every session start; a link does not prove they were loaded:

- [The shared working contract](../../WORKING_CONTRACT.md): project selection, pickup, learning-repo reuse, Git boundaries, credentials, close-out.
- [The canonical working requirements](../../templates/working-requirements.template.md): accessibility baseline, session cadence, retro, risks, decisions, documentation standards.

They are not copied here, so they cannot drift. This file records only what is specific to this project.

## The standing contract, in one breath

WCAG 2.2 AA (in spirit, for this non-HTML game surface), screen-reader-first (NVDA), keyboard-operable, for the product and the session. Scoped priorities at session start (an explicit request can set scope). At close: changelog, dated session record with the four-part retro and risk analysis, STATE edited in place, WCAG-spirit audit of changed surfaces, lessons and decisions updated. Report honestly and hand back anything that can't be verified from here. Spike the killer assumption first and honour phase gates. Build only what was asked for.

## Project-specific obligations

- **Claude cannot hear the product.** Every audible claim stays "unverified by ear" until the maintainer confirms it. Each session record keeps separate lists for confirmed by ear, verified by data, and unverified.
- **The game is a moving target.** It is in Early Access; check the installed build against STATE at every session start (see [CLAUDE.md](../CLAUDE.md)).
- **Nothing of the game's is redistributed.** Dumps and decompiled output stay in the gitignored `reference/` folder. The shipped mod contains only our code, Prism and its licence files.
- **Prism is the speech layer from the first line of code.** No Tolk path is built unless a recorded decision adds one.
- **Installing a loader into the game folder is a phase gate.** It changes the Steam install and runs third-party code inside the game, so it needs the maintainer's go-ahead each time a new loader or major version is introduced.
- **Play as sighted players do.** No cheats or rule changes unless a mechanic cannot be made operable any other way, and then only by recorded decision.

## Things only the maintainer can verify (hand back, never assume)

- Anything audible: that speech is heard, its order, its pacing, and whether it is cut off.
- That a key press reaches the game while a screen reader is running.
- Whether an announcement is too long, too short or in the wrong order to play by.

## See also

- [PROJECT_PLAN.md](../PROJECT_PLAN.md), [STATE.md](STATE.md), [lessons-and-dead-ends.md](lessons-and-dead-ends.md), [open-questions.md](open-questions.md).
