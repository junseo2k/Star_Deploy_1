from pathlib import Path
from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
RESOURCE = ROOT / "Assets/Delusion/UI/Vault/Resources/DelusionVaultUI"
SCALE = 4


def recolor_alpha(path: Path, rgb: tuple[int, int, int]) -> None:
    source = Image.open(path).convert("RGBA")
    alpha = source.getchannel("A")
    output = Image.new("RGBA", source.size, (*rgb, 0))
    output.putalpha(alpha)
    output.save(path)


def crown() -> None:
    size = 128 * SCALE
    image = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    gold = (232, 198, 109, 255)
    width = 8 * SCALE
    points = [(19, 39), (39, 80), (64, 31), (89, 80), (109, 39), (100, 96), (28, 96), (19, 39)]
    points = [(x * SCALE, y * SCALE) for x, y in points]
    draw.line(points, fill=gold, width=width, joint="curve")
    draw.line([(29 * SCALE, 108 * SCALE), (99 * SCALE, 108 * SCALE)], fill=gold, width=width)
    for x, y in [(19, 39), (64, 31), (109, 39)]:
        r = 6 * SCALE
        draw.ellipse((x * SCALE - r, y * SCALE - r, x * SCALE + r, y * SCALE + r), fill=gold)
    image.resize((128, 128), Image.Resampling.LANCZOS).save(RESOURCE / "icon_crown.png")


def shareholder_card() -> None:
    w, h = 320 * SCALE, 96 * SCALE
    image = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle(
        (2 * SCALE, 2 * SCALE, w - 2 * SCALE, h - 2 * SCALE),
        radius=24 * SCALE,
        fill=(7, 51, 53, 255),
        outline=(228, 189, 97, 255),
        width=3 * SCALE,
    )
    image.resize((320, 96), Image.Resampling.LANCZOS).save(RESOURCE / "shareholder_card_9slice.png")


def circle_cream() -> None:
    size = 128 * SCALE
    image = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.ellipse((2 * SCALE, 2 * SCALE, size - 2 * SCALE, size - 2 * SCALE), fill=(247, 243, 237, 255))
    image.resize((128, 128), Image.Resampling.LANCZOS).save(RESOURCE / "circle_cream.png")


if __name__ == "__main__":
    recolor_alpha(RESOURCE / "icon_news.png", (213, 218, 213))
    recolor_alpha(RESOURCE / "icon_wallet.png", (232, 198, 109))
    crown()
    shareholder_card()
    circle_cream()
