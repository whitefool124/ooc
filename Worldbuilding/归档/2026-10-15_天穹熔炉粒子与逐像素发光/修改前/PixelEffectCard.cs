using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OCC.Combat.Presentation
{
    /// <summary>A one-world-pixel thick image, including its actual alpha outline walls.</summary>
    public static class PixelEffectCard
    {
        public static Mesh Build(Texture2D texture,float thickness)
        {
            if(!texture.isReadable)throw new System.InvalidOperationException("Readable pixel card required: "+texture.name);
            int w=texture.width,h=texture.height;var pixels=texture.GetPixels32();
            var positions=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
            float z=thickness*.5f;
            void Face(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal,Vector2 ta,Vector2 tb,Vector2 tc,Vector2 td)
            {
                int n=positions.Count;positions.AddRange(new[]{a,b,c,d});for(int i=0;i<4;i++)normals.Add(normal);uv.AddRange(new[]{ta,tb,tc,td});
                if(Vector3.Dot(Vector3.Cross(b-a,c-a),normal)>0)indices.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});else indices.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
            }
            Face(new Vector3(-.5f,-.5f,z),new Vector3(.5f,-.5f,z),new Vector3(.5f,.5f,z),new Vector3(-.5f,.5f,z),Vector3.forward,Vector2.zero,Vector2.right,Vector2.one,Vector2.up);
            Face(new Vector3(-.5f,-.5f,-z),new Vector3(.5f,-.5f,-z),new Vector3(.5f,.5f,-z),new Vector3(-.5f,.5f,-z),Vector3.back,Vector2.zero,Vector2.right,Vector2.one,Vector2.up);
            bool Solid(int x,int y)=>x>=0&&y>=0&&x<w&&y<h&&pixels[y*w+x].a>=128;
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                if(!Solid(x,y))continue;
                float x0=x/(float)w-.5f,x1=(x+1)/(float)w-.5f,y0=y/(float)h-.5f,y1=(y+1)/(float)h-.5f;
                var t=new Vector2((x+.5f)/w,(y+.5f)/h);
                if(!Solid(x-1,y))Face(new Vector3(x0,y0,-z),new Vector3(x0,y1,-z),new Vector3(x0,y1,z),new Vector3(x0,y0,z),Vector3.left,t,t,t,t);
                if(!Solid(x+1,y))Face(new Vector3(x1,y0,-z),new Vector3(x1,y1,-z),new Vector3(x1,y1,z),new Vector3(x1,y0,z),Vector3.right,t,t,t,t);
                if(!Solid(x,y-1))Face(new Vector3(x0,y0,-z),new Vector3(x1,y0,-z),new Vector3(x1,y0,z),new Vector3(x0,y0,z),Vector3.down,t,t,t,t);
                if(!Solid(x,y+1))Face(new Vector3(x0,y1,-z),new Vector3(x1,y1,-z),new Vector3(x1,y1,z),new Vector3(x0,y1,z),Vector3.up,t,t,t,t);
            }
            var mesh=new Mesh{name="One pixel thick image: "+texture.name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(positions);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();return mesh;
        }
    }
}
