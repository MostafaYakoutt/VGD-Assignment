"""Generates the small story assets for the Part 1 props, using only the standard library.

  Assets/Story/Textures/Calendar_Loop1.png   January 2025, every day crossed out
  Assets/Story/Textures/Calendar_Loop2.png   October 2006, Walter's Friday entries, the 13th circled
  Assets/Story/Textures/Calendar_Loop3.png   October 2004, Ruth's entries in blue
  Assets/Story/Textures/ClockFaceOval.png    the dining room clock face (cut out round)
  Assets/Resources/Audio/Drip.wav            one drop from the kitchen tap
  Assets/Resources/Audio/WaterRunning.wav    the tap running (loops seamlessly)

Like the other story textures, the writing is a scrawl: the readable text is in the note reader.
Run from the repository root:  python Tools/story_assets.py
"""
import math
import os
import random
import struct
import wave
import zlib

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
TEX = os.path.join(ROOT, "Assets", "Story", "Textures")
AUDIO = os.path.join(ROOT, "Assets", "Resources", "Audio")

PAPER = (226, 214, 178, 255)
INK = (40, 34, 30, 255)
BLUE = (38, 62, 120, 255)
RED = (150, 36, 30, 255)
GRID = (150, 140, 118, 255)


# ---------------------------------------------------------------- tiny raster

class Image:
    def __init__(self, w, h, fill):
        self.w, self.h = w, h
        self.px = [list(fill) for _ in range(w * h)]

    def set(self, x, y, c):
        x, y = int(round(x)), int(round(y))
        if 0 <= x < self.w and 0 <= y < self.h:
            self.px[y * self.w + x] = list(c)

    def dot(self, x, y, c, r=1.0):
        for dy in range(-int(r), int(r) + 1):
            for dx in range(-int(r), int(r) + 1):
                if dx * dx + dy * dy <= r * r + 0.25:
                    self.set(x + dx, y + dy, c)

    def line(self, x0, y0, x1, y1, c, r=1.0):
        n = int(max(abs(x1 - x0), abs(y1 - y0)) * 2) + 1
        for i in range(n + 1):
            t = i / n
            self.dot(x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, c, r)

    def rect(self, x0, y0, x1, y1, c):
        for y in range(int(y0), int(y1)):
            for x in range(int(x0), int(x1)):
                self.set(x, y, c)

    def circle(self, cx, cy, rad, c, r=1.0):
        n = int(rad * 8) + 8
        pts = [(cx + rad * math.cos(2 * math.pi * i / n), cy + rad * math.sin(2 * math.pi * i / n)) for i in range(n + 1)]
        for a, b in zip(pts, pts[1:]):
            self.line(a[0], a[1], b[0], b[1], c, r)

    def scrawl(self, x, y, width, c, rng, height=5.0, r=0.8):
        """A line of 'handwriting': little up-and-down strokes, broken into words."""
        cx = x
        while cx < x + width:
            word = rng.uniform(8, 22)
            px, py = cx, y
            end = min(cx + word, x + width)
            while px < end:
                nx = px + rng.uniform(1.5, 3.5)
                ny = y - rng.uniform(0, height) if rng.random() < 0.6 else y
                self.line(px, py, nx, ny, c, r)
                px, py = nx, ny
            cx = end + rng.uniform(4, 8)

    def save(self, path):
        raw = b"".join(b"\x00" + bytes(v for p in self.px[y * self.w:(y + 1) * self.w] for v in p) for y in range(self.h))
        def chunk(tag, data):
            return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)
        png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", self.w, self.h, 8, 6, 0, 0, 0))
        png += chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b"")
        with open(path, "wb") as f:
            f.write(png)
        print("wrote", os.path.relpath(path, ROOT))


# ---------------------------------------------------------------- calendars

W, H = 240, 320
COLS, ROWS = 7, 5
GX0, GY0, GX1, GY1 = 14, 92, 226, 306


def cell(i):
    """Top-left corner and size of grid cell i (Monday-first)."""
    cw, ch = (GX1 - GX0) / COLS, (GY1 - GY0) / ROWS
    return GX0 + (i % COLS) * cw, GY0 + (i // COLS) * ch, cw, ch


def calendar(name, first_weekday, days, header_ink, marks, seed):
    rng = random.Random(seed)
    img = Image(W, H, PAPER)
    # Picture strip at the top (a faded print), then the month name.
    img.rect(14, 12, 226, 58, (176, 168, 146, 255))
    for _ in range(140):
        x, y = rng.uniform(16, 224), rng.uniform(14, 56)
        img.dot(x, y, (150, 146, 128, 255), rng.uniform(0.5, 2.2))
    img.scrawl(70, 78, 100, header_ink, rng, height=8, r=1.3)
    for c in range(COLS + 1):
        x = GX0 + c * (GX1 - GX0) / COLS
        img.line(x, GY0, x, GY1, GRID, 0.5)
    for r in range(ROWS + 1):
        y = GY0 + r * (GY1 - GY0) / ROWS
        img.line(GX0, y, GX1, y, GRID, 0.5)
    for d in range(1, days + 1):
        i = first_weekday + d - 1
        if i >= COLS * ROWS:
            i -= COLS            # the last days share the bottom row, as printed calendars do
        x, y, cw, ch = cell(i)
        img.line(x + 3, y + 4, x + 6, y + 4, GRID, 0.5)    # the printed day number
        marks(img, d, x, y, cw, ch, rng)
    img.save(os.path.join(TEX, name + ".png"))


def loop1_marks(img, d, x, y, cw, ch, rng):
    j = lambda: rng.uniform(-2, 2)
    img.line(x + 5 + j(), y + 8 + j(), x + cw - 5 + j(), y + ch - 5 + j(), INK, 1.0)
    img.line(x + cw - 5 + j(), y + 8 + j(), x + 5 + j(), y + ch - 5 + j(), INK, 1.0)


def loop2_marks(img, d, x, y, cw, ch, rng):
    if d in (6, 20, 27):
        img.scrawl(x + 3, y + 20, cw - 6, INK, rng, height=4)
        img.scrawl(x + 3, y + 30, cw - 10, INK, rng, height=4)
    if d == 13:
        img.scrawl(x + 3, y + 22, cw - 6, RED, rng, height=4)
        img.circle(x + cw / 2, y + ch / 2 + 2, min(cw, ch) / 2 - 2, RED, 1.0)
        img.circle(x + cw / 2 + 1, y + ch / 2 + 1, min(cw, ch) / 2 - 4, RED, 0.8)


def loop3_marks(img, d, x, y, cw, ch, rng):
    if d < 13:
        img.line(x + 4, y + ch - 6, x + cw - 4, y + 8, BLUE, 0.7)          # Ruth crosses off the days gone
    if d in (1, 8, 16, 18, 19):
        img.scrawl(x + 3, y + 22, cw - 6, BLUE, rng, height=4)
    if d == 19:
        img.scrawl(x + 3, y + 32, cw - 8, BLUE, rng, height=4)
        img.line(x + 3, y + 36, x + cw - 3, y + 36, BLUE, 0.8)            # underlined


# ---------------------------------------------------------------- clock face

def clock_face():
    S = 128
    img = Image(S, S, (0, 0, 0, 0))
    c = (S - 1) / 2
    for y in range(S):
        for x in range(S):
            if (x - c) ** 2 + (y - c) ** 2 <= (c - 1) ** 2:
                img.set(x, y, (232, 226, 206, 255))
    img.circle(c, c, c - 4, (60, 50, 40, 255), 1.2)
    for h in range(12):
        a = h / 12 * 2 * math.pi
        r0 = c - (20 if h % 3 == 0 else 13)
        img.line(c + r0 * math.sin(a), c - r0 * math.cos(a), c + (c - 8) * math.sin(a), c - (c - 8) * math.cos(a),
                 (40, 34, 30, 255), 2.0 if h % 3 == 0 else 1.0)
    for m in range(60):
        if m % 5:
            a = m / 60 * 2 * math.pi
            img.dot(c + (c - 9) * math.sin(a), c - (c - 9) * math.cos(a), (90, 80, 70, 255), 0.6)
    img.save(os.path.join(TEX, "ClockFaceOval.png"))


# ---------------------------------------------------------------- sounds

RATE = 44100


def write_wav(name, samples):
    path = os.path.join(AUDIO, name)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1.0, min(1.0, s)) * 32000)) for s in samples))
    print("wrote", os.path.relpath(path, ROOT))


def drip():
    """A drop landing in a steel sink: a bubble 'plink' that rises in pitch and dies quickly."""
    rng = random.Random(3)
    n = int(RATE * 0.3)
    out, phase = [], 0.0
    for i in range(n):
        t = i / RATE
        f = 780 + 1100 * (1 - math.exp(-t / 0.018))
        phase += 2 * math.pi * f / RATE
        s = 0.8 * math.sin(phase) * math.exp(-t / 0.045)
        if t < 0.004:
            s += rng.uniform(-0.5, 0.5) * (1 - t / 0.004)      # the tick of the impact
        s += 0.12 * math.sin(phase * 2.3) * math.exp(-t / 0.02)
        out.append(s * 0.7)
    write_wav("Drip.wav", out)


def running_water(seconds=4.0, fade=0.5):
    """Band-limited noise with a slow, uneven swell, cross-faded so it loops without a seam."""
    rng = random.Random(7)
    total = int(RATE * (seconds + fade))
    lp1 = lp2 = hp = prev = 0.0
    raw = []
    swell = 0.0
    for i in range(total):
        x = rng.uniform(-1, 1)
        lp1 += 0.35 * (x - lp1)
        lp2 += 0.35 * (lp1 - lp2)           # gentle low-pass
        hp = 0.97 * (hp + lp2 - prev)       # and high-pass: a hiss without rumble
        prev = lp2
        if i % 2205 == 0:
            swell = rng.uniform(0.75, 1.0)
        raw.append(hp * swell)
    n, f = int(RATE * seconds), int(RATE * fade)
    out = raw[:n]
    for i in range(f):                      # fade the tail into the head
        a = i / f
        out[i] = raw[n + i] * (1 - a) + raw[i] * a
    peak = max(abs(s) for s in out)
    write_wav("WaterRunning.wav", [s / peak * 0.6 for s in out])


if __name__ == "__main__":
    # Weekdays Monday = 0. 1 Jan 2025 was a Wednesday, 1 Oct 2006 a Sunday, 1 Oct 2004 a Friday.
    calendar("Calendar_Loop1", 2, 31, INK, loop1_marks, 1)
    calendar("Calendar_Loop2", 6, 31, INK, loop2_marks, 2)
    calendar("Calendar_Loop3", 4, 31, BLUE, loop3_marks, 3)
    clock_face()
    drip()
    running_water()
