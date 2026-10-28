using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace OCC.Combat.Tests
{
    public sealed class StageOneRenderPipelineTests
    {
        [Test]
        public void EveryQualityLevel_ResolvesToUniversalPipeline()
        {
            Assert.That(Application.dataPath.Replace('\\','/'), Is.EqualTo("E:/数据库/OCC_Codex/UnityProject/Assets"));
            for(int i=0;i<QualitySettings.names.Length;i++)
                Assert.That((QualitySettings.GetRenderPipelineAssetAt(i)??GraphicsSettings.defaultRenderPipeline)?.GetType().FullName,
                    Is.EqualTo("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"),
                    QualitySettings.names[i]+" must render the existing HD2D materials");
        }
    }
}
