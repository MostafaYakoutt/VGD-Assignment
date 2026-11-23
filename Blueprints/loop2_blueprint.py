"""Loop 2 ("Years earlier") blueprints for Left Behind.

Loop 2 is the real house, the same plan as Loop 1, as the protagonist
remembers it two years after the night. Only a few things are wrong, and
each one has a story reason: the table set for six, the same family photo
repeated along the corridor and landing, the study that opens onto fresh
bricks, a landing that narrows towards the Loop Door, and the
rooms that are sealed in Loop 1 (because he has forgotten them by then)
still open here.

Coordinates are Unity world metres (x = east, z = north, drawn as y).
Writes Left_Behind_Loop2_Ground_Blueprint.png and
Left_Behind_Loop2_Upper_Blueprint.png next to this script.
"""
import os

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.patches import Rectangle, Arc, FancyArrowPatch, Circle, Polygon

BG = "#0E4A80"
GRID = "#2A6198"
WALL = "#F2F6FF"
LINE = "#BFD8FF"
TXT = "#E8F0FF"
DIM = "#9CC3F0"
ROUTE = "#F7C948"
LOOP = "#F39A3D"
HATCH = "#5C8DC4"
ODD = "#E36D6D"      # dementia distortions are marked in red
BRICK = "#7A3B2E"
FONT = "DejaVu Sans Mono"
T = 0.1              # interior wall thickness
HERE = os.path.dirname(os.path.abspath(__file__))


class Plan:
    def __init__(self, title, subtitle):
        self.fig = plt.figure(figsize=(18, 13.2), dpi=150, facecolor=BG)
        self.ax = ax = self.fig.add_axes([0.02, 0.03, 0.62, 0.94])
        self.lg = lg = self.fig.add_axes([0.655, 0.03, 0.33, 0.94])
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
        self.text(-12.1, 11.6, title, 15, weight="bold", ha="left")
        self.text(-12.1, 10.95, subtitle, 8.5, color=DIM, ha="left")
        ax.add_patch(FancyArrowPatch((11.6, 10.4), (11.6, 11.6), arrowstyle="-|>", mutation_scale=16,
                                     color=WALL, lw=1.6, zorder=9))
        self.text(11.6, 11.95, "N", 12, weight="bold")
        ax.add_patch(FancyArrowPatch((-10.30, -11.3), (9.98, -11.3), arrowstyle="<|-|>", mutation_scale=12,
                                     color=DIM, lw=1.2))
        self.text(-0.16, -11.9, "20.0 m: same shell and front door as Loop 1", 10, color=DIM)
        self.marks = 0

    # ---------------------------------------------------------------- drawing helpers
    def text(self, x, y, s, size=9, color=TXT, weight="normal", rot=0, ha="center", va="center", bg=False, z=8):
        self.ax.text(x, y, s, fontsize=size, color=color, fontfamily=FONT, fontweight=weight, rotation=rot,
                     ha=ha, va=va, zorder=z, linespacing=1.15,
                     bbox=dict(facecolor=BG, edgecolor="none", pad=1.5) if bg else None)

    def box(self, x0, x1, z0, z1, fc="none", ec=LINE, lw=1.0, ls="-", hatch=None, z=3):
        self.ax.add_patch(Rectangle((x0, z0), x1 - x0, z1 - z0, facecolor=fc, edgecolor=ec, lw=lw, ls=ls,
                                    hatch=hatch, zorder=z))

    def solid(self, x0, x1, z0, z1):
        self.box(x0, x1, z0, z1, fc=WALL, ec=WALL, lw=0.5, z=6)

    def hwall(self, z, x0, x1, gaps=(), t=T):
        """Wall along x at z with openings [(centre, width)]."""
        cur = x0
        for c, w in sorted(gaps):
            self.solid(cur, c - w / 2, z - t / 2, z + t / 2)
            cur = c + w / 2
        self.solid(cur, x1, z - t / 2, z + t / 2)

    def vwall(self, x, z0, z1, gaps=(), t=T):
        cur = z0
        for c, w in sorted(gaps):
            self.solid(x - t / 2, x + t / 2, cur, c - w / 2)
            cur = c + w / 2
        self.solid(x - t / 2, x + t / 2, cur, z1)

    def shell(self, north=(), south=(), west=(), east=()):
        """Outer walls with window gaps; east may include the front door (as a 'door' gap)."""
        self.hwall(9.98, -10.30, 9.98, [g[:2] for g in north], t=0.7)
        self.hwall(-9.99, -10.30, 9.98, [g[:2] for g in south], t=0.7)
        self.vwall(-9.95, -10.34, 10.33, [g[:2] for g in west], t=0.7)
        self.vwall(9.63, -10.34, 10.33, [g[:2] for g in east if len(g) == 2], t=0.7)
        for c, w in north:
            self.window_h(9.98, c, w)
        for c, w in south:
            self.window_h(-9.99, c, w)
        for c, w in west:
            self.window_v(-9.95, c, w)
        for g in east:
            if len(g) == 2:
                self.window_v(9.63, *g)

    def window_h(self, z, c, w):
        for f in (-0.2, 0.0, 0.2):
            self.ax.plot([c - w / 2, c + w / 2], [z + f, z + f], color=LINE, lw=1.1, zorder=6)

    def window_v(self, x, c, w):
        for f in (-0.2, 0.0, 0.2):
            self.ax.plot([x + f, x + f], [c - w / 2, c + w / 2], color=LINE, lw=1.1, zorder=6)

    def door(self, hx, hz, w, a0, a1, leaf_angle, color=LINE):
        """Quarter-circle swing from a hinge; leaf_angle in degrees."""
        import math
        self.ax.add_patch(Arc((hx, hz), 2 * w, 2 * w, theta1=a0, theta2=a1, color=color, lw=1.0, zorder=6))
        r = math.radians(leaf_angle)
        self.ax.plot([hx, hx + w * math.cos(r)], [hz, hz + w * math.sin(r)], color=WALL, lw=2.0, zorder=7)

    def furn(self, x0, x1, z0, z1, label="", size=6.3, rot=0, ls="-"):
        self.box(x0, x1, z0, z1, ec=LINE, lw=1.1, ls=ls, z=5)
        if label:
            self.text((x0 + x1) / 2, (z0 + z1) / 2, label, size, color=LINE, rot=rot, z=7)

    def room(self, x, z, name, size, note="", note_dz=-0.75):
        self.text(x, z, f"{name}\n{size}", 10, weight="bold")
        if note:
            self.text(x, z + note_dz, note, 6.5, color=DIM)

    def oddity(self, x, z, label=None):
        """Numbered red marker for a dementia distortion."""
        self.marks += 1
        n = label if label is not None else self.marks
        self.ax.add_patch(Circle((x, z), 0.38, facecolor=ODD, edgecolor="none", zorder=11))
        self.text(x, z, str(n), 9, color="#1B2B40", weight="bold", z=12)

    def stairs(self, x0, x1, z0, z1, steps, up_from_south=True, label="UP"):
        self.box(x0, x1, z0, z1, ec=LINE, lw=1.2, z=4)
        for i in range(1, steps):
            zz = z0 + i * (z1 - z0) / steps
            self.ax.plot([x0, x1], [zz, zz], color=LINE, lw=0.9, zorder=4)
        a, b = (z0 + 0.25, z1 - 0.25) if up_from_south else (z1 - 0.25, z0 + 0.25)
        cx = (x0 + x1) / 2
        self.ax.add_patch(FancyArrowPatch((cx, a), (cx, b), arrowstyle="-|>", mutation_scale=14, color=WALL,
                                          lw=1.4, zorder=8))
        self.text(cx, (z0 + z1) / 2, label, 7, weight="bold", bg=True)

    def route(self, pts):
        xs, zs = zip(*pts)
        self.ax.plot(xs, zs, color=ROUTE, lw=2.2, ls=(0, (6, 4)), zorder=9)
        self.ax.add_patch(FancyArrowPatch(pts[-2], pts[-1], arrowstyle="-|>", mutation_scale=16, color=ROUTE,
                                          lw=2.2, zorder=9))

    # ---------------------------------------------------------------- legend panel
    def legend(self, notes, distortions, marker_color=LOOP):
        lg = self.lg
        lg.set_xlim(0, 10)
        lg.set_ylim(0, 14)

        def lt(x, y, s, size=9.2, color=TXT, weight="normal", va="center"):
            lg.text(x, y, s, fontsize=size, color=color, fontfamily=FONT, fontweight=weight, ha="left",
                    va=va, linespacing=1.3)

        lt(0.5, 13.3, "LEGEND", 15, weight="bold")
        y = 12.45
        lg.plot([0.6, 1.6], [y, y], color=ROUTE, lw=2.2, ls=(0, (6, 4)))
        lt(2.0, y, notes["route"])
        y -= 0.75
        lg.add_patch(Circle((1.1, y), 0.3, facecolor=ODD, edgecolor="none"))
        lt(2.0, y, "Changed by his memory (see list)")
        y -= 0.75
        lg.add_patch(Rectangle((0.6, y - 0.22), 1.0, 0.44, facecolor="none", edgecolor=HATCH, hatch="////", lw=0))
        lt(2.0, y, "Sealed, not playable")
        y -= 0.75
        for f in (-0.12, 0.0, 0.12):
            lg.plot([0.6, 1.6], [y + f, y + f], color=LINE, lw=1.1)
        lt(2.0, y, "Window (sealed)")
        y -= 0.75
        lg.add_patch(Circle((1.1, y), 0.3, facecolor=marker_color, edgecolor="none"))
        lt(2.0, y, notes["marker"])

        lt(0.5, y - 0.95, "WHAT HIS MEMORY CHANGED", 12, weight="bold")
        lg.text(0.5, y - 1.4, distortions, fontsize=8.1, color=TXT, fontfamily=FONT, ha="left", va="top",
                linespacing=1.28)

    def save(self, name):
        out = os.path.join(HERE, name)
        self.fig.savefig(out, facecolor=BG)
        plt.close(self.fig)
        print("saved", out)



def brick(p, x0, x1, z0, z1):
    p.box(x0, x1, z0, z1, fc=BRICK, ec=BRICK, z=7)


def photo_h(p, x, z):
    p.box(x - 0.25, x + 0.25, z - 0.06, z + 0.06, fc=LINE, ec=LINE, z=7)


# ============================================================================ GROUND FLOOR
# The real house (Loop 1's plan), remembered two years after the night.
g = Plan("LOOP 2  |  GROUND FLOOR  |  YEARS EARLIER",
         "autumn evening, lamplight  |  floor 0.00 m, ceiling 2.6 m  |  the Loop 1 plan, as he remembers it")

g.shell(north=[(-7.9, 1.2), (-2.3, 1.2), (3.85, 1.2)],
        south=[(-5.2, 1.2), (6.0, 1.2)],
        west=[(-6.2, 1.2), (-1.85, 1.2)],
        east=[(4.4, 0.8), (-4.1, 1.6, "door")])

# Front door (never opens) and the spawn point
g.solid(9.30, 9.38, -4.9, -3.3)
g.ax.add_patch(Circle((8.5, -4.1), 0.32, facecolor="#5BD08A", edgecolor="none", zorder=11))
g.text(8.5, -5.0, "spawn\n(facing west)", 6.3, color="#5BD08A")
g.text(11.2, -4.1, "FRONT\nDOOR", 7, color=TXT)

# Interior walls: the same lines as Loop 1
g.vwall(-0.14, 0.0, 9.63, [(3.05, 1.95)])                    # dining | kitchen (double door)
g.hwall(0.0, -9.60, -0.14, [(-3.2, 3.5)])                    # dining | living (wide opening)
g.hwall(0.0, -0.14, 9.28, [(3.0, 0.9)])                      # kitchen | pantry and coat cupboard
g.vwall(-0.14, -7.30, 0.0)                                   # living | pantry and utility
g.hwall(-7.30, -9.60, 6.40, [(-3.45, 3.0), (3.6, 0.9)])      # living and utility | south corridor
g.hwall(-3.60, -0.14, 6.40)                                  # pantry | utility
g.vwall(6.40, -7.30, 0.0)                                    # utility | vestibule and coats
g.hwall(-2.30, 6.40, 9.28, [(8.6, 0.9)])                    # vestibule | coat cupboard
g.vwall(6.40, 4.70, 9.63)                                    # kitchen | stairs
g.hwall(3.30, 6.90, 9.28)                                    # kitchen alcove | back hall


# Stairs: Loop 1's switchback in the back corner, rising north then south
g.stairs(6.55, 7.85, 5.0, 9.45, 14)
g.stairs(7.95, 9.20, 5.0, 9.45, 14, up_from_south=False, label="UP")
g.text(7.85, 4.0, "BACK HALL", 6.5, weight="bold", bg=True)

# Doors
g.door(-0.14, 2.08, 0.97, 0, 90, 0)      # dining -> kitchen (double door, both leaves)
g.door(-0.14, 4.02, 0.97, 270, 360, 0)
g.door(2.55, 0.0, 0.9, 0, 90, 90)        # kitchen -> pantry
g.door(4.05, -7.30, 0.9, 90, 180, 90)    # corridor -> utility
g.door(8.15, -2.30, 0.9, 0, 90, 90)      # vestibule -> coats

# Furniture and story props
g.furn(-7.0, -3.6, 4.4, 6.0, "TABLE SET\nFOR SIX", 6.0)
for cx, cz in ((-6.4, 3.95), (-5.3, 3.95), (-4.2, 3.95), (-6.4, 6.45), (-5.3, 6.45), (-4.2, 6.45)):
    g.furn(cx - 0.22, cx + 0.22, cz - 0.2, cz + 0.2)
g.furn(-9.55, -9.05, 3.0, 7.5, "SIDEBOARD (spare fuse)", 5.3, rot=90)
g.furn(-6.8, -3.2, 9.15, 9.58, "COMMODE", 5.5)
g.furn(1.4, 6.3, 9.0, 9.58, "COUNTERS (reminder notes)", 5.5)
g.furn(0.0, 1.3, 0.1, 1.0, "FRIDGE", 5.3)
g.furn(8.7, 9.23, 0.3, 2.9, "OVEN", 5.0, rot=90)
g.furn(2.2, 3.6, 5.0, 6.2, "TABLE", 5.5)
g.furn(0.0, 0.5, -3.4, -0.2, "SHELVES", 5.0, rot=90)
g.furn(5.85, 6.3, -3.4, -0.2, "JARS", 5.0, rot=90)
g.text(3.1, -1.8, "labelled jars,\nthe overflow of\nhis notes", 5.8, color=DIM)
g.furn(5.85, 6.3, -6.2, -4.9, "FUSE\nBOX", 4.8, rot=90)
g.furn(0.0, 1.2, -7.2, -6.3, "WASHER", 5.0)
g.furn(-9.55, -8.9, -4.6, -2.4, "FIRE", 5.0, rot=90)
g.furn(-6.2, -5.0, -6.0, -2.6, "COUCH", 5.3, rot=90)
g.furn(-9.1, -6.7, -7.2, -6.5, "COUCH", 5.3)
g.furn(-3.4, -2.4, -3.0, -2.1, "ARM\nCHAIR", 4.8)
g.furn(-1.3, -0.2, -2.9, -2.2, "TV", 5.0)
g.furn(6.5, 8.1, -0.55, -0.05, "COATS", 5.0)
g.furn(-9.55, -8.86, -9.1, -7.8, "TABLE", 4.6, rot=90)

# The same family photograph, five times along the corridor
for x in (-8.0, -1.5, 0.8, 2.6, 5.2):
    photo_h(g, x, -7.42)
g.text(-1.0, -9.0, "SOUTH CORRIDOR  19 x 2.3 m", 7.5, weight="bold", bg=True)

# Room labels
g.room(-5.3, 8.1, "DINING ROOM", "9.5 x 9.6 m", "", 0)
g.room(3.3, 7.7, "KITCHEN", "6.5 x 9.6 m", "fridge note: RUTH TOOK THE KIDS", -0.85)
g.room(-5.0, -0.95, "LIVING ROOM", "9.5 x 7.3 m", "", 0)
g.room(3.1, -0.95, "PANTRY", "6.5 x 3.6 m", "", 0)
g.room(3.1, -5.1, "UTILITY", "6.5 x 3.7 m", "power upstairs is out", -0.8)
g.room(7.85, -6.4, "VESTIBULE", "2.9 x 5.0 m", "", 0)
g.text(7.85, -1.25, "COATS", 7.0, weight="bold")

# What his memory changed
g.oddity(-2.9, 4.4, 1)      # table for six
g.oddity(-1.5, -7.95, 2)    # the same photo
g.oddity(1.6, -4.4, 3)      # rooms sealed in Loop 1 are open here

g.route([(8.5, -4.1), (7.6, -4.1), (7.6, -8.4), (-3.45, -8.4), (-3.45, 1.2), (-0.14, 3.05),
         (5.4, 3.05), (5.4, 4.0), (7.2, 4.0), (7.2, 5.0)])

g.legend(
    {"route": "Front door to the stairs (about 40 m,\nthe same route as Loop 1)", "marker": "Spawn, the same spot as Loop 1"},
    "Loop 2 is the real house, the Loop 1 plan,\n"
    "as Walter remembers it two years after the\n"
    "night. Only three things are wrong, and\n"
    "each one has a reason:\n\n"
    "1  The table is set for six. There were\n"
    "   only ever four of them.\n"
    "2  The same family photograph hangs five\n"
    "   times along the corridor: it is the\n"
    "   one thing he looks at every day.\n"
    "3  Room B, sealed in Loop 1, is open: a\n"
    "   pantry and the utility room with the\n"
    "   fuse box. He still remembers them.\n"
    "   By the present day he has forgotten\n"
    "   them, so they are walled off.\n\n"
    "Story: fuse in the dining sideboard,\n"
    "fuse box in the utility room.", marker_color="#5BD08A")
g.save("Left_Behind_Loop2_Ground_Blueprint.png")

# ============================================================================ UPPER FLOOR
u = Plan("LOOP 2  |  UPPER FLOOR  |  YEARS EARLIER",
         "floor +2.60 m, ceiling 2.40 m  |  the Loop 1 plan  |  power is out until the fuse is fixed")

# Not playable: the west half of the south strip, and the study behind its bricks
u.box(-9.60, -0.19, -9.64, -4.75, hatch="////", ec=HATCH, lw=0, z=1)
u.text(-4.9, -7.2, "SEALED\n(not playable)", 8, color=DIM, bg=True)
u.box(-0.09, 6.37, 2.75, 9.63, hatch="////", ec=HATCH, lw=0, z=1)
u.text(3.1, 6.6, "STUDY\nbehind the bricks\n(Loop 3 only)", 8, color=DIM, bg=True)

u.shell(north=[(-7.9, 1.2), (-2.3, 1.2), (3.85, 1.2)],
        south=[(3.0, 1.2)],
        west=[(7.0, 1.2), (-1.85, 1.2)],
        east=[(4.4, 0.8), (-3.0, 0.8)])

# Loop Door in the west wall, at the narrow end of the landing
u.ax.plot([-9.60, -9.60], [0.15, 1.05], color=LOOP, lw=5, zorder=8, solid_capstyle="butt")
u.ax.add_patch(Circle((-8.85, 0.6), 0.36, facecolor=LOOP, edgecolor="none", zorder=11))
u.text(-8.85, 0.6, "L", 10, color="#1B2B40", weight="bold", z=12)
u.text(-11.35, 0.6, "LOOP\nDOOR", 7.5, color=LOOP)

# Landing: straight south wall; north wall straight to x -3, then slants down to 1.0 m wide
u.hwall(0.05, -9.60, 9.28, [(-3.45, 0.9), (3.45, 0.9), (6.95, 1.0)])
u.hwall(2.15, -3.0, 6.42, [(-1.8, 0.9), (2.95, 0.9)])
u.ax.add_patch(Polygon([(-9.60, 1.10), (-3.0, 2.10), (-3.0, 2.20), (-9.60, 1.20)], closed=True,
                       facecolor=WALL, edgecolor=WALL, zorder=6))
u.vwall(-0.14, 2.15, 9.63)               # master | study
u.vwall(6.42, 2.15, 9.63)                # study | stairwell
u.vwall(-0.14, -4.70, 0.05)              # children | bathroom
u.hwall(-4.70, -9.60, 6.42)              # children and bathroom | south strip
u.vwall(6.42, -9.64, 0.05, [(-8.0, 0.9)])  # attic hall | sewing room

# The study door opens onto bricks
u.door(2.50, 2.15, 0.9, 0, 90, 90, color=ODD)
brick(u, 2.45, 3.45, 2.25, 2.50)
u.text(4.5, 1.6, "LOCKED, then bricks", 6.3, color=ODD, weight="bold")

# Doors
u.door(-2.25, 2.15, 0.9, 0, 90, 90)      # master
u.door(-3.90, 0.05, 0.9, 270, 360, 270)  # children's room
u.door(3.00, 0.05, 0.9, 270, 360, 270)   # bathroom
u.door(6.45, 0.05, 1.0, 270, 360, 270)   # attic stairs hall
u.door(6.42, -8.45, 0.9, 180, 270, 270)  # sewing room

# Stairwell from the ground floor (arrive heading south) and the attic stairs
u.stairs(6.55, 7.85, 5.7, 9.45, 14)
u.stairs(7.95, 9.20, 5.7, 9.45, 14, up_from_south=False, label="")
u.ax.plot([6.5, 6.5], [5.65, 9.6], color=WALL, lw=2.5, zorder=7)
u.text(8.6, 4.7, "arrive from\nground floor", 6.3, color=DIM)
u.box(7.3, 8.5, -4.0, -2.8, ec=DIM, lw=1.0, ls=(0, (4, 3)), z=4)
u.furn(8.6, 9.23, -4.5, -1.5, "SHELF", 5.0, rot=90)
u.furn(8.4, 9.0, -6.3, -4.9, "BOXES", 4.6, rot=90)
u.text(7.9, -3.4, "attic\nhatch", 6.0, color=DIM)

# Furniture
u.furn(-6.0, -4.0, 7.43, 9.58, "MASTER\nBED", 5.5)
u.furn(-9.55, -9.0, 3.0, 5.2, "WARDROBE", 5.0, rot=90)
u.furn(-0.8, -0.19, 4.6, 6.4, "DRESSER", 5.0, rot=90)
u.furn(-8.2, -7.2, -4.6, -2.6, "BED", 5.5)
u.furn(-6.2, -5.2, -4.6, -2.6, "BED", 5.5)
u.furn(-9.55, -9.05, -1.3, -0.4, "TOYS", 4.8, rot=90)
u.furn(-2.6, -1.4, -4.6, -4.0, "DESK", 5.5)
u.text(-6.7, -2.0, "teddy bear on the bed,\nmusic box, drawing", 5.8, color=DIM)
u.furn(2.0, 3.7, -4.4, -3.6, "BATHTUB", 5.5)
u.furn(-0.09, 0.51, -3.3, -1.4, "SINK", 5.0, rot=90)
u.furn(5.72, 6.37, -2.1, -1.5, "WC", 5.0)
u.furn(0.0, 1.4, -9.58, -8.8, "SEWING\nTABLE", 4.6)
u.furn(-0.09, 0.5, -7.8, -5.2, "FABRIC", 5.0, rot=90)
u.furn(4.6, 5.6, -5.6, -4.8, "SEWING\nBOX", 4.4)
u.furn(3.9, 5.2, -9.58, -9.0, "DRESS FORM", 4.6)

# The same photograph along the landing
for x in (-7.0, -1.0, 1.5, 4.8):
    photo_h(u, x, 0.2)

# Room labels
u.room(-5.0, 5.0, "MASTER BEDROOM", "9.4 x 7.4 m", "one side of the bed slept in", -0.85)
u.room(-4.9, -1.2, "CHILDREN'S ROOM", "9.4 x 4.6 m", "", 0)
u.room(2.6, -1.0, "BATHROOM", "6.5 x 4.6 m", "", 0)
u.room(3.0, -7.0, "SEWING ROOM", "6.5 x 4.9 m", "Ruth's room, untouched", -0.8)
u.text(7.85, -8.6, "BACK\nLANDING", 7.0, weight="bold")
u.text(-6.4, 1.05, "LANDING narrows 2.0 -> 1.0 m", 6.3, weight="bold", bg=True)

u.oddity(1.9, 2.65, 4)     # study door onto bricks
u.oddity(-6.6, 1.6, 5)     # narrowing landing
u.oddity(-1.0, 0.75, 6)    # same photo along the landing
u.oddity(5.0, -7.6, 7)     # sewing room exists

u.route([(7.2, 5.7), (7.2, 1.1), (-8.3, 0.6)])

u.legend(
    {"route": "Top of the stairs to the Loop Door\n(about 18 m)", "marker": "Loop Door (one-way, to Loop 3)"},
    "4  The study door is locked. The key is in\n"
    "   Ruth's sewing box. Behind the door is\n"
    "   a wall of fresh brick: the midpoint.\n"
    "5  The landing narrows from 2.0 m to\n"
    "   1.0 m towards the Loop Door, so the\n"
    "   walls close in as he walks to it.\n"
    "6  The family photograph again, on the\n"
    "   landing.\n"
    "7  Ruth's sewing room exists here. In\n"
    "   Loop 1 this strip is sealed: he has\n"
    "   forgotten it.\n\n"
    "Everything else is the real upper floor:\n"
    "master bedroom, children's room kept\n"
    "ready, bathroom, and the back landing\n"
    "(the attic stairs hall in Loop 1), whose\n"
    "attic hatch is closed: no attic here.")
u.save("Left_Behind_Loop2_Upper_Blueprint.png")
