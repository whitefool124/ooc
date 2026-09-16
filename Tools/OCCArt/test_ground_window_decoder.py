from __future__ import annotations

import importlib.util
import unittest
from pathlib import Path

from PIL import Image


MODULE_PATH = Path(__file__).resolve().with_name("decode_generated_pixel_grid.py")
SPEC = importlib.util.spec_from_file_location("occ_ground_window_decoder", MODULE_PATH)
DECODER = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(DECODER)


class GroundWindowDecoderTests(unittest.TestCase):
    def test_lossless_center_window_uses_only_original_logical_pixels(self):
        field = Image.new("RGBA", (36, 35))
        for y in range(field.height):
            for x in range(field.width):
                field.putpixel((x, y), (x, y, (x + y) % 256, 255))

        output, bounds = DECODER.select_ground_window(field, (32, 32))

        self.assertEqual(bounds, (2, 1, 34, 33))
        self.assertIsNotNone(output)
        self.assertEqual(output.size, (32, 32))
        self.assertEqual(output.tobytes(), field.crop(bounds).tobytes())

    def test_transparent_or_small_field_is_rejected(self):
        transparent = Image.new("RGBA", (36, 36), (1, 2, 3, 255))
        transparent.putpixel((0, 0), (0, 0, 0, 0))
        self.assertEqual(DECODER.select_ground_window(transparent, (32, 32)), (None, None))
        self.assertEqual(DECODER.select_ground_window(Image.new("RGBA", (31, 32), (1, 2, 3, 255)), (32, 32)), (None, None))

    def test_declared_phase_aligned_origin_is_lossless_and_bounded(self):
        field = Image.new("RGBA", (40, 40), (1, 2, 3, 255))
        output, bounds = DECODER.select_ground_window(field, (32, 32), (1, 5))
        self.assertEqual(bounds, (1, 5, 33, 37))
        self.assertEqual(output.tobytes(), field.crop(bounds).tobytes())
        self.assertEqual(DECODER.select_ground_window(field, (32, 32), (9, 0)), (None, None))


if __name__ == "__main__":
    unittest.main()
