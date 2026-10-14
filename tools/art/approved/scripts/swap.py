import sys, shutil
from pathlib import Path
T = Path(sys.argv[1]); src = Path(sys.argv[2]); level = sys.argv[3]
MAP = {
 "t1_gba": dict(plain=[0,1], forest=[2,3], mountain=[4,5], wall=[6,7], fort=[8], water=[10,11], bridge=[12]),
 "t2_mid": dict(plain=[0,1,2], forest=[3,4], mountain=[5,6], wall=[7,8], fort=[9], water=[11,12], bridge=[14]),
 "t3_rich": dict(plain=[0,1,2,3], forest=[4,5,6,7], mountain=[8,9,10], wall=[12], fort=[13], water=[14], bridge=[15]),
 "t4_high": dict(plain=[0,1,14], forest=[2,3], mountain=[4,5], wall=[6,7], fort=[8], water=[10,11], bridge=[12]),
}[level]
for tid in MAP:
    for f in list(T.glob(f"{tid}.png*")) + list(T.glob(f"{tid}_*.png*")):
        f.unlink()
    for n, idx in enumerate(MAP[tid]):
        shutil.copy(src / level / f"tile_{idx}.png", T / (f"{tid}.png" if n == 0 else f"{tid}_{n}.png"))
