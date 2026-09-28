# Forge Player ablation results

First pass; user reported approving an OS-style popup during this pass. Keep as exploratory evidence; see confirmation/ for clean subsequent controls. 3840x2160, VSync off, cap120, developed audit baseline, synthetic resource fuel, Vastium crafting/equipment/salvage cycling at 10 requests per real second, tasting stopped. Each capture 18s after 3s panel settling plus 3s capture warmup.

| Control | Repeats | Frame p95 ms | Main CPU p95 ms | GC KB/frame |
|---|---:|---:|---:|---:|
| Unchanged | 3 | 8.337–8.337 | 4.263–4.493 | 11.752–11.858 |
| Skip resource refresh | 3 | 8.337–8.337 | 4.211–4.300 | 11.431–11.478 |
| Skip equipment/stat refresh | 3 | 8.337–8.337 | 4.182–4.296 | 8.225–8.253 |
| Skip all visual refresh | 3 | 8.337–8.338 | 3.896–4.033 | 7.911–7.919 |
| Freeze all layout | 3 | 8.337–8.338 | 3.981–4.131 | 11.753–11.791 |
| Freeze fitters only | 3 | 8.337–8.338 | 4.299–4.347 | 11.739–11.756 |
| Window closed | 3 | 8.337–8.338 | 3.745–3.824 | 11.005–11.048 |
