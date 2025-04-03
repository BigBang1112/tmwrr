# TMWRR

**TMWRR (Trackmania World Record Report)**, aka **WRR v3**, is a continuation of the WRR system from 2021-2022 and the TMWR Discord bot for the next years, supporting TM2020, TM2, and TMF TMX/Solo.

The goal of this project is to **serve Trackmania record accomplishments acknowledged, unified, secured, and validated**. The TMWR Discord bot then sends notifications about the new records through TMWRR webhooks and provides a command interface to explore those accomplishments on Discord. The TMWRR web API can be used to export data that has been tracked over the years.

TMWRR is designed to run seamlessly in case of master server shutdowns or any other long-term future problems. Anyone is also able to host their own instance.

### Legacy WRR and TMWR v2

From 2022-2025, the WRR system runs on an unreliable, unmaintainable code. This could lead the 220+ Discord server bot to a slow painful death. The system also never had Feature Set 3 finished, which was about TMF Solo reading capabilities. The rewrite was decided for the last time so that record notifications can continue being served from all Trackmania games for the next years (where it's possible).

## Dependencies

These are the main dependencies of TMWRR that make this possible.

### ManiaAPI.NET

TMWRR heavily depends on the ManiaAPI.NET v2 library capabilities, which does the abstraction behind the Trackmania internal/external web APIs.

### Revalidate

Used for replay validation. TBD

### Discord.NET 4

Version 4 is freshly developed and expected to be maintained for multiple years.

## Tests

Major issue with WRR v2 is the lack of testability, which contributed to huge uncertainty. TMWRR aims to have testable code with very high coverage.
