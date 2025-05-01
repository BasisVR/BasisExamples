using UnityEngine;

namespace UnityEditor.Rendering.Universal.ShaderGUI
{
    public class LitExtGUI
    {
        private enum AudioTimingSource
        {
            [InspectorName("Instant")] NONE = 99,
            [InspectorName("Alpha Channel")] ALPHA = 3,
            [InspectorName("Red Channel")] RED = 0,
            [InspectorName("Green Channel")] GREEN = 1,
            [InspectorName("Blue Channel")] BLUE = 2,
            [InspectorName("Grayscale")] GRAYSCALE = 4,
            [InspectorName("UV X")] UVX = 5,
            [InspectorName("UV Y")] UVY = 6,
            [InspectorName("UV Center")] UVCENTER = 7,
        }

        private static readonly AudioTimingSource[] TextureTimings =
        {
            AudioTimingSource.RED,
            AudioTimingSource.GREEN,
            AudioTimingSource.BLUE,
            AudioTimingSource.ALPHA,
            AudioTimingSource.GRAYSCALE
        };

        private enum AudioBand
        {
            [InspectorName("Bass")] BASS = 0,
            [InspectorName("Low Mids")] LOWMIDS = 1,
            [InspectorName("High Mids")] HIGHMIDS = 2,
            [InspectorName("Treble")] TREBLE = 3,
            [InspectorName("Waveform")] WAVEFORM = 27
        }

        public struct LitExtProperties
        {
            public readonly MaterialProperty giBrightness;

            public LitExtProperties(MaterialProperty[] properties)
            {
                giBrightness = BaseShaderGUI.FindProperty("_GIBrightness", properties, false);
            }
        }

        public struct LitAudioLinkProperties
        {
            public readonly MaterialProperty audioBand;
            public readonly MaterialProperty audioIntensity;
            public readonly MaterialProperty audioTimingSource;
            public readonly MaterialProperty audioEmissionMinimum;
            public readonly MaterialProperty audioEmissionInverse;
            public readonly MaterialProperty audioTimingInverse;
            public readonly MaterialProperty audioTimingTexture;

            public LitAudioLinkProperties(MaterialProperty[] properties)
            {
                audioBand = BaseShaderGUI.FindProperty("_AudioBand", properties, true);
                audioIntensity = BaseShaderGUI.FindProperty("_AudioIntensity", properties, true);
                audioEmissionMinimum = BaseShaderGUI.FindProperty("_AudioEmissionMinimum", properties, true);
                audioEmissionInverse = BaseShaderGUI.FindProperty("_AudioEmissionInverse", properties, true);
                audioTimingSource = BaseShaderGUI.FindProperty("_AudioTimingSource", properties, true);
                audioTimingTexture = BaseShaderGUI.FindProperty("_AudioTimingTexture", properties, true);
                audioTimingInverse = BaseShaderGUI.FindProperty("_AudioTimingInverse", properties, true);
            }
        }

        public static void DrawGIOptions(Material material, LitExtProperties properties)
        {
            if (material.globalIlluminationFlags != MaterialGlobalIlluminationFlags.EmissiveIsBlack)
            {
                MaterialEditor.BeginProperty(properties.giBrightness);
                EditorGUI.BeginChangeCheck();

                float giBrightness = properties.giBrightness.floatValue;
                if ((material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.AnyEmissive) != 0)
                    giBrightness = EditorGUILayout.FloatField("GI Brightness", giBrightness);

                if (EditorGUI.EndChangeCheck())
                {
                    properties.giBrightness.floatValue = giBrightness;
                }

                MaterialEditor.EndProperty();
            }
        }

        public static void DrawAudioLinkOptions(Material material, LitAudioLinkProperties properties)
        {
            if (material.globalIlluminationFlags != MaterialGlobalIlluminationFlags.EmissiveIsBlack)
            {
                EditorGUI.BeginChangeCheck();

                float audioIntensity = properties.audioIntensity.floatValue;
                float audioEmissionMinimum = properties.audioEmissionMinimum.floatValue;
                bool audioEmissionInverse = properties.audioEmissionInverse.floatValue == 1;
                AudioBand audioBand = (AudioBand)properties.audioBand.intValue;
                AudioTimingSource audioTimingSource = (AudioTimingSource)properties.audioTimingSource.intValue;
                Texture audioTimingTexture = properties.audioTimingTexture.textureValue;
                bool audioTimingInverse = properties.audioTimingInverse.floatValue == 1;

                MaterialEditor.BeginProperty(properties.audioIntensity);
                audioIntensity = EditorGUILayout.FloatField("Audio Intensity", audioIntensity);
                MaterialEditor.EndProperty();

                using (new EditorGUI.DisabledScope(audioIntensity <= 0))
                {

                    MaterialEditor.BeginProperty(properties.audioEmissionMinimum);
                    audioEmissionMinimum = EditorGUILayout.FloatField("Emission Minimum", audioEmissionMinimum);
                    MaterialEditor.EndProperty();

                    MaterialEditor.BeginProperty(properties.audioEmissionInverse);
                    audioEmissionInverse = EditorGUILayout.Toggle("Audio Emission Inverse", audioEmissionInverse);
                    MaterialEditor.EndProperty();

                    MaterialEditor.BeginProperty(properties.audioBand);
                    audioBand = (AudioBand)EditorGUILayout.EnumPopup("Audio Band", audioBand);
                    MaterialEditor.EndProperty();

                    MaterialEditor.BeginProperty(properties.audioTimingSource);
                    audioTimingSource = (AudioTimingSource)EditorGUILayout.EnumPopup("Audio Timing Source", audioTimingSource);
                    MaterialEditor.EndProperty();

                    if (audioTimingSource != AudioTimingSource.NONE)
                    {
                        MaterialEditor.BeginProperty(properties.audioTimingInverse);
                        audioTimingInverse = EditorGUILayout.Toggle("Audio Timing Inverse", audioTimingInverse);
                        MaterialEditor.EndProperty();
                    }

                    if (System.Array.IndexOf(TextureTimings, audioTimingSource) > -1)
                    {
                        MaterialEditor.BeginProperty(properties.audioTimingTexture);
                        var label = new GUIContent("Audio Timing Override", "Custom texture to use in place of the Emission Map when sampling for delay timing.");
                        audioTimingTexture = (Texture)EditorGUILayout.ObjectField(label, audioTimingTexture, typeof(Texture), false, GUILayout.Height(18), GUILayout.ExpandWidth(true));
                        MaterialEditor.EndProperty();
                    }
                }

                if (EditorGUI.EndChangeCheck())
                {
                    if (audioIntensity < 0) audioIntensity = 0;
                    if (audioEmissionMinimum > audioIntensity) audioEmissionMinimum = audioIntensity;
                    if (audioEmissionMinimum < 0) audioEmissionMinimum = 0;

                    properties.audioBand.intValue = (int)audioBand;
                    properties.audioIntensity.floatValue = audioIntensity;
                    properties.audioEmissionMinimum.floatValue = audioEmissionMinimum;
                    properties.audioEmissionInverse.floatValue = audioEmissionInverse ? 1f : 0f;
                    properties.audioTimingSource.intValue = (int)audioTimingSource;
                    properties.audioTimingInverse.floatValue = audioTimingInverse ? 1f : 0f;
                    properties.audioTimingTexture.textureValue = audioTimingTexture;

                    EditorUtility.SetDirty(material);
                }

            }
        }
    }
}
