"""Attic blueprint for Left Behind.

Same coordinates as the Unity scene (x = east, z = north, drawn as y).
The attic floor is at +5.15 m on the full 20 x 20 m shell. A flat ceiling
2.6 m above the floor spans z -7.63..7.63; along the north and south
walls the roof slopes down to 1.2 m knee walls. The boxed attic stairs
from the upper floor arrive heading south in the stair-head at
x 7.95..9.28, z -7.15..-1.15, which opens west into the attic.
"""
import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.patches import Rectangle, Arc, FancyArrowPatch, Circle

BG = "#0E4A80"
GRID = "#2A6198"
WALL = "#F2F6FF"
LINE = "#BFD8FF"
TXT = "#E8F0FF"
DIM = "#9CC3F0"
ROUTE = "#F7C948"
HATCH = "#5C8DC4"
LOW = "#3E74AE"
FONT = "DejaVu Sans Mono"

fig = plt.figure(figsize=(18, 13.2), dpi=150, facecolor=BG)
ax = fig.add_axes([0.02, 0.03, 0.62, 0.94])
lg = fig.add_axes([0.655, 0.03, 0.33, 0.94])
for a in (ax, lg):
    a.set_facecolor(BG)
    a.set_xticks([])
    a.set_yticks([])
    for s in a.spines.values():
        s.set_color(WALL)
        s.set_linewidth(2)
ax.set_xlim(-12.6, 12.4)
ax.set_ylim(-12.9, 12.3)
ax.set_aspect("equal")
for v in range(-12, 13):
    ax.axvline(v, color=GRID, lw=0.4, zorder=0)
    ax.axhline(v, color=GRID, lw=0.4, zorder=0)


def text(x, y, s, size=9, color=TXT, weight="normal", rot=0, ha="center", va="center", z=8, bg=False):
    ax.text(x, y, s, fontsize=size, color=color, fontfamily=FONT, fontweight=weight, rotation=rot,
            ha=ha, va=va, zorder=z, linespacing=1.15,
            bbox=dict(facecolor=BG, edgecolor="none", pad=1.5) if bg else None)


def box(x0, x1, z0, z1, fc="none", ec=LINE, lw=1.0, ls="-", hatch=None, z=3):
    ax.add_patch(Rectangle((x0, z0), x1 - x0, z1 - z0, facecolor=fc, edgecolor=ec, lw=lw, ls=ls,
                           hatch=hatch, zorder=z))


def wall(x0, x1, z0, z1):
    box(x0, x1, z0, z1, fc=WALL, ec=WALL, lw=0.5, z=6)


def furn(x0, x1, z0, z1, label="", size=6.5, rot=0, ls="-"):
    box(x0, x1, z0, z1, ec=LINE, lw=1.1, ls=ls, z=5)
    if label:
        text((x0 + x1) / 2, (z0 + z1) / 2, label, size=size, color=LINE, rot=rot, z=7)


def boxes(cells):
    for x, z, w, d in cells:
        furn(x - w / 2, x + w / 2, z - d / 2, z + d / 2)


# ---------------------------------------------------------------- low ceiling bands (roof slopes)
for z0, z1 in ((7.63, 9.63), (-9.64, -7.63)):
    box(-9.60, 9.28, z0, z1, fc=LOW, ec="none", lw=0, z=1)
    ax.plot([-9.60, 9.28], [z1 if z0 > 0 else z0] * 2, color=DIM, lw=0)
for zz in (7.63, -7.63):
    ax.plot([-9.60, 9.28], [zz, zz], color=DIM, lw=1.0, ls=(0, (6, 4)), zorder=4)
text(-1.0, 9.05, "roof slopes down: 2.6 m  ->  1.2 m knee wall", size=7, color=TXT, bg=True)
text(-1.0, -9.05, "roof slopes down: 2.6 m  ->  1.2 m knee wall", size=7, color=TXT, bg=True)

# ---------------------------------------------------------------- walls
ow = [
    (-10.30, 9.98, 9.63, 10.33),                      # north knee wall
    (-10.30, 9.98, -10.34, -9.64),                    # south knee wall
    # west gable: round windows at z 0.5 and 4.5 (0.7 m)
    (-10.30, -9.60, 4.85, 10.33), (-10.30, -9.60, 0.85, 4.15), (-10.30, -9.60, -10.34, 0.15),
    # east gable: round window at z 4.0
    (9.28, 9.98, 4.35, 10.33), (9.28, 9.98, -10.34, 3.65),
]
for w in ow:
    wall(*w)
for zc in (0.5, 4.5):
    for f in (0.3, 0.5, 0.7):
        ax.plot([-10.30 + 0.7 * f] * 2, [zc - 0.35, zc + 0.35], color=LINE, lw=1.1, zorder=6)
for f in (0.3, 0.5, 0.7):
    ax.plot([9.28 + 0.7 * f] * 2, [3.65, 4.35], color=LINE, lw=1.1, zorder=6)

# Stair-head box: west wall with the door, north and south walls
wall(7.95, 8.05, -7.15, -6.90)
wall(7.95, 8.05, -6.00, -1.15)
wall(7.95, 9.28, -1.25, -1.15)
wall(8.05, 9.28, -7.15, -7.05)
ax.add_patch(Arc((7.95, -6.90), 1.8, 1.8, theta1=90, theta2=180, color=LINE, lw=1.0, zorder=6))
ax.plot([7.95, 7.05], [-6.90, -6.90], color=WALL, lw=2.0, zorder=7)
text(6.65, -7.25, "attic door", size=6.5, color=DIM)

# Chimney (above the ground floor fireplace)
box(-9.60, -8.90, -4.70, -2.80, fc="#7A3B2E", ec=WALL, lw=1.0, z=5)
text(-8.30, -3.75, "CHIMNEY", size=6.5, color=TXT, rot=90)

# Timber posts along the edge of the slopes
for px in (-8.0, -4.0, 0.0, 4.0):
    for pz in (7.55, -7.55):
        box(px - 0.08, px + 0.08, pz - 0.08, pz + 0.08, fc=LINE, ec=LINE, lw=0.5, z=6)

# Attic stairs arriving from the upper floor (15 steps rising south) and the stair-head landing
box(8.05, 9.25, -5.70, -1.20, fc="none", ec=LINE, lw=1.2, z=4)
for i in range(1, 15):
    zz = -1.20 - i * 0.30
    ax.plot([8.05, 9.25], [zz, zz], color=LINE, lw=0.9, zorder=4)
ax.add_patch(FancyArrowPatch((8.65, -1.45), (8.65, -5.45), arrowstyle="-|>", mutation_scale=14,
                             color=WALL, lw=1.4, zorder=8))
text(8.65, -3.4, "UP", size=7, color=TXT, weight="bold", bg=True)
text(8.05, -0.55, "STAIR-HEAD", size=7, color=TXT, weight="bold")
text(8.65, -6.45, "landing", size=6, color=DIM)

# ---------------------------------------------------------------- furniture
# North-west: storage, furniture under dust sheets
furn(-6.90, -5.10, 5.90, 6.70, "COUCH\n(dust sheet)", 5.5)
furn(-4.05, -3.15, 5.75, 6.65, "ARMCHAIR\n(sheet)", 5)
furn(-9.60, -9.25, 0.90, 2.10, "SHELF", 5.5, rot=90)
furn(-8.36, -7.24, 4.33, 4.87, "TRUNK", 5.5)
boxes([(x, 8.60, 0.6, 0.5) for x in (-9.0, -7.9, -6.8, -5.4, -4.3, -3.2)])
boxes([(-9.05, 6.95, 0.6, 0.5), (-9.05, 6.30, 0.6, 0.5), (-8.30, 7.10, 0.5, 0.5)])
text(-6.0, 3.10, "STORED FURNITURE", size=9.5, weight="bold")
text(-6.0, 2.45, "covered couch and armchair, trunk,\nboxes under the eaves", size=6.5, color=DIM)

# South-west: the children's things
furn(-8.50, -6.50, -6.90, -5.90, "OLD BED", 6)
furn(-5.80, -5.00, -7.40, -6.90, "TOYS", 5)
furn(-4.77, -4.43, -5.77, -5.43, "", 5)
ax.add_patch(Circle((-6.0, -4.60), 0.75, facecolor="none", edgecolor=DIM, lw=0.8, ls=(0, (4, 3)), zorder=4))
boxes([(x, -8.60, 0.6, 0.5) for x in (-9.0, -7.9, -6.8, -5.7, -4.6)])
text(-6.0, -2.25, "THE CHILDREN'S THINGS", size=9.5, weight="bold")
text(-6.0, -2.90, "old bed, toy chest, stool, rug", size=6.5, color=DIM)

# Centre: a lone chair facing the west window, a tall mirror
furn(-6.80, -6.20, 0.25, 0.75, "", 5)
ax.add_patch(FancyArrowPatch((-6.35, 0.50), (-8.70, 0.50), arrowstyle="-|>", mutation_scale=10,
                             color=DIM, lw=0.9, ls=(0, (3, 3)), zorder=6))
text(-6.5, 1.25, "a chair facing\nthe window", size=6.5, color=DIM)
furn(-1.60, -0.40, -3.98, -3.92, "", 5)
text(-1.0, -4.45, "tall mirror", size=6.5, color=DIM)

# North-east: the old dining set
ax.add_patch(Circle((3.5, 4.8), 0.6, facecolor="none", edgecolor=LINE, lw=1.1, zorder=5))
text(3.5, 4.8, "TABLE", size=5.5, color=LINE)
furn(2.35, 2.80, 4.60, 5.00, "", 5)
furn(4.20, 4.65, 4.60, 5.00, "", 5)
furn(3.30, 3.70, 5.75, 6.20, "", 5)
furn(6.45, 6.75, 6.65, 6.95, "", 5)
text(6.6, 7.25, "lamp", size=6, color=DIM)
boxes([(x, 8.60, 0.6, 0.5) for x in (1.2, 2.3, 3.4, 4.5, 5.6, 6.7)])
text(3.6, 2.90, "OLD DINING SET", size=9.5, weight="bold")
text(3.6, 2.25, "small table, three chairs", size=6.5, color=DIM)

# South-east: sideboard and boxes by the stair-head
furn(4.45, 5.55, -7.50, -6.90, "SIDEBOARD", 5.5)
boxes([(x, -8.60, 0.6, 0.5) for x in (1.2, 2.3, 3.4)])

# Ceiling bulbs (switchable)
for bx, bz in ((-6.0, 5.0), (-6.0, -4.5), (0.0, 0.0), (4.0, 3.4), (5.5, -4.5)):
    ax.add_patch(Circle((bx, bz), 0.16, facecolor="none", edgecolor=ROUTE, lw=1.2, zorder=7))

# Main route from the stair-head door
ax.plot([7.40, 3.0, 0.0, -5.6], [-6.45, -6.45, 0.0, 0.5], color=ROUTE, lw=2.2, ls=(0, (6, 4)), zorder=9)
ax.add_patch(FancyArrowPatch((-5.0, 0.45), (-5.6, 0.50), arrowstyle="-|>", mutation_scale=14,
                             color=ROUTE, lw=2.2, zorder=9))

text(2.2, 1.55, "ATTIC   18.9 x 19.3 m   (flat ceiling 2.6 m)", size=10.5, weight="bold", bg=True)

# ---------------------------------------------------------------- title, dimensions
text(-12.1, 11.6, "ATTIC   |   LEFT BEHIND, HOUSE PLAN", size=15, weight="bold", ha="left")
text(-12.1, 10.95, "floor +5.15 m, flat ceiling +7.75 m, knee walls 1.2 m on the north and south sides",
     size=8.5, color=DIM, ha="left")
ax.add_patch(FancyArrowPatch((11.6, 10.4), (11.6, 11.6), arrowstyle="-|>", mutation_scale=16,
                             color=WALL, lw=1.6, zorder=9))
text(11.6, 11.95, "N", size=12, weight="bold")
ax.add_patch(FancyArrowPatch((-10.30, -11.3), (9.98, -11.3), arrowstyle="<|-|>", mutation_scale=12,
                             color=DIM, lw=1.2))
text(-0.16, -11.9, "20.0 m (outer)   |   18.9 m inside", size=10, color=DIM)
ax.add_patch(FancyArrowPatch((11.0, -10.34), (11.0, 10.33), arrowstyle="<|-|>", mutation_scale=12,
                             color=DIM, lw=1.2))
text(11.55, 0.0, "20.0 m (outer)   |   19.3 m inside", size=10, color=DIM, rot=90)

# ---------------------------------------------------------------- legend
lg.set_xlim(0, 10)
lg.set_ylim(0, 14)


def ltext(x, y, s, size=9.5, color=TXT, weight="normal"):
    lg.text(x, y, s, fontsize=size, color=color, fontfamily=FONT, fontweight=weight, ha="left",
            va="center", linespacing=1.3)


ltext(0.5, 13.3, "LEGEND", 15, weight="bold")
y = 12.3
lg.plot([0.6, 1.6], [y, y], color=ROUTE, lw=2.2, ls=(0, (6, 4)))
ltext(2.0, y, "Route from the attic door\nto the lone chair")
y -= 0.95
lg.add_patch(Rectangle((0.6, y - 0.25), 1.0, 0.5, facecolor=LOW, edgecolor=DIM, lw=0.8))
ltext(2.0, y, "Low ceiling: roof slopes from\n2.6 m down to 1.2 m")
y -= 0.95
for f in (-0.12, 0.0, 0.12):
    lg.plot([0.6, 1.6], [y + f, y + f], color=LINE, lw=1.1)
ltext(2.0, y, "Round gable window (sealed)")
y -= 0.8
lg.add_patch(Rectangle((0.6, y - 0.3), 1.0, 0.6, facecolor="none", edgecolor=LINE, lw=1.2))
for i in range(1, 5):
    lg.plot([0.6 + i * 0.2] * 2, [y - 0.3, y + 0.3], color=LINE, lw=0.9)
ltext(2.0, y, "Stairs (arrow points up)")
y -= 0.8
lg.add_patch(Circle((1.1, y), 0.16, facecolor="none", edgecolor=ROUTE, lw=1.2))
ltext(2.0, y, "Bare ceiling bulb (switchable)")
y -= 0.8
lg.add_patch(Rectangle((1.02, y - 0.08), 0.16, 0.16, facecolor=LINE, edgecolor=LINE))
ltext(2.0, y, "Timber post")
y -= 0.8
lg.add_patch(Rectangle((0.6, y - 0.25), 1.0, 0.5, facecolor="none", edgecolor=LINE, lw=1.1))
ltext(2.0, y, "Furniture and storage boxes")

ltext(0.5, 5.7, "NOTES", 13, weight="bold")
notes = (
    "One open room under a gable roof, on the\n"
    "same 20 x 20 m shell. Floor +5.15 m.\n\n"
    "The attic stairs from the upper floor arrive\n"
    "in the stair-head (SE). Its door now faces\n"
    "west into the full-height part of the attic\n"
    "instead of south under the low roof.\n\n"
    "Where the family's past is packed away:\n"
    "covered furniture, the children's things,\n"
    "the old dining set, and one chair set out\n"
    "facing the window.\n\n"
    "Attic floor about 365 m2, of which about\n"
    "290 m2 has full headroom."
)
lg.text(0.5, 5.2, notes, fontsize=8.6, color=TXT, fontfamily=FONT, ha="left", va="top", linespacing=1.3)

out = __file__.replace("attic_blueprint.py", "Left_Behind_Attic_Blueprint.png")
fig.savefig(out, facecolor=BG)
print("saved", out)
