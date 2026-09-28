# Per-scenario measurements

All times are milliseconds. First two boundary samples are excluded from percentiles. Raw captures retain them. Empty throughput means not observed; zero means observed zero. Cold/transition windows and timeline/probe scenarios must not be treated as steady-state. See the main report for fixture definitions and limitations.

| Scenario | Warm/capture s | Frame median / p95 / p99 / worst | PlayerLoop p95 | GC bytes/frame | Tasks / kills / crafts / rolls per real s | VSync / cap / scale |
|---|---:|---:|---:|---:|---:|---|
| player-town-developed-idle-r1 | 5.0/15.0 | 0.41 / 0.53 / 0.70 / 3.56 | 0.53 | 510.52 | 0.00 / 0.00 / 0.00 / 0.00 | 0 / -1 / 1.00–1.00 |
| player-town-fresh-r1 | 5.0/15.0 | 0.43 / 0.51 / 0.71 / 2.45 | 0.50 | 395.93 | 0.00 / 0.00 / 0.00 / 0.00 | 0 / -1 / 1.00–1.00 |
