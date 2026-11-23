"""Figures for the Assignment 3 part of the Left Behind GDD.

Writes into Blueprints/GDD_Figures:
  fig_characters.png  visual references for the four characters (in-game captures)
  fig_ui_layout.png   HUD, dialogue box, choice prompt and Pockets screen layout
  fig_story_flow.png  story flow diagram from the main menu to both endings
  fig_art.png         art samples: one capture per loop mood plus the rewind effect
"""
import os

import matplotlib

matplotlib.use("Agg")
import matplotlib.image as mpimg
import matplotlib.pyplot as plt
from matplotlib.patches import FancyBboxPatch, Rectangle, Polygon, Circle

HERE = os.path.dirname(os.path.abspath(__file__))
SHOTS = os.path.join(HERE, "..", "Screenshots")
OUT = os.path.join(HERE, "GDD_Figures")
os.makedirs(OUT, exist_ok=True)
FONT = "DejaVu Sans"
INK = "#1E1E1E"
RED = "#7A1F1F"


def shot(name):
    return mpimg.imread(os.path.join(SHOTS, name))


def crop_169(img):
    """Crop any capture to 16:9 around its centre so the panels line up."""
    h, w = img.shape[:2]
    th = int(w * 9 / 16)
    if th <= h:
        y0 = (h - th) // 2
        return img[y0:y0 + th]
    tw = int(h * 16 / 9)
    x0 = (w - tw) // 2
    return img[:, x0:x0 + tw]


def panel_grid(items, rows, cols, out, title=None, width=12):
    fig, axes = plt.subplots(rows, cols, figsize=(width, width * rows / cols * 9 / 16 + 0.9 * rows), dpi=150)
    fig.patch.set_facecolor("white")
    for ax, (img, head, body) in zip(axes.flat, items):
        ax.imshow(crop_169(shot(img)))
        ax.set_xticks([])
        ax.set_yticks([])
        for s in ax.spines.values():
            s.set_color("#999999")
        ax.set_title(head, fontsize=12, fontweight="bold", color=INK, fontfamily=FONT, loc="left", pad=6)
        ax.text(0, -0.04, body, transform=ax.transAxes, fontsize=9.5, color="#444444", fontfamily=FONT,
                va="top", ha="left", wrap=True)
    if title:
        fig.suptitle(title, fontsize=13, fontweight="bold", fontfamily=FONT, color=INK)
    fig.tight_layout(h_pad=3.2, w_pad=1.6)
    fig.savefig(os.path.join(OUT, out), facecolor="white")
    plt.close(fig)
    print("saved", out)


# ---------------------------------------------------------------------------- characters
panel_grid([
    ("furniture_dining_solo.png", "Walter Hale (the player)",
     "One chair and one place setting: how he lives in the present."),
    ("inspect_1_close.png", "The family photograph",
     "Inspected up close. In Loop 1 the faces are blurred out."),
    ("loop2b_dining.png", "Ruth Hale",
     "Loop 2: he still sets her table, for six. There were only four of them."),
    ("loop2b_kids.png", "Emily and Sam Hale",
     "Loop 2: the children's room, beds made, kept as if they still sleep here."),
], 2, 2, "fig_characters.png")

# ---------------------------------------------------------------------------- art samples
panel_grid([
    ("exterior_from_master.png", "Loop 1: present day",
     "Grey winter light, fog, bare trees. Cold and empty."),
    ("loop2b_landing.png", "Loop 2: years earlier",
     "Warm lamplight; the landing narrows towards the Loop Door."),
    ("loop_transition_d_rewind.png", "The loop transition",
     "A VHS rewind over the last frame: tearing, tracking bars, static."),
    ("flashlight_3_room.png", "PS1/PS2 rendering",
     "40% resolution, point-filtered textures, 32-level colour with dither."),
], 2, 2, "fig_art.png")

# ---------------------------------------------------------------------------- UI layout
fig = plt.figure(figsize=(12, 6.0), dpi=150)
fig.patch.set_facecolor("white")


def screen(ax, title):
    ax.set_xlim(0, 16)
    ax.set_ylim(0, 9)
    ax.set_aspect("equal")
    ax.set_xticks([])
    ax.set_yticks([])
    ax.add_patch(Rectangle((0, 0), 16, 9, facecolor="#2B2622", edgecolor="#777777", lw=1.5))
    ax.set_title(title, fontsize=12, fontweight="bold", loc="left", fontfamily=FONT, color=INK)


def label(ax, x, y, n, text, ha="left"):
    ax.add_patch(Circle((x, y), 0.32, facecolor="#F7C948", edgecolor="none", zorder=5))
    ax.text(x, y, str(n), ha="center", va="center", fontsize=9, fontweight="bold", color=INK, zorder=6)


a1 = fig.add_axes([0.02, 0.33, 0.47, 0.62])
screen(a1, "In-game HUD and dialogue")
a1.text(0.5, 8.4, "Find Ruth.", color="#E8E2D6", fontsize=11, style="italic", fontfamily=FONT, va="center")
label(a1, 0.35, 7.7, 1, "")
a1.add_patch(Circle((8, 4.5), 0.06, facecolor="white", edgecolor="none"))
a1.text(8, 3.9, "[E] Read the note", color="#E8E2D6", fontsize=9, ha="center", fontfamily=FONT)
label(a1, 9.9, 4.2, 2, "")
a1.add_patch(Rectangle((2.2, 0.45), 11.6, 2.0, facecolor="#000000", alpha=0.55, edgecolor="none"))
a1.add_patch(Rectangle((2.45, 0.7), 1.5, 1.5, facecolor="#8A7F72", edgecolor="#CFC6B8", lw=1))
a1.text(3.2, 1.45, "portrait", ha="center", va="center", fontsize=7, color="#2B2622")
a1.text(4.2, 2.05, "RUTH", color="#E8C28A", fontsize=10, fontweight="bold", fontfamily=FONT, va="center")
a1.text(4.2, 1.35, "Walter, dinner's ready. Wash your hands.", color="#E8E2D6", fontsize=9.5,
        fontfamily=FONT, va="center")
a1.text(4.2, 0.85, "(remembered voice)", color="#A89F92", fontsize=8, style="italic", fontfamily=FONT)
label(a1, 2.25, 2.6, 3, "")
a1.add_patch(Rectangle((12.4, 7.6), 3.3, 1.1, facecolor="#000000", alpha=0.4, edgecolor="none"))
a1.text(14.05, 8.15, "AUTUMN. YEARS EARLIER.", color="#E8E2D6", fontsize=7, ha="center", fontfamily=FONT)
label(a1, 12.2, 7.4, 4, "")

a2 = fig.add_axes([0.51, 0.33, 0.47, 0.62])
screen(a2, "Pockets screen (Tab)")
a2.add_patch(Rectangle((0.6, 0.6), 14.8, 7.8, facecolor="#D9CFBE", edgecolor="#8A7F72", lw=1.5))
a2.text(1.1, 7.85, "POCKETS", fontsize=11, fontweight="bold", color=INK, fontfamily=FONT)
a2.text(4.2, 7.85, "NOTES", fontsize=11, color="#7A6F62", fontfamily=FONT)
for i, name in enumerate(["Flashlight", "Spare fuse", "Study key", "Photograph"]):
    x = 1.1 + (i % 2) * 2.6
    y = 5.6 - (i // 2) * 2.6
    a2.add_patch(Rectangle((x, y), 2.2, 2.0, facecolor="#EFE7DA", edgecolor="#8A7F72", lw=1))
    a2.text(x + 1.1, y + 0.3, name, ha="center", fontsize=8, color=INK, fontfamily=FONT)
a2.add_patch(Rectangle((1.1, 5.6), 2.2, 2.0, facecolor="none", edgecolor=RED, lw=2.2))
a2.text(6.6, 7.0, "Flashlight", fontsize=12, fontweight="bold", color=INK, fontfamily=FONT)
a2.text(6.6, 6.2, "Ruth bought it for the power cuts.\nShe always hated the dark.\nThe switch sticks. I keep meaning\nto fix it.",
        fontsize=9.5, color=INK, fontfamily=FONT, va="top", style="italic", linespacing=1.4)
label(a2, 6.3, 7.7, 5, "")
a2.text(6.6, 2.0, "Esc  close     E  look closer", fontsize=8.5, color="#5A5045", fontfamily=FONT)

legend = ("1  Objective line: the current story objective, written as his own reminder.\n"
          "2  Crosshair and interaction prompt: what can be used, read or picked up.\n"
          "3  Dialogue box: speaker name, portrait cut from the family photo, typewriter text,\n"
          "    and the two choices at the climax.\n"
          "4  Status: the period card (season and time) on arrival and in the pause menu.\n"
          "5  Pockets: carried items with descriptions in his voice; Notes keeps the notes read.")
fig.text(0.03, 0.29, legend, fontsize=10, fontfamily=FONT, color=INK, va="top", linespacing=1.55)
fig.savefig(os.path.join(OUT, "fig_ui_layout.png"), facecolor="white")
plt.close(fig)
print("saved fig_ui_layout.png")

# ---------------------------------------------------------------------------- story flow
fig = plt.figure(figsize=(12, 15), dpi=150)
ax = fig.add_axes([0, 0, 1, 1])
ax.set_xlim(0, 12)
ax.set_ylim(0, 15)
ax.axis("off")
fig.patch.set_facecolor("white")
LEVEL_COL = {"menu": "#E8E8E8", "l1": "#DCE6F2", "l2": "#F6E3C8", "l3": "#EBCFCF", "end": "#E2E2E2"}


def node(x, y, text, kind, w=4.6, h=0.62, flag=None):
    ax.add_patch(FancyBboxPatch((x - w / 2, y - h / 2), w, h, boxstyle="round,pad=0.02,rounding_size=0.12",
                                facecolor=LEVEL_COL[kind], edgecolor="#555555", lw=1.1))
    ax.text(x, y + (0.08 if flag else 0), text, ha="center", va="center", fontsize=9.6, fontfamily=FONT, color=INK)
    if flag:
        ax.text(x, y - 0.19, flag, ha="center", va="center", fontsize=7.8, fontfamily="DejaVu Sans Mono",
                color=RED)


def diamond(x, y, text, w=3.4, h=1.05):
    ax.add_patch(Polygon([(x, y + h / 2), (x + w / 2, y), (x, y - h / 2), (x - w / 2, y)], closed=True,
                         facecolor="#FFF6D6", edgecolor="#555555", lw=1.1))
    ax.text(x, y, text, ha="center", va="center", fontsize=8.8, fontfamily=FONT, color=INK)


def arrow(x0, y0, x1, y1, text=None, side="right"):
    ax.annotate("", xy=(x1, y1), xytext=(x0, y0),
                arrowprops=dict(arrowstyle="-|>", color="#444444", lw=1.1, shrinkA=0, shrinkB=0))
    if text:
        ax.text((x0 + x1) / 2 + (0.12 if side == "right" else -0.12), (y0 + y1) / 2, text, fontsize=8,
                fontfamily=FONT, color="#444444", ha="left" if side == "right" else "right", va="center")


def band(y0, y1, text, kind):
    ax.add_patch(Rectangle((0.2, y0), 11.6, y1 - y0, facecolor=LEVEL_COL[kind], alpha=0.35, edgecolor="none"))
    ax.text(0.35, y1 - 0.25, text, fontsize=10.5, fontweight="bold", fontfamily=FONT, color=INK, va="top")


X = 6.0
band(13.55, 14.85, "MAIN MENU", "menu")
band(9.15, 13.45, "LEVEL 1  |  LOOP 1: PRESENT DAY", "l1")
band(4.2, 9.05, "LEVEL 2  |  LOOP 2: YEARS EARLIER", "l2")
band(0.0, 4.1, "LEVEL 3  |  LOOP 3: THE AFTERMATH", "l3")

node(X, 14.2, "Main menu: Begin / Chapters / Quit", "menu")
arrow(X, 13.89, X, 13.33)
node(X, 13.02, "Intro: title card, wakes at the front door", "l1", flag="introSeen")
arrow(X, 12.71, X, 12.33)
node(X, 12.02, "Front door rattles, will not open", "l1", flag="triedFrontDoor")
arrow(X, 11.71, X, 11.33)
node(2.95, 11.0, "Inspect the family photo", "l1", w=3.6, flag="sawFamilyPhoto")
node(9.05, 11.0, "Pick up the flashlight", "l1", w=3.6, flag="hasFlashlight")
ax.text(X, 11.0, "either order", ha="center", va="center", fontsize=8, style="italic", color="#555555")
arrow(X - 0.6, 11.33, 3.6, 11.31)
arrow(X + 0.6, 11.33, 8.4, 11.31)
arrow(2.95, 10.69, X - 0.9, 10.24)
arrow(9.05, 10.69, X + 0.9, 10.24)
diamond(X, 9.85, "Loop Door: both\nflags set?")
ax.text(X + 1.85, 9.85, "no: \"Not yet.\"", fontsize=8, va="center", color="#444444")
arrow(X, 9.32, X, 8.84, "yes: rewind + recap", "right")
node(X, 8.53, "Arrival: \"Autumn. Years earlier.\"  Ruth's voice", "l2", w=5.2, flag="loop1Complete")
arrow(X, 8.22, X, 7.84)
node(X, 7.53, "Table set for six; read the note on the fridge", "l2", w=5.2, flag="readFridgeNote")
arrow(X, 7.22, X, 6.84)
node(X, 6.53, "Spare fuse into the fuse box: lights come on upstairs", "l2", w=5.6, flag="powerRestored")
arrow(X, 6.22, X, 5.84)
node(X, 5.53, "Study key in Ruth's sewing box", "l2", w=5.2, flag="hasStudyKey")
arrow(X, 5.22, X, 4.89)
node(X, 4.58, "Open the study: a fresh brick wall behind the door", "l2", w=5.6, flag="studyBricked")
ax.text(X + 2.95, 4.58, "Loop Door\nunlocks", fontsize=8, va="center", color="#444444")
arrow(X, 4.27, X, 3.76, "rewind + recap", "right")
node(X, 3.45, "Arrival: storm, every light on; the music box plays", "l3", w=5.6, flag="loop2Complete")
arrow(X, 3.14, X, 2.81)
node(X, 2.5, "Knife block, children's room, note on the study door", "l3", w=5.6, flag="readDelusionNote")
arrow(X, 2.19, X, 2.02)
diamond(X, 1.52, "In the study:\nremember?")
node(2.4, 0.62, "Remember: examine the knife", "end", w=3.9, flag="choseToRemember = true")
node(9.6, 0.62, "Look away: walk out", "end", w=3.9, flag="choseToRemember = false")
arrow(X - 1.7, 1.52, 2.4, 0.95, "")
arrow(X + 1.7, 1.52, 9.6, 0.95, "")
ax.text(2.4, 0.12, "Ending: he remembers their names", ha="center", fontsize=8.3, color=INK)
ax.text(9.6, 0.12, "Cliffhanger: rewinds to Loop 1, again", ha="center", fontsize=8.3, color=INK)
fig.savefig(os.path.join(OUT, "fig_story_flow.png"), facecolor="white")
plt.close(fig)
print("saved fig_story_flow.png")
