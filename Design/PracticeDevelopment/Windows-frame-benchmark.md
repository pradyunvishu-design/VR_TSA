# FIRST180 standalone Windows performance

Measured on 2026-11-25 with the laptop connected to AC power. The non-development Windows benchmark player rendered the existing lobby and all five saved practice rooms on the internal display.

## Test conditions

- AMD Ryzen AI 5 430 w/ Radeon 840M, AMD Radeon(TM) 840M Graphics, 15.3 GiB system memory. The integrated graphics device reports 7.7 GiB graphics memory.
- Display ATNA40KW02-0, 1920 × 1200 at 60 Hz. Unity 6000.5.10f1, Direct3D12, Ultra graphics preset.
- Three seconds of warm-up before each 12-second sample. The lobby sample turns a full circle at the entry. Room samples move the desktop camera 3.5 metres forward with a lateral pan while a training incident and patient animations are active.
- Vertical sync was disabled only for this diagnostic process to measure rendering headroom. The project uses vertical sync by default. The timer records the interval between consecutive engine frames. The physical display refreshes at 60 Hz.
- An interval of 16.67 ms corresponds to 60 fps. Each scene passes when its 95th-percentile frame interval is within that budget. The median and P95 use the nearest-rank convention.

## 1920 × 1200

Window mode FullScreenWindow. The player reported 0 managed errors during the measured session.

| Scene | Frames | Mean fps | Median ms | P95 ms | P99 ms | ≤16.67 ms | 60 fps at P95 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| Training hub | 1,681 | 140.0 | 6.28 | 11.36 | 16.17 | 99.1% | Pass |
| Juniper Café | 1,161 | 96.7 | 9.51 | 15.42 | 17.80 | 98.0% | Pass |
| Commons dining hall | 1,302 | 108.4 | 8.75 | 13.33 | 16.88 | 98.9% | Pass |
| Riverside service road | 1,207 | 100.6 | 9.19 | 15.53 | 25.82 | 95.7% | Pass |
| Fabrication workshop | 1,537 | 128.1 | 7.47 | 10.45 | 13.01 | 99.7% | Pass |
| North Ridge refuge | 964 | 80.3 | 11.87 | 17.60 | 23.10 | 91.2% | Miss |

Camera sightlines reached 15/15 patient chest locations at least once during the scripted routes. This checks viewport inclusion and solid, non-trigger colliders along each ray. The player had foreground focus throughout the sample.

## 1600 × 1000

Window mode Windowed. The player reported 0 managed errors during the measured session.

| Scene | Frames | Mean fps | Median ms | P95 ms | P99 ms | ≤16.67 ms | 60 fps at P95 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| Training hub | 2,271 | 189.2 | 4.47 | 8.70 | 9.86 | 100.0% | Pass |
| Juniper Café | 1,735 | 144.5 | 6.35 | 10.56 | 12.47 | 99.9% | Pass |
| Commons dining hall | 1,564 | 130.3 | 7.20 | 10.94 | 12.57 | 100.0% | Pass |
| Riverside service road | 1,803 | 150.2 | 6.53 | 7.79 | 9.73 | 99.9% | Pass |
| Fabrication workshop | 1,920 | 159.9 | 5.82 | 9.32 | 13.48 | 99.7% | Pass |
| North Ridge refuge | 1,413 | 117.7 | 7.75 | 12.77 | 14.87 | 99.4% | Pass |

Camera sightlines reached 15/15 patient chest locations at least once during the scripted routes. This checks viewport inclusion and solid, non-trigger colliders along each ray. The player had foreground focus throughout the sample.

## Scope and reproduction

Each resolution is one traversal with the Unity Editor open in the background. These measurements cover this laptop and camera path. Headset, worst-case, and long-session performance require separate measurements. OpenXR reported that a runtime was unavailable. The desktop player rendered in mono.

In the Editor, choose **FIRST180 Practice → Diagnostics → Build Windows frame benchmark**. The command writes a separate player into the Windows temporary directory and includes the harness only in that build. The harness starts only with the explicit flag. It sets the practice session to test mode so learner progress and clinical exports are untouched.

Run the following in PowerShell. The player exits after six samples and writes `frame-results.json` next to the executable. Use windowed mode for the 1600 × 1000 fallback.

```powershell
$bench = Join-Path $env:TEMP "FIRST180-PracticeBenchmark"
$player = Join-Path $bench "FIRST180-Benchmark.exe"
Start-Process $player -Wait -ArgumentList "-first180-frame-benchmark -no-xr -monitor 1 -screen-fullscreen 1 -window-mode borderless -screen-width 1920 -screen-height 1200"
Copy-Item "$bench\frame-results.json" "$bench\native-run.json" -Force
Start-Process $player -Wait -ArgumentList "-first180-frame-benchmark -no-xr -monitor 1 -screen-fullscreen 0 -screen-width 1600 -screen-height 1000"
Copy-Item "$bench\frame-results.json" "$bench\fallback-run.json" -Force
```

