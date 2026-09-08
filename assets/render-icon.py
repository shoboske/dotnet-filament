#!/usr/bin/env python3
"""Regenerate assets/icon.png (the NuGet package icon) from assets/logo.svg.

NuGet's PackageIcon takes a raster image, so the SVG that is the real source of the mark has
to be rasterized somewhere. This does it with headless Chromium and the standard library
only — no ImageMagick, no cairosvg, no Pillow to install first.

    python3 assets/render-icon.py            # uses $CHROME, else the first chromium on PATH

It renders at 4x and box-downsamples to 128x128, which is both what NuGet recommends and
sharper than asking the browser for 128px directly.
"""

import os
import shutil
import struct
import subprocess
import sys
import tempfile
import zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parent
SIZE = 128
SCALE = 4  # Supersampling factor. SIZE * SCALE is what Chromium actually paints.

# Headless Chromium sizes the *window*, not the viewport, so the page is short by whatever
# the browser chrome would have occupied and the bottom of the image comes back transparent.
# Asking for extra height and cropping is more robust than trying to predict that number.
CHROME_UI_SLACK = 200


def find_chrome() -> str:
    if env := os.environ.get("CHROME"):
        return env
    for name in ("chromium", "chromium-browser", "google-chrome", "chrome"):
        if path := shutil.which(name):
            return path
    sys.exit("No Chromium found. Set CHROME=/path/to/chrome and re-run.")


def read_png(path: Path) -> tuple[int, int, bytearray]:
    """Decode an 8-bit RGBA PNG into (width, height, pixels). Enough for Chromium's output."""
    data = path.read_bytes()
    pos, idat = 8, b""
    width = height = 0
    while pos < len(data):
        (length,) = struct.unpack(">I", data[pos:pos + 4])
        kind, payload = data[pos + 4:pos + 8], data[pos + 8:pos + 8 + length]
        if kind == b"IHDR":
            width, height, depth, color = struct.unpack(">IIBB", payload[:10])
            if (depth, color) != (8, 6):
                sys.exit(f"Expected 8-bit RGBA, got depth={depth} color-type={color}.")
        elif kind == b"IDAT":
            idat += payload
        pos += 12 + length

    raw, stride = zlib.decompress(idat), width * 4
    pixels, prior, at = bytearray(), bytearray(stride), 0
    for _ in range(height):
        filter_type, at = raw[at], at + 1
        line, at = bytearray(raw[at:at + stride]), at + stride
        for x in range(stride):
            left = line[x - 4] if x >= 4 else 0
            up = prior[x]
            up_left = prior[x - 4] if x >= 4 else 0
            if filter_type == 1:
                line[x] = (line[x] + left) & 0xFF
            elif filter_type == 2:
                line[x] = (line[x] + up) & 0xFF
            elif filter_type == 3:
                line[x] = (line[x] + (left + up) // 2) & 0xFF
            elif filter_type == 4:
                estimate = left + up - up_left
                dl, du, dul = (abs(estimate - v) for v in (left, up, up_left))
                nearest = left if dl <= du and dl <= dul else (up if du <= dul else up_left)
                line[x] = (line[x] + nearest) & 0xFF
        pixels += line
        prior = line
    return width, height, pixels


def downsample(width: int, pixels: bytearray) -> bytearray:
    """Box-average SCALE x SCALE blocks, premultiplying so transparent pixels carry no color."""
    out = bytearray()
    for y in range(SIZE):
        for x in range(SIZE):
            r = g = b = a = 0
            for dy in range(SCALE):
                row = (y * SCALE + dy) * width * 4
                for dx in range(SCALE):
                    i = row + (x * SCALE + dx) * 4
                    alpha = pixels[i + 3]
                    r += pixels[i] * alpha
                    g += pixels[i + 1] * alpha
                    b += pixels[i + 2] * alpha
                    a += alpha
            out += bytes((r // a, g // a, b // a, a // (SCALE * SCALE)) if a else (0, 0, 0, 0))
    return out


def write_png(path: Path, pixels: bytearray) -> None:
    raw = b"".join(b"\x00" + bytes(pixels[y * SIZE * 4:(y + 1) * SIZE * 4]) for y in range(SIZE))

    def chunk(kind: bytes, payload: bytes) -> bytes:
        body = kind + payload
        return struct.pack(">I", len(payload)) + body + struct.pack(">I", zlib.crc32(body))

    path.write_bytes(
        b"\x89PNG\r\n\x1a\n"
        + chunk(b"IHDR", struct.pack(">IIBBBBB", SIZE, SIZE, 8, 6, 0, 0, 0))
        + chunk(b"IDAT", zlib.compress(raw, 9))
        + chunk(b"IEND", b"")
    )


def main() -> None:
    painted = SIZE * SCALE
    with tempfile.TemporaryDirectory() as tmp:
        page = Path(tmp) / "render.html"
        page.write_text(
            "<style>html,body{margin:0;padding:0;background:transparent}"
            f"svg{{display:block;width:{painted}px;height:{painted}px}}</style>\n"
            + (ROOT / "logo.svg").read_text()
        )
        shot = Path(tmp) / "shot.png"
        subprocess.run(
            [find_chrome(), "--headless", "--no-sandbox", "--disable-gpu", "--hide-scrollbars",
             "--default-background-color=00000000",
             f"--window-size={painted},{painted + CHROME_UI_SLACK}",
             f"--screenshot={shot}", page.as_uri()],
            check=True, capture_output=True,
        )
        width, height, pixels = read_png(shot)

    if width < painted or height < painted:
        sys.exit(f"Chromium painted {width}x{height}, need at least {painted}x{painted}.")

    icon = ROOT / "icon.png"
    write_png(icon, downsample(width, pixels))
    print(f"Wrote {icon.relative_to(ROOT.parent)} ({SIZE}x{SIZE}, {icon.stat().st_size} bytes)")


if __name__ == "__main__":
    main()
