# Rank redesign - 28 September 2026

`rank-final.png` shows the redesigned leaderboard and the new Statistics navigation glyph. All leaderboard names/scores in these captures are local fixtures, never submitted. The navigation icon is a three-bar Painter2D shape using the inherited text colour; the previous cropped framed sprite and its temporary Sprite allocation were removed.

Explicit Completion time / Tasks completed and Top players / Around me controls replace Show Top / Seasonal checkbox toggles. Rank, Player and Time/Tasks headers align with the table. Usernames are bold and remain literal text, discriminators are muted, scores align right, top-three rank numbers have restrained medal colours, and the user's row is purple with a You marker. The personal result stays pinned below the scroll area. When provided by the existing source, total participant count appears in the pinned result. Click/tap opens version metadata and the full player name; another activation closes it. No giant podium or nested card panels.

Loading, unranked, empty, unavailable/retry and stale-request behavior are explicit. The existing read-only data adapter and ranking/score formatting remain unchanged; this revision does not submit scores or alter progression. Physical touch, live service availability, portrait layout and translations are not verified.

`checks-rank.txt` passed board/scope selection, pinned result, version detail open/close, unranked and empty states, error/retry, loading, stale response protection, and smaller-landscape row bounds. The phone capture is 844x390 despite its filename. Local fixtures use 14 rows including a long name with a discriminator, a pinned rank and 1,200 participants. Captures include both boards and failure states.

Used the disposable developed profile with save/network/feedback guards, timeScale 0, VSync disabled and 30 FPS. Temporary source guards were restored byte-for-byte (restoration.json), normal project identity restored, and Play mode stopped. No real progression or synthetic cloud scores were written. Test harnesses remain ignored under Library.
