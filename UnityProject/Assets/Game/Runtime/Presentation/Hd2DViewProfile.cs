using UnityEngine;

namespace OCC.Combat.Presentation
{
    // Long lens keeps the diorama readable while retaining real depth and occlusion.
    public static class Hd2DViewProfile
    {
        public const float DioramaPitch=35f,DioramaYaw=0f,DioramaFieldOfView=24f;
        public const float BattlefieldPitch=45f;
        public const float BattlefieldWallHeight=.65f;
        // Screen-facing cutouts preserve their drawn proportions under the shared camera.
        public const float BattlefieldUnitGroundAngle=90f-BattlefieldPitch;
        public static Quaternion BattlefieldUnitRotation=>BattlefieldRotation;
        public static Quaternion BattlefieldRotation=>Quaternion.Euler(BattlefieldPitch,DioramaYaw,0);
        public static Vector3 BattlefieldCameraPosition(Vector3 target,float distance)
            =>target-BattlefieldRotation*Vector3.forward*distance;
        public static Quaternion DioramaRotation=>Quaternion.Euler(DioramaPitch,DioramaYaw,0);
        public static Vector3 CameraPosition(Vector3 target,float distance)
            =>target-DioramaRotation*Vector3.forward*distance;
    }
}
