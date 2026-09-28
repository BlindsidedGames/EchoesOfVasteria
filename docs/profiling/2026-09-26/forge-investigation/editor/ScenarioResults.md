# Per-scenario measurements

All times are milliseconds. First two boundary samples are excluded from percentiles. Raw captures retain them. Empty throughput means not observed; zero means observed zero. Cold/transition windows and timeline/probe scenarios must not be treated as steady-state. See the main report for fixture definitions and limitations.

| Scenario | Warm/capture s | Frame median / p95 / p99 / worst | PlayerLoop p95 | GC bytes/frame | Tasks / kills / crafts / rolls per real s | VSync / cap / scale |
|---|---:|---:|---:|---:|---:|---|
| editor-cleanup | 1.0/3.0 | 9.47 / 13.13 / 14.56 / 22.05 | 5.87 | 19413.49 | — / — / — / 0.00 | 1 / -1 / 1.00–1.00 |
| editor-closed-r1 | 5.0/15.0 | 10.67 / 21.23 / 23.70 / 27.34 | 6.07 | 47674.31 | — / — / — / 0.00 | 0 / 120 / 1.00–1.00 |
| editor-closed-r2 | 5.0/15.0 | 10.42 / 22.40 / 24.55 / 88.37 | 6.20 | 47547.29 | — / — / — / 0.00 | 0 / 120 / 1.00–1.00 |
| editor-layout-frozen-r1 | 5.0/15.0 | 12.09 / 24.26 / 26.61 / 30.18 | 6.73 | 53359.74 | — / — / — / 0.00 | 0 / 120 / 1.00–1.00 |
| editor-layout-frozen-r2 | 5.0/15.0 | 12.11 / 24.63 / 30.25 / 114.21 | 6.83 | 54320.20 | — / — / — / 0.00 | 0 / 120 / 1.00–1.00 |
| editor-matched-player-open-r1 | 5.0/15.0 | 11.50 / 30.59 / 33.18 / 87.68 | 10.49 | 58496.18 | — / — / — / 0.00 | 0 / 120 / 1.00–1.00 |
| editor-matched-player-open-r2 | 5.0/15.0 | 12.30 / 26.42 / 29.61 / 32.19 | 7.34 | 54582.72 | — / — / — / 0.00 | 0 / 120 / 1.00–1.00 |
| editor-original-setup-open-r1 | 5.0/15.0 | 13.14 / 27.63 / 31.62 / 93.94 | 7.95 | 58117.45 | — / — / — / 99.73 | 1 / -1 / 1.00–1.00 |
| editor-original-setup-open-r2 | 5.0/15.0 | 12.65 / 26.16 / 29.40 / 32.04 | 7.41 | 55976.33 | — / — / — / 100.26 | 1 / -1 / 1.00–1.00 |
| editor-timeline | 3.0/5.0 | 15.00 / 115.32 / 231.06 / 287.66 | 14.14 | 769813.51 | — / — / — / 101.11 | 1 / -1 / 1.00–1.00 |
