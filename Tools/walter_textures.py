"""Paints Walter's textures onto the PS1 humanoid's freshly unwrapped parts, using only the standard library.

Called by Left Behind > Walter > Build Walter (Assets/Editor/WalterModelBuilder.cs), which first writes
every part's rest-pose vertices, normals, UVs and triangles to Library/WalterMesh.json.

Each texel is painted from where it sits on the body, not from where it sits in the texture, so the
patterns carry on across UV seams. Model space: Y up, the feet at about 0.05 and the top of the head
at about 3.74, and he faces -Z (the builder turns him round). The result is Walter at seventy-two:
grey hair round a bald crown, a lined face, a brown cardigan buttoned over a pale shirt, grey slacks
and felt slippers. Colours are cut to 15 bits, like the PS1's.

  python Tools/walter_textures.py Library/WalterMesh.json Assets/Characters/Walter/Textures
"""
import json
import math
import os
import struct
import sys
import zlib

# ---------------------------------------------------------------- palette

SKIN = (192, 152, 128)
SCALP = (198, 160, 138)
HAIR = (182, 180, 174)
BROW = (205, 203, 196)
EYE_WHITE = (196, 186, 170)
IRIS = (58, 62, 66)
LID = (96, 62, 54)
LIP = (138, 84, 76)
CARDIGAN = (102, 82, 58)
CARDIGAN_RIB = (84, 66, 46)
BUTTON = (176, 160, 120)
SHIRT = (156, 166, 172)
SLACKS = (88, 86, 84)
SLIPPER = (82, 46, 40)
SOLE = (36, 30, 28)


# ---------------------------------------------------------------- helpers

def mul(c, k):
    return tuple(max(0, min(255, int(x * k))) for x in c)


def mix(a, b, t):
    t = max(0.0, min(1.0, t))
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def hash3(x, y, z):
    h = math.sin(x * 127.1 + y * 311.7 + z * 74.7) * 43758.5453
    return h - math.floor(h)


def noise(p, scale):
    """Value noise in 3D, smooth, 0..1."""
    x, y, z = p[0] / scale, p[1] / scale, p[2] / scale
    ix, iy, iz = math.floor(x), math.floor(y), math.floor(z)
    fx, fy, fz = x - ix, y - iy, z - iz
    fx, fy, fz = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy), fz * fz * (3 - 2 * fz)

    def c(a, b, d):
        return hash3(ix + a, iy + b, iz + d)

    x00 = c(0, 0, 0) + (c(1, 0, 0) - c(0, 0, 0)) * fx
    x10 = c(0, 1, 0) + (c(1, 1, 0) - c(0, 1, 0)) * fx
    x01 = c(0, 0, 1) + (c(1, 0, 1) - c(0, 0, 1)) * fx
    x11 = c(0, 1, 1) + (c(1, 1, 1) - c(0, 1, 1)) * fx
    y0 = x00 + (x10 - x00) * fy
    y1 = x01 + (x11 - x01) * fy
    return y0 + (y1 - y0) * fz


def grain(c, p, amount=10, scale=0.012):
    """The speckle every PS1 texture has."""
    return mul(c, 1 + (noise(p, scale) - 0.5) * amount / 50)


def across(p, n):
    """A coordinate that runs round the body, for vertical stripes (knit ribs, creases)."""
    return p[0] if abs(n[2]) >= abs(n[0]) else p[2]


# ---------------------------------------------------------------- the face and hair

EYE_Y = 3.425
EYE_X = 0.072


def head(p, n):
    x, y, z = p
    ax = abs(x)
    front = z < -0.12 and n[2] < -0.2
    c = SKIN

    # Hair: thin and grey round the sides and back, a bald crown, a receding front.
    crown = y > 3.6 and ax < 0.11 and z < 0.18
    sides = (ax > 0.12 or z > -0.05) and 3.24 < y < 3.64
    back = z > 0.06 and 3.16 < y < 3.64
    hairline = front and y > 3.6 and ax > 0.09
    if crown:
        c = SCALP
        if noise(p, 0.01) > 0.8:
            c = HAIR                                   # a few strands left
    elif sides or back or hairline:
        c = mix(HAIR, mul(HAIR, 0.78), noise(p, 0.008))
        if back and y < 3.22:
            c = mix(SKIN, c, (y - 3.16) / 0.06)       # thinning into the neck
    # Ears, on the sides.
    ez, ey = (z - 0.01) / 0.05, (y - 3.375) / 0.07
    if ax > 0.14 and ez * ez + ey * ey < 1:
        c = mul(SKIN, 0.94)
        if ez * ez + ey * ey > 0.6:
            c = mul(SKIN, 0.84)                        # the rim of the ear

    if front and not (sides and ax > 0.12):
        c = SKIN
        if y > 3.6 and ax > 0.09:
            c = mix(HAIR, mul(HAIR, 0.78), noise(p, 0.008))
        # Forehead lines.
        for ly in (3.525, 3.55, 3.575):
            if abs(y - ly - 0.006 * math.sin(x * 40)) < 0.0035 and ax < 0.11:
                c = mul(SKIN, 0.82)
        # Brows: bushy, white, drooping at the ends.
        bx = ax - 0.035
        if 0 < bx < 0.09 and abs(y - (3.47 - 0.12 * bx * bx / 0.09)) < 0.011:
            c = mix(BROW, mul(BROW, 0.75), noise(p, 0.006))
        # Eyes: narrow, the upper lid heavy, bags below.
        ex, ey = (ax - EYE_X) / 0.03, (y - EYE_Y) / 0.012
        if ex * ex + ey * ey < 1:
            c = EYE_WHITE
            if abs(ax - EYE_X) < 0.011:
                c = IRIS
            if y > EYE_Y + 0.004:
                c = LID
        elif abs(ax - EYE_X) < 0.034 and EYE_Y - 0.03 < y < EYE_Y - 0.014:
            c = mul(SKIN, 0.86)
        elif abs(ax - EYE_X) < 0.036 and EYE_Y + 0.012 < y < EYE_Y + 0.02:
            c = mul(SKIN, 0.8)                         # the lid's crease
        # Nose: shaded sides and nostrils.
        if ax < 0.032 and 3.3 < y < 3.405 and ax > 0.019:
            c = mul(SKIN, 0.86)
        if 0.007 < ax < 0.022 and 3.296 < y < 3.31:
            c = mul(SKIN, 0.55)
        # The lines from nose to mouth.
        t = (3.3 - y) / 0.1
        if 0 < t < 1 and abs(ax - (0.036 + 0.03 * t)) < 0.005:
            c = mul(SKIN, 0.8)
        # Mouth: thin, turned down at the corners.
        if ax < 0.05 and abs(y - (3.205 - 0.25 * ax * ax / 0.05)) < 0.0055:
            c = LIP if ax < 0.04 else mul(SKIN, 0.75)
        # Grey stubble on the jaw.
        if y < 3.19 and noise(p, 0.004) > 0.72:
            c = mix(c, HAIR, 0.5)

    if c in (SKIN, SCALP) or c == mul(SKIN, 0.92):
        if noise(p, 0.02) > 0.78:
            c = mul(c, 0.86)                           # age spots
    return grain(c, p, 6)


# ---------------------------------------------------------------- the clothes

def cardigan(p, n, rib_band=False):
    c = CARDIGAN
    if int((across(p, n) + 10) / 0.016) % 2:
        c = mul(c, 0.93)                               # knit ribs
    if rib_band:
        c = CARDIGAN_RIB if int((across(p, n) + 10) / 0.012) % 2 else mul(CARDIGAN_RIB, 0.9)
    return grain(c, p, 8, 0.006)


def body(p, n):
    x, y, z = p
    ax = abs(x)
    front = z < -0.08 and n[2] < -0.25

    if y < 0.075:
        return grain(SOLE, p, 4)
    if y < 0.24:
        c = SLIPPER
        if (int((x + 10) / 0.03) + int((z + 10) / 0.03)) % 2:
            c = mul(c, 0.88)                           # a faint felt check
        return grain(c, p, 8)
    if y < 1.72:
        c = SLACKS
        leg = ax - 0.2
        if front and abs(leg) < 0.008:
            c = mul(c, 0.8)                            # the crease
        if y < 0.3:
            c = mul(c, 0.85)                           # turn-ups
        if 1.55 < y and front and ax < 0.012:
            c = mul(c, 0.75)                           # the fly
        return grain(c, p, 7, 0.008)
    if y > 3.01 and ax < 0.16:
        return grain(SKIN, p, 6)                       # the neck
    if y > 2.94 and ax < 0.16:
        return grain(SHIRT, p, 5)                      # the collar, round the neck only

    # The cardigan, open in a V over the shirt and buttoned down the front.
    if front:
        v = 0.018 + max(0.0, y - 2.45) * 0.36
        if ax < v:
            if y > 2.45:
                c = SHIRT
                if ax < 0.01 and y < 2.88:
                    c = mul(SHIRT, 0.86)               # shirt placket
                if y > 2.83 and ax > v - 0.06:
                    c = mul(SHIRT, 0.9)                # collar points
                return grain(c, p, 5)
        for by in (1.9, 2.08, 2.26, 2.44):
            if ax < 0.022 and abs(y - by) < 0.016:
                return BUTTON
        if abs(ax - v) < 0.014:
            return cardigan(p, n, rib_band=True)       # the front band
        if 0.12 < ax < 0.3 and 1.86 < y < 2.12:
            if abs(y - 2.12) < 0.01 or abs(ax - 0.12) < 0.006 or abs(ax - 0.3) < 0.006 or abs(y - 1.86) < 0.006:
                return mul(CARDIGAN, 0.78)             # pockets
    if y < 1.82:
        return cardigan(p, n, rib_band=True)           # the hem
    return cardigan(p, n)


def upper_arm(p, n):
    return cardigan(p, n)


def forearm(p, n):
    ax = abs(p[0])
    if ax > 1.56:
        c = SKIN
        if ax > 1.62 and noise(p, 0.015) > 0.75:
            c = mul(SKIN, 0.85)                        # age spots on the backs of his hands
        if ax > 1.62 and abs(((ax - 1.62) % 0.11) - 0.055) < 0.004 and p[1] > 2.86:
            c = mul(SKIN, 0.82)                        # knuckles
        return grain(c, p, 6)
    if ax > 1.47:
        return cardigan(p, n, rib_band=True)           # the cuff
    return cardigan(p, n)


PAINTERS = {
    "Body": body,
    "Head": head,
    "UpperArmL": upper_arm,
    "UpperArmR": upper_arm,
    "ForearmL": forearm,
    "ForearmR": forearm,
}


# ---------------------------------------------------------------- rasterising

def paint(part, data):
    size = data["size"]
    v, nrm, uv, t = data["v"], data["n"], data["uv"], data["t"]
    px = [None] * (size * size)
    fn = PAINTERS[part]
    for i in range(0, len(t), 3):
        a, b, c = t[i], t[i + 1], t[i + 2]
        pa = (uv[a][0] * size, (1 - uv[a][1]) * size)
        pb = (uv[b][0] * size, (1 - uv[b][1]) * size)
        pc = (uv[c][0] * size, (1 - uv[c][1]) * size)
        den = (pb[1] - pc[1]) * (pa[0] - pc[0]) + (pc[0] - pb[0]) * (pa[1] - pc[1])
        if abs(den) < 1e-12:
            continue
        x0 = max(0, int(math.floor(min(pa[0], pb[0], pc[0]))) - 1)
        x1 = min(size - 1, int(math.ceil(max(pa[0], pb[0], pc[0]))) + 1)
        y0 = max(0, int(math.floor(min(pa[1], pb[1], pc[1]))) - 1)
        y1 = min(size - 1, int(math.ceil(max(pa[1], pb[1], pc[1]))) + 1)
        for py in range(y0, y1 + 1):
            for qx in range(x0, x1 + 1):
                sx, sy = qx + 0.5, py + 0.5
                w0 = ((pb[1] - pc[1]) * (sx - pc[0]) + (pc[0] - pb[0]) * (sy - pc[1])) / den
                w1 = ((pc[1] - pa[1]) * (sx - pc[0]) + (pa[0] - pc[0]) * (sy - pc[1])) / den
                w2 = 1 - w0 - w1
                if w0 < -0.02 or w1 < -0.02 or w2 < -0.02:
                    continue
                pos = tuple(v[a][k] * w0 + v[b][k] * w1 + v[c][k] * w2 for k in range(3))
                n = [nrm[a][k] * w0 + nrm[b][k] * w1 + nrm[c][k] * w2 for k in range(3)]
                ln = math.sqrt(sum(q * q for q in n)) or 1
                px[py * size + qx] = fn(pos, tuple(q / ln for q in n))
    # Bleed each chart a few texels outwards so the seams never show the background.
    for _ in range(4):
        grown = list(px)
        for i, c in enumerate(px):
            if c is not None:
                continue
            x, y = i % size, i // size
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                if 0 <= x + dx < size and 0 <= y + dy < size and px[(y + dy) * size + x + dx] is not None:
                    grown[i] = px[(y + dy) * size + x + dx]
                    break
        px = grown
    fill = (60, 50, 40)
    return size, [tuple((ch >> 3) << 3 for ch in (c or fill)) for c in px]


def write_png(path, size, px):
    raw = bytearray()
    for y in range(size):
        raw.append(0)
        for x in range(size):
            raw.extend(px[y * size + x])

    def chunk(tag, body):
        return struct.pack(">I", len(body)) + tag + body + struct.pack(">I", zlib.crc32(tag + body) & 0xFFFFFFFF)

    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n")
        f.write(chunk(b"IHDR", struct.pack(">IIBBBBB", size, size, 8, 2, 0, 0, 0)))
        f.write(chunk(b"IDAT", zlib.compress(bytes(raw), 9)))
        f.write(chunk(b"IEND", b""))


def main():
    src, out = sys.argv[1], sys.argv[2]
    with open(src) as f:
        parts = json.load(f)
    os.makedirs(out, exist_ok=True)
    for part, data in parts.items():
        size, px = paint(part, data)
        write_png(os.path.join(out, f"Walter_{part}.png"), size, px)
        print(f"Walter_{part}.png {size}x{size}")


if __name__ == "__main__":
    main()
