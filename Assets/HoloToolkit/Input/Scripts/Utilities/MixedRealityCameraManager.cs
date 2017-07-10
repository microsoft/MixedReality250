using UnityEngine;
using UnityEngine.XR.WSA;

namespace HoloToolkit.Unity.InputModule
{
    /// <summary>
    /// This script tells you if your head mounted display (HMD)
    /// is a transparent device or an occluded device.
    /// Based on those values, you can customize your camera settings.
    /// </summary>
    public class MixedRealityCameraManager : MonoBehaviour
    {
#pragma warning disable 0618
        // Unity has marked these as deprecated but we haven't found a better
        // way to implement this functionality
        public QualityLevel OpaqueQualityLevel;
        public QualityLevel HoloLensQualityLevel;
#pragma warning restore 0618
        public float OpaqueNearPlane = 0.3f;
        public float HoloLensNearPlane = 0.5f;

        void Start()
        {
            if (HolographicSettings.IsDisplayOpaque)
            {
                ApplySettingsForOpaqueDisplay();
            }
            else
            {
                ApplySettingsForTransparentDisplay();
            }
        }
        
        public void ApplySettingsForOpaqueDisplay()
        {
            Debug.Log("Display is Opaque");
            Camera.main.clearFlags = CameraClearFlags.Skybox;
            Camera.main.nearClipPlane = OpaqueNearPlane;

            SetQuality(OpaqueQualityLevel);
        }

        public void ApplySettingsForTransparentDisplay()
        {
            Debug.Log("Display is Transparent");
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = Color.clear;
            Camera.main.nearClipPlane = HoloLensNearPlane;
           SetQuality(HoloLensQualityLevel);
            UnityEngine.XR.WSA.HolographicSettings.ActivateLatentFramePresentation(true);
        }

#pragma warning disable 0618
        private void SetQuality(QualityLevel level)
        {
            string levelString = level.ToString();
            for(int index=0;index<QualitySettings.names.Length;index++)
            {
                if (levelString == QualitySettings.names[index])
                {
                    Debug.LogFormat("Level {0} is index {1}", levelString, index);
                    QualitySettings.SetQualityLevel(index, false);
                    return;
                }
            }

            Debug.Log("Didn't find quality level " + levelString);
        }
#pragma warning restore 0618
    }
}
