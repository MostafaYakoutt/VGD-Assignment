"""Second floor blueprint for Left Behind.

Coordinates are Unity world metres: x = east, z = north (drawn as y).
The footprint matches the built ground floor: inner faces of the outer
walls at x -9.60 / 9.28 and z -9.64 / 9.63. The stairwell from the
ground floor is the opening x 6.47..9.28, z 5.63..9.63; the upper flight
arrives heading south at z 5.63.
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
LOOP = "#F39A3D"
HATCH = "#5C8DC4"
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

# 1 m grid
for v in range(-12, 13):
    ax.axvline(v, color=GRID, lw=0.4, zorder=0)
    ax.axhline(v, color=GRID, lw=0.4, zorder=0)


def text(x, y, s, size=9, color=TXT, weight="normal", rot=0, ha="center", va="center", z=8, bg=False):
    ax.text(x, y, s, fontsize=size, color=color, fontfamily=FONT, fontweight=weight,
            rotation=rot, ha=ha, va=va, zorder=z, linespacing=1.15,
            bbox=dict(facecolor=BG, edgecolor="none", pad=1.5) if bg else None)


def box(x0, x1, z0, z1, fc="none", ec=LINE, lw=1.0, ls="-", hatch=None, z=3, alpha=1.0):
    ax.add_patch(Rectangle((x0, z0), x1 - x0, z1 - z0, facecolor=fc, edgecolor=ec, lw=lw,
                           ls=ls, hatch=hatch, zorder=z, alpha=alpha))


def wall(x0, x1, z0, z1):
    box(x0, x1, z0, z1, fc=WALL, ec=WALL, lw=0.5, z=6)


def furn(x0, x1, z0, z1, label="", size=6.5, rot=0):
    box(x0, x1, z0, z1, ec=LINE, lw=1.1, z=5)
    if label:
        text((x0 + x1) / 2, (z0 + z1) / 2, label, size=size, color=LINE, rot=rot, z=7)


# ---------------------------------------------------------------- not playable
box(-9.60, 6.37, -9.64, -4.70, fc="none", ec=HATCH, lw=0, hatch="////", z=1)
text(-1.6, -7.1, "SEALED\n(not playable, filler like\nthe ground floor's Room B)", size=9, color=DIM, bg=True)

# ---------------------------------------------------------------- walls
# Outer walls, 0.7 m thick, split around the window and Loop Door openings
ow = [
    # north wall: windows at x -7.9, -2.3 (Master), 3.85 (Study)
    (-10.30, -8.50, 9.63, 10.33), (-7.30, -2.90, 9.63, 10.33), (-1.70, 3.25, 9.63, 10.33),
    (4.45, 9.98, 9.63, 10.33),
    # south wall (no windows: the sealed strip and the attic store sit behind it)
    (-10.30, 9.98, -10.34, -9.64),
    # west wall: window z 7.0 (Master), Loop Door z 0.6..1.6, window z -1.85 (Children's)
    (-10.30, -9.60, 7.60, 10.33), (-10.30, -9.60, 1.60, 6.40), (-10.30, -9.60, -1.25, 0.60),
    (-10.30, -9.60, -10.34, -2.45),
    # east wall: window z 4.4 (landing), window z -3.0 (attic stairs hall)
    (9.28, 9.98, 4.80, 10.33), (9.28, 9.98, -2.60, 4.00), (9.28, 9.98, -10.34, -3.40),
]
for w in ow:
    wall(*w)

# Interior walls, 0.1 m thick
iw = [
    # landing north wall (z 2.10..2.20) with doors to the Master Bedroom and Study
    (-9.60, -5.45, 2.10, 2.20), (-4.55, 2.05, 2.10, 2.20), (2.95, 6.47, 2.10, 2.20),
    # landing south wall (z 0.00..0.10) with doors to Children's Room, Bathroom, attic hall
    (-9.60, -3.45, 0.00, 0.10), (-2.55, 2.55, 0.00, 0.10), (3.45, 6.90, 0.00, 0.10),
    (7.90, 9.28, 0.00, 0.10),
    # Master | Study, on the ground floor's x -0.14 wall line
    (-0.19, -0.09, 2.20, 9.63),
    # Study east wall = west side of the stairwell and of the landing's east arm
    (6.37, 6.47, 2.20, 9.63),
    # Children's | Bathroom
    (-0.19, -0.09, -4.70, 0.00),
    # rooms | sealed strip
    (-9.60, 6.37, -4.70, -4.60),
    # Bathroom | attic stairs hall
    (6.37, 6.47, -9.64, 0.00),
]
for w in iw:
    wall(*w)

# Windows: thin double lines across the opening
def window_ns(xc, w, z0=9.63, z1=10.33):
    for f in (0.3, 0.5, 0.7):
        ax.plot([xc - w / 2, xc + w / 2], [z0 + (z1 - z0) * f] * 2, color=LINE, lw=1.1, zorder=6)


def window_ew(zc, w, x0, x1):
    for f in (0.3, 0.5, 0.7):
        ax.plot([x0 + (x1 - x0) * f] * 2, [zc - w / 2, zc + w / 2], color=LINE, lw=1.1, zorder=6)


window_ns(-7.9, 1.2)
window_ns(-2.3, 1.2)
window_ns(3.85, 1.2)
window_ew(7.0, 1.2, -10.30, -9.60)
window_ew(-1.85, 1.2, -10.30, -9.60)
window_ew(4.4, 0.8, 9.28, 9.98)
window_ew(-3.0, 0.8, 9.28, 9.98)

# ---------------------------------------------------------------- doors (0.9 m, swing into the room)
def door(hx, hz, w, start, end, leaf_dx, leaf_dz):
    ax.add_patch(Arc((hx, hz), 2 * w, 2 * w, theta1=start, theta2=end, color=LINE, lw=1.0, zorder=6))
    ax.plot([hx, hx + leaf_dx], [hz, hz + leaf_dz], color=WALL, lw=2.0, zorder=7)


door(-5.45, 2.20, 0.9, 0, 90, 0, 0.9)      # Master Bedroom
door(2.95, 2.20, 0.9, 90, 180, 0, 0.9)     # Study
door(-3.45, 0.00, 0.9, 270, 360, 0, -0.9)  # Children's Room
door(3.45, 0.00, 0.9, 180, 270, 0, -0.9)   # Bathroom
door(6.90, 0.00, 1.0, 270, 360, 0, -1.0)   # attic stairs hall

# Loop Door in the west wall at the far end of the landing
ax.plot([-9.60, -9.60], [0.60, 1.60], color=LOOP, lw=5, zorder=8, solid_capstyle="butt")
ax.add_patch(Circle((-8.75, 1.10), 0.42, facecolor=LOOP, edgecolor="none", zorder=9))
text(-8.75, 1.10, "L", size=11, color="#1B2B40", weight="bold", z=10)
text(-11.35, 1.10, "LOOP\nDOOR\n(one-way)", size=7.5, color=LOOP)

# ---------------------------------------------------------------- stairwell from the ground floor
# Flight 1 (west, x 6.47..7.87) is below the floor: dashed. Flight 2 (east, x 7.87..9.28) arrives here.
box(6.47, 9.28, 5.63, 9.63, fc="none", ec=LINE, lw=1.2, z=4)
for i in range(1, 16):
    zz = 5.63 + i * 4.0 / 16
    ax.plot([6.47, 7.87], [zz, zz], color=DIM, lw=0.7, ls=(0, (3, 3)), zorder=4)
    ax.plot([7.87, 9.28], [zz, zz], color=LINE, lw=0.9, zorder=4)
text(7.17, 7.63, "open to\nground\nfloor", size=6.5, color=DIM, bg=True)
ax.add_patch(FancyArrowPatch((8.57, 9.35), (8.57, 5.85), arrowstyle="-|>", mutation_scale=14,
                             color=WALL, lw=1.4, zorder=8))
text(8.57, 7.10, "UP", size=7, color=TXT, weight="bold", bg=True)
# Railing over the drop: south edge above flight 1 and the banister between the flights
ax.plot([6.47, 7.87], [5.63, 5.63], color=WALL, lw=3.0, zorder=7)
ax.plot([7.87, 7.87], [5.63, 9.15], color=WALL, lw=2.0, zorder=7)
text(7.17, 5.30, "railing", size=6, color=DIM)
text(7.87, 10.75, "STAIRWELL  2.8 x 4.0 m", size=7.5, color=DIM)

# ---------------------------------------------------------------- attic stairs
ax_x0, ax_x1, st0, st1 = 8.05, 9.25, -1.20, -5.70  # one straight flight rising south
box(ax_x0, ax_x1, st1, st0, fc="none", ec=LINE, lw=1.2, z=4)
for i in range(1, 15):
    zz = st0 - i * (st0 - st1) / 15
    ax.plot([ax_x0, ax_x1], [zz, zz], color=LINE, lw=0.9, zorder=4)
ax.add_patch(FancyArrowPatch((8.65, -1.40), (8.65, -5.50), arrowstyle="-|>", mutation_scale=14,
                             color=WALL, lw=1.4, zorder=8))
ax.plot([8.00, 8.00], [-2.60, -5.70], color=WALL, lw=2.0, zorder=7)  # handrail on the open side
box(8.05, 9.25, -7.10, -5.70, fc="none", ec=DIM, lw=1.0, ls=(0, (4, 3)), z=4)
text(7.15, -3.45, "UP TO ATTIC\n15 steps\n+2.55 m", size=6.8, color=TXT)
text(8.65, -6.40, "attic\nlanding\nabove", size=6, color=DIM)
text(7.87, -8.55, "ATTIC STAIRS\nHALL\n2.8 x 9.6 m\n\nstorage under\nthe attic", size=7.5, color=TXT,
     weight="bold")

# ---------------------------------------------------------------- furniture
# Master Bedroom (x -9.60..-0.19, z 2.20..9.63)
box(-7.0, -3.0, 6.1, 9.0, fc="none", ec=DIM, lw=0.8, ls=(0, (4, 3)), z=4)        # rug
furn(-6.0, -4.0, 7.43, 9.63, "MASTER\nBED\n2.0x2.2")
furn(-6.62, -6.12, 9.15, 9.60, "NS", 5.5)
furn(-3.88, -3.38, 9.15, 9.60, "NS", 5.5)
furn(-9.60, -9.00, 2.60, 5.20, "WARDROBE", 6, rot=90)
furn(-8.95, -8.15, 6.60, 7.40, "CHAIR", 5.5)
furn(-0.80, -0.19, 4.60, 6.40, "DRESSER", 6, rot=90)
furn(-0.75, -0.19, 7.60, 8.80, "VANITY", 6, rot=90)
text(-4.9, 4.7, "MASTER BEDROOM\n9.4 x 7.4 m", size=11, weight="bold")
text(-4.9, 3.85, "wedding ring on the dresser,\npill bottle on the nightstand", size=6.8, color=DIM)

# Study (x -0.09..6.37, z 2.20..9.63)
furn(-0.09, 0.31, 3.40, 9.40, "BOOKSHELVES", 6, rot=90)
furn(3.05, 4.65, 8.60, 9.40, "DESK", 6.5)
furn(3.55, 4.15, 7.85, 8.45, "", 5)
furn(5.75, 6.32, 8.70, 9.55, "FILES", 5.5, rot=90)
furn(1.10, 1.90, 3.70, 4.50, "CHAIR", 5.5)
text(3.10, 6.30, "STUDY\n6.5 x 7.4 m", size=11, weight="bold")
text(3.10, 5.20, "final scene (the knife)\nLoop 1: plain wall, no door\nLoop 2: locked", size=6.8, color=DIM)

# Children's Room (x -9.60..-0.19, z -4.60..0.00)
furn(-8.20, -7.20, -4.60, -2.60, "BED\n1.0x2.0", 6)
furn(-6.20, -5.20, -4.60, -2.60, "BED\n1.0x2.0", 6)
furn(-6.95, -6.45, -4.60, -4.15, "NS", 5.5)
furn(-9.60, -9.10, -2.25, -1.45, "TOY\nCHEST", 5, rot=90)
furn(-4.20, -3.00, -4.60, -4.00, "WARDROBE", 5.5)
furn(-2.60, -1.40, -4.60, -4.00, "DESK", 6)
furn(-0.60, -0.19, -3.60, -2.20, "SHELF", 5.5, rot=90)
text(-5.9, -1.15, "CHILDREN'S ROOM\n9.4 x 4.6 m", size=11, weight="bold")
text(-5.9, -2.05, "teddy bear, drawing, music box", size=6.8, color=DIM)

# Bathroom (x -0.09..6.37, z -4.60..0.00)
furn(2.00, 3.70, -4.40, -3.60, "BATHTUB", 6.5)
box(5.17, 6.37, -4.60, -3.40, fc="none", ec=LINE, lw=1.1, z=5)
ax.plot([5.17, 5.17], [-4.60, -3.40], color=WALL, lw=1.6, zorder=6)
text(5.77, -4.00, "WALK-IN\nSHOWER", size=5.5, color=LINE)
furn(5.72, 6.37, -2.10, -1.50, "WC", 6)
furn(-0.09, 0.51, -3.30, -1.40, "DOUBLE\nSINK", 5.5, rot=90)
furn(4.30, 4.90, -4.60, -4.00, "WASHER", 5)
furn(2.40, 3.30, -3.30, -2.80, "MAT", 5)
text(2.60, -1.55, "BATHROOM\n6.5 x 4.6 m", size=11, weight="bold")
text(2.60, -2.40, "spacious, no outside wall", size=6.8, color=DIM)

# Landing
text(-1.2, 1.62, "UPPER LANDING  16.1 x 2.0 m", size=8.5, weight="bold", bg=True)
for lx in (-6.5, -1.6, 3.3):
    ax.add_patch(Circle((lx, 0.60), 0.13, facecolor="none", edgecolor=LINE, lw=1.0, zorder=6))
text(1.1, 0.55, "ceiling lights (one flickers)", size=6, color=DIM)
text(7.45, 3.75, "arrive from\nground\nfloor", size=7, color=TXT, bg=True)
furn(6.52, 6.92, 3.40, 4.60, "PHOTOS", 5, rot=90)

# ---------------------------------------------------------------- route
ax.plot([8.57, 8.57, -8.30], [5.45, 1.10, 1.10], color=ROUTE, lw=2.2, ls=(0, (6, 4)), zorder=9)
ax.add_patch(FancyArrowPatch((-7.6, 1.10), (-8.30, 1.10), arrowstyle="-|>", mutation_scale=16,
                             color=ROUTE, lw=2.2, zorder=9))
for x, z0, z1 in ((-5.0, 1.10, 3.4), (2.5, 1.10, 3.4), (-3.0, 1.10, -1.0), (3.0, 1.10, -0.6),
                  (7.4, 1.10, -0.9)):
    ax.plot([x, x], [z0, z1], color=ROUTE, lw=1.2, ls=(0, (2, 2.5)), zorder=9)

# ---------------------------------------------------------------- frame, title, dimensions
text(-12.1, 11.6, "UPPER FLOOR   |   LEFT BEHIND, HOUSE PLAN", size=15, weight="bold", ha="left")
text(-12.1, 10.95, "floor level +2.60 m, ceiling 2.40 m (to +5.00 m), attic floor +5.15 m",
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

# ---------------------------------------------------------------- legend panel
lg.set_xlim(0, 10)
lg.set_ylim(0, 14)


def ltext(x, y, s, size=9.5, color=TXT, weight="normal", ha="left"):
    lg.text(x, y, s, fontsize=size, color=color, fontfamily=FONT, fontweight=weight, ha=ha,
            va="center", linespacing=1.3)


ltext(0.5, 13.3, "LEGEND", 15, weight="bold")
rows = 12.3
lg.add_patch(Circle((1.1, rows), 0.32, facecolor=LOOP, edgecolor="none"))
lg.text(1.1, rows, "L", fontsize=11, color="#1B2B40", fontweight="bold", ha="center", va="center",
        fontfamily=FONT)
ltext(2.0, rows, "Loop Door (end of the Upper Landing,\none-way, in the west wall)")
rows -= 0.95
lg.plot([0.6, 1.6], [rows, rows], color=ROUTE, lw=2.2, ls=(0, (6, 4)))
ltext(2.0, rows, "Main route: top of the stairs to the\nLoop Door (about 20 m)")
rows -= 0.95
lg.plot([0.6, 1.6], [rows, rows], color=ROUTE, lw=1.2, ls=(0, (2, 2.5)))
ltext(2.0, rows, "Side rooms")
rows -= 0.8
lg.add_patch(Rectangle((0.6, rows - 0.25), 1.0, 0.5, facecolor="none", edgecolor=HATCH, hatch="////", lw=0))
ltext(2.0, rows, "Not playable / sealed")
rows -= 0.8
for f in (-0.12, 0.0, 0.12):
    lg.plot([0.6, 1.6], [rows + f, rows + f], color=LINE, lw=1.1)
ltext(2.0, rows, "Window (sealed, cannot open)")
rows -= 0.8
lg.add_patch(Rectangle((0.6, rows - 0.3), 1.0, 0.6, facecolor="none", edgecolor=LINE, lw=1.2))
for i in range(1, 5):
    lg.plot([0.6 + i * 0.2] * 2, [rows - 0.3, rows + 0.3], color=LINE, lw=0.9)
ltext(2.0, rows, "Stairs (arrow points up)")
rows -= 0.8
lg.plot([0.6, 1.6], [rows, rows], color=WALL, lw=3)
ltext(2.0, rows, "Railing / banister")
rows -= 0.8
lg.add_patch(Rectangle((0.6, rows - 0.25), 1.0, 0.5, facecolor="none", edgecolor=LINE, lw=1.1))
ltext(2.0, rows, "Furniture (NS = nightstand)")

ltext(0.5, 5.7, "NOTES", 13, weight="bold")
notes = (
    "Same 20 x 20 m shell as the ground floor:\n"
    "outer walls 0.7 m, inner walls 0.1 m.\n\n"
    "The stairs from the ground floor arrive\n"
    "in the NE corner heading south, over the\n"
    "Back Hall. A railing guards the drop over\n"
    "the first flight.\n\n"
    "The Upper Landing is one long straight\n"
    "hallway, a nod to P.T., ending at the\n"
    "Loop Door.\n\n"
    "Walls at x = -0.14 and z = 0 sit on the\n"
    "ground floor walls below.\n\n"
    "Rooms: Master Bedroom 70 m2, Study 48 m2,\n"
    "Children's Room 43 m2, Bathroom 30 m2,\n"
    "Landing 48 m2, Attic stairs hall 27 m2.\n"
    "Playable upper floor: about 266 m2."
)
lg.text(0.5, 5.2, notes, fontsize=8.6, color=TXT, fontfamily=FONT, ha="left", va="top", linespacing=1.3)

out = __file__.replace("second_floor_blueprint.py", "Left_Behind_Second_Floor_Blueprint.png")
fig.savefig(out, facecolor=BG)
print("saved", out)
