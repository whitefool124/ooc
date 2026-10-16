using System.Collections.Generic;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    // Simple box faces repeat an existing material patch without compressing its native pixels.
    // UVs remain within the chosen patch; no texture copies, recolouring or runtime image generation.
    internal static class Hd2DNativeMaterialMesh
    {
        internal static void Rebuild(Mesh mesh, Vector3 scale, Rect patch, Vector2 textureSize, float ppu,
            bool verticalGrain = false)
        {
            if (mesh == null || scale.x <= 0 || scale.y <= 0 || scale.z <= 0 ||
                patch.width <= 0 || patch.height <= 0 || ppu <= 0)
                throw new System.ArgumentException("Positive box dimensions and pixel patch required.");
            var vertices=new List<Vector3>();var uv=new List<Vector2>();
            var normals=new List<Vector3>();var triangles=new List<int>();
            var directions=new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
            foreach(var normal in directions)
            {
                Vector3 u=Mathf.Abs(normal.x)>.5f?Vector3.forward:Vector3.right;
                Vector3 v=Mathf.Abs(normal.y)>.5f?Vector3.forward:Vector3.up;
                if(verticalGrain && Mathf.Abs(normal.y)<.5f)
                {var across=u;u=v;v=across;}
                float width=Vector3.Dot(u,scale),height=Vector3.Dot(v,scale);
                float tileW=patch.width/ppu,tileH=patch.height/ppu;
                int cols=Mathf.CeilToInt(width/tileW),rows=Mathf.CeilToInt(height/tileH);
                for(int row=0;row<rows;row++)for(int col=0;col<cols;col++)
                {
                    float x0=col*tileW-width*.5f,x1=Mathf.Min((col+1)*tileW,width)-width*.5f;
                    float y0=row*tileH-height*.5f,y1=Mathf.Min((row+1)*tileH,height)-height*.5f;
                    int start=vertices.Count;
                    foreach(var p in new[]{new Vector2(x0,y0),new Vector2(x1,y0),new Vector2(x1,y1),new Vector2(x0,y1)})
                    {
                        var world=Vector3.Scale(normal,scale)*.5f+u*p.x+v*p.y;
                        vertices.Add(new Vector3(world.x/scale.x,world.y/scale.y,world.z/scale.z));
                        normals.Add(normal);
                    }
                    float a=patch.xMin/textureSize.x,b=patch.yMin/textureSize.y;
                    float c=(patch.xMin+(x1-x0)*ppu)/textureSize.x,d=(patch.yMin+(y1-y0)*ppu)/textureSize.y;
                    uv.AddRange(new[]{new Vector2(a,b),new Vector2(c,b),new Vector2(c,d),new Vector2(a,d)});
                    bool outward=Vector3.Dot(Vector3.Cross(u,v),normal)>0;
                    triangles.AddRange(outward?new[]{start,start+1,start+2,start,start+2,start+3}:
                        new[]{start,start+2,start+1,start,start+3,start+2});
                }
            }
            mesh.Clear();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetNormals(normals);
            mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
        }
    }
}
