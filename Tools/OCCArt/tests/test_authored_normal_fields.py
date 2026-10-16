import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('bake', Path(__file__).resolve().parents[1]/'bake_authored_normal_fields.py')
bake = importlib.util.module_from_spec(spec)
spec.loader.exec_module(bake)


class AuthoredNormalTests(unittest.TestCase):
    def test_flat_plane_including_edges(self):
        w,h,rgb=bake.normal_pixels([[.25]*4 for _ in range(3)], 1/32)
        self.assertEqual((w,h),(4,3))
        self.assertEqual(rgb,bytes([128,128,255])*12)

    def test_x_slope_points_against_height_gradient(self):
        _,_,rgb=bake.normal_pixels([[0,.01,.02]]*3,1/32)
        for i in range(0,len(rgb),3):
            self.assertLess(rgb[i],128)
            self.assertEqual(rgb[i+1],128)
            self.assertGreater(rgb[i+2],240)

    def test_top_down_rows_use_positive_green_for_downward_rise(self):
        _,_,rgb=bake.normal_pixels([[0]*3,[.01]*3,[.02]*3],1/32)
        self.assertTrue(all(rgb[i+1]>128 for i in range(0,len(rgb),3)))

    def test_absolute_height_offset_cannot_change_normals(self):
        field=[[x*.01+y*.02 for x in range(3)] for y in range(3)]
        self.assertEqual(bake.normal_pixels(field,1/32),bake.normal_pixels([[v+5 for v in row] for row in field],1/32))

    def test_world_density_affects_slope(self):
        field=[[0,.01,.02]]*3
        dense=bake.normal_pixels(field,1/32)[2]
        sparse=bake.normal_pixels(field,1/16)[2]
        self.assertLess(dense[0],sparse[0])

    def test_invalid_geometry_rejected(self):
        for field,pitch in [([[0,0],[0,float('nan')]],1/32),([[0,0],[0]],1/32),([[0,0],[0,0]],0)]:
            with self.assertRaises(ValueError):bake.normal_pixels(field,pitch)


if __name__ == '__main__':unittest.main()
