from __future__ import annotations

import importlib.util
import unittest
from pathlib import Path

from PIL import Image


MODULE_PATH = Path(__file__).resolve().with_name("decode_generated_pixel_grid.py")
SPEC = importlib.util.spec_from_file_location("occ_ground_field_pitch_decoder", MODULE_PATH)
DECODER = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(DECODER)


class GroundFieldPitchDecoderTests(unittest.TestCase):
    def test_explicit_pitch_can_only_feed_an_opaque_lossless_window(self):
        field = Image.new("RGBA", (104, 104), (180, 96, 64, 255))
        output, bounds = DECODER.select_ground_window(field, (32, 32))
        self.assertEqual(bounds, (36, 36, 68, 68))
        self.assertEqual(output.tobytes(), field.crop(bounds).tobytes())


if __name__ == "__main__":
    unittest.main()
