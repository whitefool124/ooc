using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OCC.Combat.Presentation
{
    /// <summary>Native32 modelling grid. Only exposed faces are emitted, merged within each plane.</summary>
    public sealed class VfxVoxelMesh
    {
        public const float Pitch=1f/32;
        private readonly HashSet<Vector3Int> cells=new HashSet<Vector3Int>();
        private static readonly Vector3Int[] Axes={Vector3Int.right,Vector3Int.up,new Vector3Int(0,0,1)};
        public int VoxelCount=>cells.Count;
        public void Shape(Vector3 minimum,Vector3 maximum,Func<Vector3,bool> inside)
        {
            var lo=Vector3Int.FloorToInt(minimum/Pitch);var hi=Vector3Int.CeilToInt(maximum/Pitch);
            for(int z=lo.z;z<hi.z;z++)for(int y=lo.y;y<hi.y;y++)for(int x=lo.x;x<hi.x;x++)
            {var cell=new Vector3Int(x,y,z);if(inside(((Vector3)cell+Vector3.one*.5f)*Pitch))cells.Add(cell);}
        }
        public void Sphere(Vector3 centre,float radius)
        {Shape(centre-Vector3.one*radius,centre+Vector3.one*radius,p=>(p-centre).sqrMagnitude<=radius*radius);}
        public void Tube(IReadOnlyList<Vector3> path,IReadOnlyList<float> radii)
        {
            for(int i=0;i<path.Count-1;i++)
            {
                float distance=Vector3.Distance(path[i],path[i+1]);int steps=Mathf.Max(1,Mathf.CeilToInt(distance/(Pitch*.65f)));
                for(int j=0;j<=steps;j++){float t=j/(float)steps;Sphere(Vector3.Lerp(path[i],path[i+1],t),Mathf.Lerp(radii[i],radii[i+1],t));}
            }
        }
        public void Ring(float radius,float radialWidth,float height)
        {Shape(new Vector3(-radius-radialWidth,-height*.5f,-radius-radialWidth),new Vector3(radius+radialWidth,height*.5f,radius+radialWidth),p=>Mathf.Abs(new Vector2(p.x,p.z).magnitude-radius)<=radialWidth*.5f);}
        public Mesh Bake(string name)
        {
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            for(int axis=0;axis<3;axis++)for(int side=-1;side<=1;side+=2)
            {
                int u=(axis+1)%3,v=(axis+2)%3;var direction=Axes[axis]*side;
                var planes=new Dictionary<int,HashSet<Vector2Int>>();
                foreach(var cell in cells)
                {
                    if(cells.Contains(cell+direction))continue;
                    int plane=cell[axis]+(side>0?1:0);
                    if(!planes.TryGetValue(plane,out var mask)){mask=new HashSet<Vector2Int>();planes.Add(plane,mask);}
                    mask.Add(new Vector2Int(cell[u],cell[v]));
                }
                foreach(var pair in planes)
                {
                    var mask=pair.Value;
                    while(mask.Count>0)
                    {
                        Vector2Int start=default;foreach(var p in mask){start=p;break;}
                        int w=1,h=1;while(mask.Contains(start+Vector2Int.right*w))w++;
                        bool row=true;while(row){for(int x=0;x<w;x++)if(!mask.Contains(start+new Vector2Int(x,h))){row=false;break;}if(row)h++;}
                        for(int y=0;y<h;y++)for(int x=0;x<w;x++)mask.Remove(start+new Vector2Int(x,y));
                        Vector3 origin=(Vector3)Axes[axis]*pair.Key+(Vector3)Axes[u]*start.x+(Vector3)Axes[v]*start.y;
                        Vector3 a=origin*Pitch,b=(origin+(Vector3)Axes[u]*w)*Pitch,c=(origin+(Vector3)Axes[u]*w+(Vector3)Axes[v]*h)*Pitch,d=(origin+(Vector3)Axes[v]*h)*Pitch;
                        int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});
                        for(int i=0;i<4;i++)normals.Add(direction);
                        uv.Add(new Vector2(start.x,start.y)*Pitch);uv.Add(new Vector2(start.x+w,start.y)*Pitch);uv.Add(new Vector2(start.x+w,start.y+h)*Pitch);uv.Add(new Vector2(start.x,start.y+h)*Pitch);
                        if(side>0)triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});else triangles.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
                    }
                }
            }
            var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;
        }
    }
}
