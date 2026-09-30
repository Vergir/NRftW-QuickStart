"""Builds the 1920x1080 Nexus / README images in docs/pics/nexus from the raw screenshots in docs/pics.

Raw screenshots (git-ignored): logo.png (a frame of the intro video), menu.png, loading.png, game.png, settings3.png.
They were taken on a 9:8 monitor with the game at 16:9, so the game area sits between black bars; crop16x9 finds it.
Needs Pillow. Usage: python make_nexus_images.py
"""
import os
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont, ImageOps

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "nexus")
FONT = "C:/Windows/Fonts/segoeuib.ttf"
FONT_LIGHT = "C:/Windows/Fonts/segoeui.ttf"
WHITE, GOLD, GREY, RED = (255, 255, 255), (216, 201, 163), (187, 187, 187), (214, 76, 60)
W, H = 1920, 1080


def font(size, bold=True):
    return ImageFont.truetype(FONT if bold else FONT_LIGHT, size)


def load(name):
    return Image.open(os.path.join(HERE, name)).convert("RGB")


def crop16x9(im, focus_y=None):
    """The 16:9 game area: full width; vertically the first non-black row, or centred on focus_y (0..1) for all-black frames."""
    w, h = im.size
    ch = round(w * 9 / 16)
    if focus_y is None:
        gray = im.convert("L")
        top = next((y for y in range(h) if gray.crop((0, y, w, y + 1)).getextrema()[1] > 24), 0)
        top = min(top, h - ch)
    else:
        top = max(0, min(h - ch, round(focus_y * h - ch / 2)))
    return im.crop((0, top, w, top + ch))


def centered(draw, xy, text, fnt, fill):
    x, y = xy
    l, t, r, b = draw.textbbox((0, 0), text, font=fnt)
    draw.text((x - (r - l) / 2 - l, y), text, font=fnt, fill=fill)


def card(im, size, skipped=False):
    """A screenshot tile with a thin border; skipped tiles are grey, dark and struck through."""
    tile = im.resize(size, Image.LANCZOS)
    if skipped:
        tile = ImageEnhance.Brightness(ImageOps.grayscale(tile).convert("RGB")).enhance(0.35)
    d = ImageDraw.Draw(tile)
    if skipped:
        d.line((14, size[1] - 14, size[0] - 14, 14), fill=RED, width=6)
        f = font(34)
        l, t, r, b = d.textbbox((0, 0), "SKIPPED", font=f)
        bw, bh = r - l + 28, b - t + 18
        bx, by = (size[0] - bw) // 2, (size[1] - bh) // 2
        d.rectangle((bx, by, bx + bw, by + bh), fill=(20, 20, 20), outline=RED, width=3)
        d.text((bx + 14 - l, by + 9 - t), "SKIPPED", font=f, fill=RED)
    d.rectangle((0, 0, size[0] - 1, size[1] - 1), outline=(90, 90, 90) if skipped else GOLD, width=2)
    return tile


def arrow(draw, x, y, color):
    draw.line((x, y, x + 26, y), fill=color, width=5)
    draw.polygon([(x + 26, y - 11), (x + 40, y), (x + 26, y + 11)], fill=color)


def cover(shots):
    bg = ImageEnhance.Brightness(shots["game"].resize((W, H), Image.LANCZOS).filter(ImageFilter.GaussianBlur(14))).enhance(0.28)
    d = ImageDraw.Draw(bg)
    centered(d, (W / 2, 44), "Quick Start", font(92), WHITE)
    centered(d, (W / 2, 168), "Launch the game, land in your realm", font(42, bold=False), GOLD)

    steps = [("logo", "Logo video"), ("menu", "Main menu: click Continue"), ("loading", "Loading"), ("game", "In game")]
    cw, ch, gap, label_w = 330, 186, 56, 300
    x0 = (W - (label_w + 4 * cw + 3 * gap)) // 2 + label_w
    for row, (title, y, skip) in enumerate([("Vanilla Game", 320, set()), ("Quick Start", 650, {"logo", "menu"})]):
        d.text((x0 - label_w, y + ch / 2 - 26), title, font=font(40), fill=WHITE if row == 0 else GOLD)
        for i, (key, caption) in enumerate(steps):
            x = x0 + i * (cw + gap)
            skipped = key in skip
            bg.paste(card(shots[key], (cw, ch), skipped), (x, y))
            if skipped and key == "menu":
                caption = "no clicks"
            centered(d, (x + cw / 2, y + ch + 14), caption, font(28, bold=False), (130, 130, 130) if skipped else GREY)
            if i < 3:
                arrow(d, x + cw + 8, y + ch / 2, (110, 110, 110) if skipped else GOLD)
    bg.save(os.path.join(OUT, "cover.jpg"), quality=92)


# Highlight boxes on the settings screenshot, as fractions of the 16:9 game area (left, top, right, bottom):
# our three rows at the end of Options > Gameplay, and the description panel on the right (only when the screenshot
# was taken with the mouse over one of our rows, so the panel shows its text).
SETTINGS_ROWS = (0.085, 0.750, 0.505, 0.912)
SETTINGS_DESC = None  # e.g. (0.54, 0.17, 0.99, 0.45)


def header(shots):
    """Nexus page header (1300x372): title on the left, the Quick Start timeline on the right, over a dimmed game shot."""
    hw, hh = 1300, 372
    game = shots["game"]
    band = game.crop((0, int(game.height * 0.30), game.width, int(game.height * 0.30 + game.width * hh / hw)))
    bg = ImageEnhance.Brightness(band.resize((hw, hh), Image.LANCZOS).filter(ImageFilter.GaussianBlur(6))).enhance(0.3)
    d = ImageDraw.Draw(bg)
    d.text((44, 108), "Quick Start", font=font(64), fill=WHITE)
    d.text((47, 196), "Launch the game,", font=font(28, bold=False), fill=GOLD)
    d.text((47, 234), "land in your realm", font=font(28, bold=False), fill=GOLD)

    steps = [("logo", "Logo video", True), ("menu", "Main menu", True), ("loading", "Loading", False), ("game", "In game", False)]
    cw, ch, gap = 176, 99, 44
    x0, y = hw - 36 - (4 * cw + 3 * gap), (hh - ch) // 2 - 14
    for i, (key, caption, skipped) in enumerate(steps):
        x = x0 + i * (cw + gap)
        tile = card(shots[key], (cw * 2, ch * 2), skipped).resize((cw, ch), Image.LANCZOS)  # stamp drawn at 2x, then scaled
        bg.paste(tile, (x, y))
        centered(d, (x + cw / 2, y + ch + 10), caption, font(22, bold=False), (130, 130, 130) if skipped else GREY)
        if i < 3:
            ax, ay = x + cw + 6, y + ch / 2
            color = (110, 110, 110) if skipped else GOLD
            d.line((ax, ay, ax + 20, ay), fill=color, width=4)
            d.polygon([(ax + 20, ay - 8), (ax + 31, ay), (ax + 20, ay + 8)], fill=color)
    bg.save(os.path.join(OUT, "header.jpg"), quality=92)


def settings(shots):
    """Options > Gameplay, full screen, dimmed except our rows (and their description), with a gold frame."""
    full = shots["settings"].resize((W, H), Image.LANCZOS)
    canvas = ImageEnhance.Brightness(full).enhance(0.35)
    d = ImageDraw.Draw(canvas)
    for box in filter(None, [SETTINGS_ROWS, SETTINGS_DESC]):
        l, t, r, b = int(box[0] * W), int(box[1] * H), int(box[2] * W), int(box[3] * H)
        canvas.paste(full.crop((l, t, r, b)), (l, t))
        d.rectangle((l - 4, t - 4, r + 4, b + 4), outline=GOLD, width=4)
    canvas.save(os.path.join(OUT, "settings.jpg"), quality=92)


def main():
    os.makedirs(OUT, exist_ok=True)
    shots = {
        "logo": crop16x9(load("logo.png"), focus_y=0.49),
        "menu": crop16x9(load("menu.png")),
        "loading": crop16x9(load("loading.png")),
        "game": crop16x9(load("game.png")),
        "settings": crop16x9(load("settings3.png")),
    }
    for k, v in shots.items():
        print(f"{k:9} {v.size}")
    cover(shots)
    header(shots)
    settings(shots)
    for f in sorted(os.listdir(OUT)):
        print("->", os.path.join("nexus", f))


if __name__ == "__main__":
    main()
