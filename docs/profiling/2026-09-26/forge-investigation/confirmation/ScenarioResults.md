# Per-scenario measurements

All times are milliseconds. First two boundary samples are excluded from percentiles. Raw captures retain them. Empty throughput means not observed; zero means observed zero. Cold/transition windows and timeline/probe scenarios must not be treated as steady-state. See the main report for fixture definitions and limitations.

| Scenario | Warm/capture s | Frame median / p95 / p99 / worst | PlayerLoop p95 | GC bytes/frame | Tasks / kills / crafts / rolls per real s | VSync / cap / scale |
|---|---:|---:|---:|---:|---:|---|
| player-forge-probe-mode0-r1 | 3.0/18.0 | 8.33 / 8.34 / 8.41 / 9.17 | 8.34 | 11860.74 | 0.00 / 0.00 / 10.03 / 0.00 | 0 / 120 / 1.00–1.00 |
| player-forge-probe-mode0-r2 | 3.0/18.0 | 8.33 / 8.34 / 8.60 / 18.44 | 8.34 | 11762.88 | 0.00 / 0.00 / 10.04 / 0.00 | 0 / 120 / 1.00–1.00 |
| player-forge-probe-mode3-r1 | 3.0/18.0 | 8.33 / 8.34 / 8.57 / 12.54 | 8.34 | 7922.10 | 0.00 / 0.00 / 10.04 / 0.00 | 0 / 120 / 1.00–1.00 |
| player-forge-probe-mode3-r2 | 3.0/18.0 | 8.33 / 8.34 / 8.62 / 21.18 | 8.34 | 7911.79 | 0.00 / 0.00 / 10.04 / 0.00 | 0 / 120 / 1.00–1.00 |
| player-forge-probe-mode4-r1 | 3.0/18.0 | 8.33 / 8.34 / 8.48 / 9.06 | 8.34 | 11793.05 | 0.00 / 0.00 / 10.04 / 0.00 | 0 / 120 / 1.00–1.00 |
| player-forge-probe-mode4-r2 | 3.0/18.0 | 8.33 / 8.34 / 8.58 / 12.13 | 8.33 | 11762.89 | 0.00 / 0.00 / 10.04 / 0.00 | 0 / 120 / 1.00–1.00 |
| player-forge-probe-mode6-r1 | 3.0/18.0 | 8.33 / 8.34 / 8.55 / 19.33 | 8.34 | 11043.69 | 0.00 / 0.00 / 10.04 / 0.00 | 0 / 120 / 1.00–1.00 |
| player-forge-probe-mode6-r2 | 3.0/18.0 | 8.33 / 8.34 / 8.57 / 10.22 | 8.34 | 11036.18 | 0.00 / 0.00 / 10.04 / 0.00 | 0 / 120 / 1.00–1.00 |
