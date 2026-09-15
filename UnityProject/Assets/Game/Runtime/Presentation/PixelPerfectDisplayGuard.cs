using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Reflection;

namespace OCC.Combat.Presentation
{
    /// <summary>
    /// First-stage display guard for the OCC pixel pipeline.
    /// It removes renderer-level features that introduce blended pixels and
    /// applies the same policy to cameras created by runtime presentations.
    /// </summary>
    internal static class PixelPerfectDisplayGuard
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            QualitySettings.antiAliasing = 0;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplyToCameras();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplyToCameras();

        private static void ApplyToCameras()
        {
            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Camera camera in cameras)
            {
                if (camera == null) continue;
                camera.allowMSAA = false;
                camera.allowHDR = false;
                ConfigurePixelPerfectCamera(camera);
            }
        }

        private static void ConfigurePixelPerfectCamera(Camera camera)
        {
            Type type = FindPixelPerfectCameraType();
            if (type == null) return;
            Component component = camera.GetComponent(type) ?? camera.gameObject.AddComponent(type);
            Set(type, component, "assetsPPU", 32);
            Set(type, component, "refResolutionX", 480);
            Set(type, component, "refResolutionY", 270);
            Set(type, component, "upscaleRenderTexture", true);
            Set(type, component, "pixelSnapping", true);
        }

        private static Type FindPixelPerfectCameraType()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType("UnityEngine.U2D.PixelPerfectCamera", false);
                if (type != null && typeof(Component).IsAssignableFrom(type)) return type;
            }
            return null;
        }

        private static void Set(Type type, Component component, string name, object value)
        {
            PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (property != null && property.CanWrite) property.SetValue(component, value, null);
        }
    }
}
