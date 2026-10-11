> **Status:** Living reference (assistant → maintainer questions). **Last updated:** 2026-10-11, Session 01.

# Open questions — Shroom and Gloom accessibility

## Open

Nothing open.

## Answered

### Q-01 — May a mod loader be installed into the game folder for Spike 1? — answered 2026-10-11 (Session 01)

Yes. "Go ahead and build whatever you want... any tests you need to do are fine." One condition: at the end of development, reset the game's values so the maintainer can play through from the start as a new player. A copy of the save folder as it was after the first launch is kept in `backups/2026-10-11-fresh-install-SaveData/` for that purpose.

In practice the session's safety check does not let Claude download a loader and unpack it into the game folder, so that one step is run by the maintainer from `paste-commands.ps1`.

### Q-02 — Who will play-test by ear, and with which screen reader? — answered 2026-10-11 (Session 01)

The maintainer, with NVDA. The mod will be distributed to the audio games community, so Prism must stay active and other screen readers such as JAWS must be supported.

### Q-03 — Have you played far enough to say how exploration moves? — answered 2026-10-11 (Session 01)

No: "I know literally nothing about the game itself." Spike 2 finds out, and the mod has to teach the game's rules as it goes, since the player cannot learn them from the screen.
