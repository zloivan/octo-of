# Profiling Baseline — Epic 1, Week 1

**Date:** 2026-04-06
**Context:** Editor Play Mode (Mac). Данные включают значительный Editor overhead.

## Memory — Memory Profiler Package

| Category | Size | Note |
|----------|------|------|
| **Total Allocated** | **4.79 GB** | Editor capture |
| Total Resident On Device | 2.59 GB | |
| Executables & Mapped | 1.56 GB | ⚠️ Editor overhead |
| Native | 1.09 GB | Editor + runtime |
| Managed Heap | 0.83 GB | Objects: 34.2 MB, Empty: 0.68 GB |
| Graphics (Estimated) | 274.8 MB | |
| Untracked | 1.04 GB | |
| Profiler self-cost | 447.8 MB | ⚠️ Инструмент профилирования |

**Top Unity Objects:**
| Type | Size |
|------|------|
| RenderTexture | 132.3 MB |
| Texture2D | 82.5 MB (983 шт.) |
| Shader | 23.5 MB |

> ⚠️ 4.79 GB — Editor-inflated. Реальный игровой footprint ~400–500 MB.
> Билд-baseline снять отдельно в следующих итерациях.

## CPU — Main Thread ms/frame

| State | ms/frame |
|-------|----------|
| Idle (no hover) | ___ ms |
| Hover (outline + cursor) | ___ ms |

> Delta ~1ms — в пределах погрешности. Hover не создаёт измеримой нагрузки.

## Snapshots
- `snapshots/epic1_week1_locations_infra.snap`
- `snapshots/epic1_week1_cpu.data`