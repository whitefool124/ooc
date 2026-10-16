using UnityEngine;

namespace OCC.Combat.Presentation
{
    // Long lens keeps the diorama readable while retaining real depth and occlusion.
    public static class Hd2DViewProfile
    {
        public const float DioramaPitch=35f,DioramaYaw=12f,DioramaFieldOfView=24f;
        public static Quaternion DioramaRotation=>Quaternion.Euler(DioramaPitch,DioramaYaw,0);
        public static Vector3 CameraPosition(Vector3 target,float distance)
            =>target-DioramaRotation*Vector3.forward*distance;
    }
}
