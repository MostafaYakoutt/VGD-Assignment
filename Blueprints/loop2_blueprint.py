"""Loop 2 ("Years earlier") blueprints for Left Behind.

Same 20 x 20 m shell and front door as the Loop 1 house, but the inside is
rearranged the way his memory has rearranged it: rooms in the wrong order,
a corridor that runs in a circle, a living room shrunk around its furniture,
a duplicate living room, a door that opens onto bricks, a stairwell in the
middle of the house, a hallway that narrows towards the Loop Door, and a
master bedroom that can only be reached through the bathroom.

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
        lt(2.0, y, "Dementia distortion (see list)")
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

        lt(0.5, y - 0.95, "DEMENTIA DISTORTIONS", 12, weight="bold")
        lg.text(0.5, y - 1.4, distortions, fontsize=8.1, color=TXT, fontfamily=FONT, ha="left", va="top",
                linespacing=1.28)

    def save(self, name):
        out = os.path.join(HERE, name)
        self.fig.savefig(out, facecolor=BG)
        plt.close(self.fig)
        print("saved", out)


# ============================================================================ GROUND FLOOR
g = Plan("LOOP 2  |  GROUND FLOOR  |  YEARS EARLIER",
         "autumn evening, lamplight  |  floor 0.00 m, ceiling 2.45 m  |  same shell, front door and spawn as Loop 1")

# Sealed areas
g.box(-9.60, -5.05, -4.45, 0.45, hatch="////", ec=HATCH, lw=0, z=1)
g.text(-7.3, -2.0, "SEALED\nbehind the bricks", 7.5, color=DIM, bg=True)

g.shell(north=[(-6.5, 1.2), (-1.5, 1.2), (5.5, 1.2)],
        south=[(-6.0, 1.2), (1.0, 1.2)],
        west=[(2.5, 1.2), (-7.0, 1.2)],
        east=[(1.5, 0.8), (-4.1, 1.6, "door")])

# Front door (never opens) and the spawn point
g.solid(9.30, 9.38, -4.9, -3.3)
g.ax.add_patch(Circle((8.5, -4.1), 0.32, facecolor="#5BD08A", edgecolor="none", zorder=11))
g.text(8.5, -5.0, "spawn\n(facing west)", 6.3, color="#5BD08A")
g.text(11.2, -4.1, "FRONT\nDOOR", 7, color=TXT)

# Interior walls
g.hwall(4.5, -9.60, 9.28, [(-3.0, 1.2), (3.5, 0.9), (7.0, 0.9)])       # north rooms | ring and utility
g.hwall(-4.5, -9.60, 5.0, [(-1.0, 2.0)])                                # ring | other living room
g.hwall(0.5, -9.60, -5.0)                                               # small living | sealed
g.vwall(-5.0, -4.5, 4.5, [(2.5, 0.9), (-2.0, 0.9)])                     # ring | west rooms
g.vwall(5.0, -9.64, 4.5, [(-3.2, 1.6), (1.5, 0.9)])                     # ring | east rooms
g.hwall(-6.2, 5.0, 9.28, [(7.5, 0.9)])                                  # vestibule | coats
g.hwall(-2.0, 5.0, 9.28)                                                # vestibule | utility
g.vwall(2.0, 4.5, 9.63, [(7.0, 0.9)])                                   # dining | kitchen
# Core: stairwell with a cupboard either side
g.hwall(3.0, -3.5, 3.5)
g.hwall(-3.0, -3.5, 3.5, [(0.0, 1.4)])
g.vwall(-3.5, -3.0, 3.0, [(0.0, 0.9)])
g.vwall(3.5, -3.0, 3.0, [(0.0, 0.9)])
g.vwall(-0.75, -3.0, 3.0)
g.vwall(0.75, -3.0, 3.0)

# Doors
g.door(-3.45, 4.5, 1.2, 0, 90, 90)          # dining
g.door(3.05, 4.5, 0.9, 0, 90, 90)           # kitchen
g.door(6.55, 4.5, 0.9, 270, 360, 270)       # kitchen -> utility
g.door(1.95, 6.55, 0.9, 0, 90, 0)           # dining -> kitchen
g.door(-5.0, 2.05, 0.9, 90, 180, 180)       # small living
g.door(5.0, 1.05, 0.9, 0, 90, 0)            # utility
g.door(7.05, -6.2, 0.9, 270, 360, 270)      # coats
g.door(-3.5, -0.45, 0.9, 90, 180, 180)      # west cupboard
g.door(3.5, -0.45, 0.9, 0, 90, 0)           # east cupboard
# The door to nowhere: a real door leaf in the ring wall, bricks right behind it
g.door(-5.0, -2.45, 0.9, 90, 180, 90, color=ODD)
g.solid(-5.30, -5.15, -2.6, -1.4)
g.box(-5.30, -5.15, -2.6, -1.4, fc=BRICK, ec=BRICK, z=7)

# Stairs: one straight flight in the middle of the house, rising north
g.stairs(-0.6, 0.6, -2.2, 2.0, 15)

# Furniture and story props
g.furn(-6.4, -2.6, 6.4, 7.7, "TABLE SET FOR\nTHE WHOLE FAMILY", 5.8)
for cx, cz in ((-5.8, 6.0), (-4.5, 6.0), (-3.2, 6.0), (-5.8, 8.1), (-4.5, 8.1), (-3.2, 8.1)):
    g.furn(cx - 0.22, cx + 0.22, cz - 0.2, cz + 0.2)
g.furn(-9.55, -9.05, 5.2, 8.6, "SIDEBOARD", 5.5, rot=90)
g.furn(-0.6, 1.9, 9.1, 9.58, "PHOTOS", 5.5)
g.furn(2.1, 9.23, 9.0, 9.58, "COUNTERS (reminder notes)", 5.5)
g.furn(8.6, 9.23, 5.0, 8.9, "", 5)
g.furn(4.0, 5.2, 6.4, 7.6, "TABLE", 5.5)
g.furn(5.1, 6.1, -1.9, -1.2, "WASHER", 5)
g.furn(8.5, 9.23, 2.6, 3.9, "FUSE\nBOX", 5.5)
g.furn(5.1, 5.7, 2.2, 4.3, "SHELF", 5, rot=90)
g.furn(-9.55, -8.0, 1.0, 1.8, "SOFA", 5.5)
g.furn(-7.8, -6.9, 1.0, 1.9, "ARM\nCHAIR", 4.8)
g.furn(-9.55, -8.9, 2.4, 3.9, "FIRE", 5, rot=90)
g.furn(-6.7, -5.2, 3.6, 4.4, "SOFA", 5.5)
g.furn(-6.4, -5.6, 2.2, 2.9, "TV", 5)
g.furn(-3.4, -0.85, 1.2, 2.9, "LABELLED\nBOXES", 5.3)
g.furn(0.85, 3.4, -2.9, -2.4, "", 5)
g.text(2.15, -1.5, "one note:\n'WHERE\nARE THEY?'", 5.8, color=DIM)
g.furn(-3.2, -2.3, -8.0, -7.1, "ARM\nCHAIR", 4.8)
g.furn(-3.4, -2.1, -9.58, -9.15, "TV", 5.5)
g.furn(5.1, 9.23, -9.58, -9.0, "COATS + BOOTS", 5.3)

# Ring corridor photos: the same frame on every inner wall
for x, z in ((0.0, 3.2), (-3.7, 1.8), (3.7, -1.8), (2.2, -3.2)):
    g.box(x - 0.25, x + 0.25, z - 0.06, z + 0.06, fc=LINE, ec=LINE, z=7)

# Room labels
g.room(-4.0, 9.0, "DINING ROOM", "11.6 x 5.1 m", "", 0)
g.room(5.6, 8.0, "KITCHEN", "7.3 x 5.1 m")
g.room(7.15, -4.1, "VESTIBULE", "4.3 x 4.2 m")
g.room(7.15, -0.5, "UTILITY", "4.3 x 6.4 m", "power is out")
g.room(7.15, -7.9, "COATS", "4.3 x 3.4 m")
g.room(-7.75, 2.95, "LIVING ROOM", "4.6 x 4.0 m", "", 0)
g.room(-3.2, -6.0, "THE OTHER LIVING ROOM", "14.6 x 5.1 m", "one armchair, the TV, a lamp")
g.text(0.0, 4.0, "RING HALL  1.5 m wide, about 40 m around", 7.5, weight="bold", bg=True)

# Dementia distortions
g.oddity(-4.25, 3.75, 1)    # ring corridor
g.oddity(-7.3, 0.95, 2)     # living room shrunk
g.oddity(-4.45, -2.0, 3)    # door to nowhere
g.oddity(0.0, -3.7, 4)      # stairs in the middle
g.oddity(-4.6, -8.6, 5)     # duplicate living room
g.oddity(-6.4, 9.1, 6)      # wrong order: dining first

g.route([(8.5, -4.1), (5.6, -4.1), (5.6, -3.75), (4.3, -3.75), (0.0, -3.75), (0.0, -2.2)])
g.text(2.5, -4.15, "to the stairs", 6.3, color=ROUTE)

g.legend(
    {"route": "Front door to the stairs (about 9 m)", "marker": "Spawn, the same spot as Loop 1"},
    "1  The hall runs in a circle around the\n"
    "   stairs. The same photo hangs on every\n"
    "   side, so it feels like walking in loops.\n"
    "2  The living room has shrunk to 4.6 x 4 m\n"
    "   but still holds all its furniture.\n"
    "3  A real door in the hall opens onto a\n"
    "   brick wall.\n"
    "4  The stairs moved from the back corner\n"
    "   to the middle of the house.\n"
    "5  A second living room: wide and nearly\n"
    "   empty, one armchair facing the TV.\n"
    "6  Rooms are in the wrong order: the\n"
    "   dining room is now first, the kitchen\n"
    "   is beside it, the living room is last.\n\n"
    "LOOP 2 STORY (from the GCD)\n"
    "Table set for the whole family, photos\n"
    "back on the walls, reminder notes on the\n"
    "kitchen cupboards. The power is out: the\n"
    "fuse box is in the utility room.", marker_color="#5BD08A")
g.save("Left_Behind_Loop2_Ground_Blueprint.png")

# ============================================================================ UPPER FLOOR
u = Plan("LOOP 2  |  UPPER FLOOR  |  YEARS EARLIER",
         "floor +2.60 m, ceiling 2.40 m  |  the stairs arrive in the middle  |  no attic in this loop")

# Sealed south-east block
u.box(-3.45, 9.28, -9.64, -3.05, hatch="////", ec=HATCH, lw=0, z=1)
u.text(2.9, -6.4, "SEALED\n(not playable)", 8, color=DIM, bg=True)

u.shell(north=[(-7.0, 1.2), (-2.5, 1.2), (2.0, 0.8), (6.4, 1.2)],
        south=[(-6.5, 1.2)],
        west=[(6.0, 1.2), (-5.5, 1.2), (-0.35, 0.8)],
        east=[(6.4, 1.2), (0.0, 1.2)])

# Narrowing corridor: straight south wall, slanted north wall (1.6 m wide at the landing, 0.9 m at the door)
u.hwall(-0.8, -9.60, -3.5, [(-6.5, 0.9)])
u.ax.add_patch(Polygon([(-9.60, 0.05), (-3.5, 0.75), (-3.5, 0.85), (-9.60, 0.15)], closed=True,
                       facecolor=WALL, edgecolor=WALL, zorder=6))
u.ax.plot([-9.6, -3.5], [0.10 - 0.25, 0.80 - 0.25], color=ODD, lw=0, zorder=6)
# Loop Door in the west wall, at the narrow end
u.ax.plot([-9.60, -9.60], [-0.75, 0.05], color=LOOP, lw=5, zorder=8, solid_capstyle="butt")
u.ax.add_patch(Circle((-8.85, -0.35), 0.36, facecolor=LOOP, edgecolor="none", zorder=11))
u.text(-8.85, -0.35, "L", 10, color="#1B2B40", weight="bold", z=12)
u.text(-11.35, -0.35, "LOOP\nDOOR", 7.5, color=LOOP)

# Landing walls
u.hwall(3.0, -3.5, 3.5, [(-1.5, 0.9), (2.0, 0.9)])
u.hwall(-3.0, -3.5, 3.5)
u.vwall(-3.5, -3.0, -0.8)
u.vwall(-3.5, 0.85, 3.0)
u.vwall(3.5, -3.0, 3.0, [(0.0, 0.9)])
# Children's room: north of the slanted wall and the landing
u.vwall(0.5, 3.0, 9.63)
u.vwall(3.5, 3.0, 9.63, [(7.5, 0.9)])     # bathroom | master bedroom (the only way in)
u.hwall(3.0, 3.5, 9.28)
u.vwall(-3.5, -9.64, -3.0)
u.hwall(-3.05, -3.5, 9.28)

# Doors
u.door(-1.95, 3.0, 0.9, 0, 90, 90)          # children's room
u.door(1.55, 3.0, 0.9, 0, 90, 90)           # bathroom
u.door(3.5, 7.05, 0.9, 0, 90, 0)            # bathroom -> master
u.door(-6.95, -0.8, 0.9, 270, 360, 270)     # sewing room
u.door(3.5, -0.45, 0.9, 0, 90, 90, color=ODD)   # study, locked
u.text(5.0, -1.15, "LOCKED", 6.5, color=ODD, weight="bold")

# Stair hole and railings
u.stairs(-0.6, 0.6, -2.2, 2.0, 15, label="UP")
u.ax.plot([-0.75, -0.75], [-2.2, 1.6], color=WALL, lw=2.5, zorder=7)
u.ax.plot([0.75, 0.75], [-2.2, 1.6], color=WALL, lw=2.5, zorder=7)
u.ax.plot([-0.75, 0.75], [-2.2, -2.2], color=WALL, lw=2.5, zorder=7)
u.text(-1.55, -1.3, "railing", 6, color=DIM)

# Furniture
u.furn(-8.6, -7.6, 7.4, 9.4, "BED", 5.5)
u.furn(-6.4, -5.4, 7.4, 9.4, "BED", 5.5)
u.furn(-9.55, -8.9, 4.0, 5.6, "TOYS", 5, rot=90)
u.furn(-2.9, -1.6, 9.0, 9.58, "DESK", 5.5)
u.ax.add_patch(Circle((-6.8, 2.2), 0.9, facecolor="none", edgecolor=DIM, lw=0.8, ls=(0, (4, 3)), zorder=4))
u.furn(0.55, 2.1, 7.6, 9.58, "BATH", 5.5, rot=90)
u.furn(2.7, 3.45, 3.1, 3.9, "WC", 5)
u.furn(2.8, 3.45, 5.0, 6.4, "SINK", 5, rot=90)
u.furn(5.4, 7.4, 7.4, 9.58, "DOUBLE\nBED", 5.5)
u.furn(8.6, 9.23, 3.2, 5.4, "WARDROBE", 5, rot=90)
u.furn(4.2, 6.6, 1.9, 2.95, "DESK", 5.5)
u.furn(8.65, 9.23, -2.8, 2.4, "FILES", 5, rot=90)
u.furn(-9.55, -8.4, -4.4, -3.4, "SEWING\nTABLE", 4.8)
u.furn(-9.55, -8.95, -8.8, -6.0, "FABRIC", 5, rot=90)
u.furn(-5.0, -4.0, -9.58, -8.9, "DRESS\nFORM", 4.6)

# Room labels
u.room(-4.6, 5.3, "CHILDREN'S ROOM", "about 70 m2", "kept as if they still sleep here", -0.85)
u.room(2.0, 5.0, "BATH\nROOM", "3.0 x 6.6", "", 0)
u.room(6.4, 5.4, "MASTER\nBEDROOM", "5.8 x 6.6 m", "one side of the bed slept in", -0.95)
u.room(6.4, -0.6, "STUDY", "5.8 x 6.0 m", "locked: the key comes later", -0.85)
u.room(-6.5, -5.8, "SEWING ROOM", "6.1 x 8.8 m", "his wife's room", -0.8)
u.text(1.9, 1.3, "LANDING", 7.5, weight="bold", bg=True)
u.text(-6.5, 0.45, "CORRIDOR narrows 1.6 -> 0.9 m", 6.3, weight="bold", bg=True)

u.oddity(-1.6, 6.3, 7)     # children's room bigger than master
u.oddity(3.0, 8.4, 8)      # master only through the bathroom
u.oddity(-4.6, -0.05, 9)   # narrowing corridor

u.route([(0.0, 2.0), (0.0, 2.55), (-2.2, 2.55), (-2.2, 0.0), (-8.2, -0.35)])

u.legend(
    {"route": "Top of the stairs to the Loop Door\n(about 11 m)", "marker": "Loop Door (one-way, to Loop 3)"},
    "7  The children's room is the biggest room\n"
    "   upstairs and spreads down to the\n"
    "   corridor: he keeps it exactly as it was,\n"
    "   beds made, toys out.\n"
    "8  The master bedroom can only be reached\n"
    "   through the bathroom.\n"
    "9  The corridor to the Loop Door narrows\n"
    "   from 1.6 m to 0.9 m, so the walls close\n"
    "   in as the player walks to it.\n\n"
    "LOOP 2 STORY (from the GCD)\n"
    "The study now has a door, but it is\n"
    "locked; in Loop 1 it was a plain wall.\n"
    "His wife's sewing room is untouched.\n"
    "The stairs arrive in the middle of the\n"
    "landing, beside the locked study.")
u.save("Left_Behind_Loop2_Upper_Blueprint.png")
