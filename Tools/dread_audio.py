"""Generates the sounds for the dread pass (room tone, breathing, a branch tapping, a failing bulb).

  Assets/Resources/Audio/RoomTone.wav   the empty house: a low rumble and a faint mains hum (loops seamlessly)
  Assets/Resources/Audio/Breathing.wav  one of Walter's breaths, in and out, shaky (loops seamlessly)
  Assets/Resources/Audio/Tap.wav        a twig knocking on a window pane
  Assets/Resources/Audio/LampBuzz.wav   a bulb buzzing as it fails

Needs NumPy. Run from the repository root:  python Tools/dread_audio.py
"""
import os
import wave

import numpy as np

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
AUDIO = os.path.join(ROOT, "Assets", "Resources", "Audio")
RATE = 22050
rng = np.random.default_rng(1989)


def save(name, x, peak=0.8):
    x = np.asarray(x, dtype=np.float64)
    x = x / max(1e-9, np.max(np.abs(x))) * peak
    data = (np.clip(x, -1, 1) * 32767).astype("<i2").tobytes()
    with wave.open(os.path.join(AUDIO, name), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(data)
    print("wrote", name, f"{len(x) / RATE:.2f} s")


def band(x, lo, hi):
    """Band-pass by zeroing FFT bins outside [lo, hi] Hz (with soft edges)."""
    f = np.fft.rfftfreq(len(x), 1 / RATE)
    X = np.fft.rfft(x)
    gain = np.clip((f - lo * 0.7) / (lo * 0.3 + 1e-9), 0, 1) * np.clip((hi * 1.3 - f) / (hi * 0.3), 0, 1)
    return np.fft.irfft(X * gain, len(x))


def loop_seam(x, fade):
    """Crossfades the tail into the head, so the clip loops without a click."""
    n = int(fade * RATE)
    head, body, tail = x[:n], x[n:-n], x[-n:]
    k = np.linspace(0, 1, n)
    return np.concatenate([tail * (1 - k) + head * k, body])


def room_tone():
    n = int(24 * RATE)
    t = np.arange(n) / RATE
    brown = np.cumsum(rng.standard_normal(n))
    brown -= np.convolve(brown, np.ones(4001) / 4001, mode="same")      # remove the drift
    rumble = band(brown, 25, 220)
    air = band(rng.standard_normal(n), 200, 2500) * 0.05
    hum = sum(np.sin(2 * np.pi * 50 * h * t + h) / h ** 1.6 for h in (1, 2, 3, 4)) * 0.18
    swell = 0.8 + 0.2 * np.sin(2 * np.pi * t / 12.0)                    # the house breathing, very slowly
    x = (rumble / np.std(rumble) * 0.6 + air / np.std(air) * 0.05 + hum) * swell
    return loop_seam(x, 2.0)


def breathing():
    # In (1.5 s), a catch, out (1.9 s), a pause: about 4.3 s, an old man's shaky breath.
    def breath(length, lo, hi, shape, shake):
        n = int(length * RATE)
        k = np.linspace(0, 1, n)
        env = np.sin(np.pi * k ** shape) ** 1.4
        tremor = 1 + shake * np.sin(2 * np.pi * 6.5 * k * length + rng.uniform(0, 6))
        rasp = 1 + 0.25 * np.sin(2 * np.pi * 38 * k * length)
        return band(rng.standard_normal(n), lo, hi) * env * tremor * rasp

    inhale = breath(1.5, 900, 3200, 0.8, 0.35) * 0.8
    gap = np.zeros(int(0.18 * RATE))
    exhale = breath(1.9, 350, 1600, 1.3, 0.2)
    rest = np.zeros(int(0.7 * RATE))
    x = np.concatenate([inhale, gap, exhale, rest])
    return x


def tap():
    # Three knocks of a twig on glass, the last one softer.
    out = np.zeros(int(1.4 * RATE))
    for at, vol in ((0.0, 1.0), (0.32, 0.8), (0.58, 0.55)):
        n = int(0.12 * RATE)
        k = np.arange(n) / RATE
        ring = np.sin(2 * np.pi * 2350 * k) * np.exp(-k * 90) + 0.6 * np.sin(2 * np.pi * 3900 * k) * np.exp(-k * 140)
        click = band(rng.standard_normal(n), 1500, 6000) * np.exp(-k * 400)
        thump = np.sin(2 * np.pi * 140 * k) * np.exp(-k * 60) * 0.5
        s = int(at * RATE)
        out[s:s + n] += (ring * 0.5 + click + thump) * vol
    return out


def lamp_buzz():
    n = int(1.2 * RATE)
    t = np.arange(n) / RATE
    buzz = sum(np.sign(np.sin(2 * np.pi * 100 * h * t)) / h ** 1.2 for h in (1, 2, 3)) * 0.3
    buzz += band(rng.standard_normal(n), 2000, 7000) * 0.15
    # Sputtering: the current comes and goes.
    gate = (rng.random(n // 300 + 1) > 0.35).astype(float).repeat(300)[:n]
    gate = np.convolve(gate, np.ones(40) / 40, mode="same")
    env = np.minimum(1, t / 0.02) * np.minimum(1, (t[-1] - t) / 0.08)
    return buzz * gate * env


if __name__ == "__main__":
    save("RoomTone.wav", room_tone(), 0.6)
    save("Breathing.wav", breathing(), 0.7)
    save("Tap.wav", tap(), 0.8)
    save("LampBuzz.wav", lamp_buzz(), 0.5)
