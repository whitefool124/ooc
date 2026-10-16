import sys
from pathlib import Path
import unittest
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from build_authored_material_batch import expand


class MaterialRecipeTests(unittest.TestCase):
    def test_paint_is_surface_only_not_geometric_bump(self):
        roles={'void':dict(roughness=1,metallic=0,coat=0),'iron':dict(roughness=.65,metallic=.85,coat=0),'paint':dict(roughness=.8,metallic=0,coat=0)}
        asset=dict(layers=[dict(shape='rect',bounds=[0,0,3,3],height_m=.003,material='iron'),dict(shape='rect',bounds=[1,1,2,2],material='paint')])
        heights,labels,rgb=expand(asset,[4,4],roles)
        self.assertEqual(heights,[[.003]*4 for _ in range(4)])
        self.assertEqual(labels[1][1],'paint')
        self.assertEqual(rgb[(1*4+1)*3:(1*4+1)*3+3],bytes([204,0,0]))
        self.assertEqual(rgb[:3],bytes([166,217,0]))

    def test_unknown_physical_role_rejected(self):
        with self.assertRaises(ValueError):expand(dict(layers=[dict(shape='rect',bounds=[0,0,1,1],material='guessed_blue_emission')]),[2,2],{})

    def test_surface_values_outside_physical_domain_rejected(self):
        with self.assertRaises(ValueError):expand(dict(layers=[]),[2,2],{'void':dict(roughness=2,metallic=0,coat=0)})


if __name__=='__main__':unittest.main()
