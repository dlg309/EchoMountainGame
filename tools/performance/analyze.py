import csv, math, statistics, sys
from collections import defaultdict
from pathlib import Path
path = Path(sys.argv[1])
rows = defaultdict(list)
with path.open(newline='', encoding='utf-8-sig') as f:
    for row in csv.DictReader(f):
        if row['label'].startswith('Stress') or row.get('screen_h') is None: continue
        rows[row['label']].append(row)

def pct(values, p):
    v = sorted(values)
    return v[max(0, math.ceil(p*len(v))-1)]
print("label frames wall_s avg_ms p95_ms p99_ms worst_ms >33ms >50ms unity_mem_start_mb unity_mem_end_mb unity_mem_peak_mb gc0_delta avg_cpu_ms avg_gpu_ms avg_main_ms avg_render_ms avg_physics_ms avg_scripts_ms avg_draw_ms max_speed_kmh grounded_pct landings resets")
for label, group in rows.items():
    group = group[1:]
    vals = [float(r['wall_ms']) for r in group]
    mem = [int(r['unity_alloc_bytes'])/1048576 for r in group]
    def avg_nonzero(key, scale=1):
        x = [float(r[key])*scale for r in group if float(r[key]) > 0]
        return statistics.mean(x) if x else float('nan')
    print(label, len(group), round(sum(vals)/1000,1), round(statistics.mean(vals),2), round(pct(vals,.95),2), round(pct(vals,.99),2), round(max(vals),2), sum(v>33.33 for v in vals), sum(v>50 for v in vals), round(mem[0],1), round(mem[-1],1), round(max(mem),1), int(group[-1]['gc0'])-int(group[0]['gc0']), round(avg_nonzero('cpu_ms'),2), round(avg_nonzero('gpu_ms'),2), round(avg_nonzero('cpu_main_ms'),2), round(avg_nonzero('cpu_render_ms'),2), round(avg_nonzero('physics_ns',1e-6),2), round(avg_nonzero('scripts_ns',1e-6),2), round(avg_nonzero('draw_ns',1e-6),2), round(max(float(r['speed_kmh']) for r in group),1), round(100*sum(int(r['grounded']) for r in group)/len(group),1), max(int(r['landings']) for r in group), max(int(r['reset']) for r in group))
