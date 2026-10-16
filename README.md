# Toy Story 3 Complete Edition — FPS Patcher

An unofficial Windows app for changing the game's FPS cap from 60 to a selected target between **75 and 360 FPS**.

**Open the app → select your FPS → Apply.** No Python or command line is needed.

> **Beta / validation pending:** the application has passed byte-level and simulated transaction tests, and Steam discovery worked on the development machine. The GUI and real disk apply/restore have not yet been tested end-to-end. Keep the first release as a draft until this is checked on a copy of the supported executable. Do not describe it as fully tested or guaranteed safe.

## Download

Download **TS3-FPS-Patcher.exe** from the [Releases page](https://github.com/torbolink/TS3-CE-Steam-UnlockFPSPatch/releases). The source archives GitHub generates automatically are for developers, not the app download.

Windows x64; uses .NET Framework 4.x. No original game executable or assets are distributed. The app is unsigned, so Windows may show a reputation warning; a signature or reputation warning is not itself a malware scan result.

## Use

1. Close the game and open the patcher.
2. Check the automatically detected Steam path. Use **Browse** if necessary.
3. Select a target and click **Apply FPS**. If write permission is required, the app offers to restart with Windows administrator approval; click Apply again after restarting.
4. Start the game through Steam and choose **60 FPS** in the game menu. Its label stays unchanged; that branch now uses the chosen target.

The **30 FPS option remains unchanged**. Driver caps and synchronization can still limit achieved FPS.

Supported targets: **75, 90, 100, 120, 144, 165, 180, 200, 240, 277, 280, 300, 360**.

## Restore

Click **Restore original** with the game closed. The app stores a verified clean backup beside the executable as:

```text
Toy Story 3 Complete Edition.exe.fps-patcher.original.bak
```

It refuses to overwrite a backup with an unexpected hash. It can also reconstruct the verified original from its exact known FPS-only variants. Keep an independent clean backup for the beta test.

## Supported build

The clean executable must have:

```text
File: Toy Story 3 Complete Edition.exe
Size: 10,712,576 bytes
SHA-256: f87941b418a56b935a5e8372c11720304410d204a8cabd1d9970becfceff9722
```

Other builds, including graphics-modified executables, are rejected. A game update may require an updated patcher. Do not force the patch onto an unknown build.

The app changes no graphics assets or saves. Replacing an executable does not undo unrelated modifications already made to assets.

## How the patch was identified

Comparing settings files revealed the flag selecting the 30/60 FPS modes. Tracing that flag into the executable identified a frame-wait routine with intervals of `1/60` and `1/30` second. The patch replaces only the little-endian 64-bit floating-point interval at file offset `0x7ED810` with `1 / targetFPS`.

The eight-byte slot is the only range allowed to change. File size and every other byte, including the 30 FPS interval at `0x7ED818`, remain identical to the supported original. For 120 FPS, only one byte actually differs. Expected output SHA-256 hashes are embedded and checked.

## What has been tested

- Output bytes and hashes for all 13 FPS targets.
- Apply, switching and restore using an in-memory file store.
- Unknown executable and unexpected-backup rejection.
- Simulated replacement failure, temporary cleanup and rollback after simulated corruption.
- Steam discovery and manifest path traversal rejection.

The earlier manual patches had user-reported gameplay results for **120 and 277 FPS targets**. This does not validate all targets, every level, cutscene or physics behavior. The new GUI application's physical file replacement and interface still need an end-to-end test.

## Verify a download

Compare the SHA-256 of the downloaded app with `SHA256SUMS.txt` attached to the **same release**. Hashes help detect file differences; they do not independently prove publisher identity or that software is harmless.

Download only from this repository's release assets. The patcher has no networking code, embedded game binary or automatic updater. Source is available in `src/` for inspection.

## Build from source

Run `build.cmd` on Windows with the .NET Framework C# compiler available. It produces `bin/TS3-FPS-Patcher.exe`. This is for contributors; players can use the release executable directly.

The build is not claimed to be byte-for-byte reproducible: rebuilding with another compiler or build metadata may produce a different binary hash. The published checksum refers to the exact attached binary.

## Reports and license

Report the patcher version, supported-game-file hash, Windows version, FPS target and exact error or affected scene in [Issues](https://github.com/torbolink/TS3-CE-Steam-UnlockFPSPatch/issues). Do not attach game executables, saves, account data or credentials.

MIT license for the patcher and its original documentation. Game files are not included or licensed by this project. Unofficial community project, not affiliated with the developer, publisher, Valve or Steam.
