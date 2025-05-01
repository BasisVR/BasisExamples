using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.ShaderGUI
{
    internal class LitAudioLink : BaseShaderGUI
    {

        public struct LitProperties
        {
            public MaterialProperty detailMask;
            public MaterialProperty detailAlbedoMapScale;
            public MaterialProperty detailAlbedoMap;
            public MaterialProperty detailNormalMapScale;
            public MaterialProperty detailNormalMap;

            public LitProperties(MaterialProperty[] properties)
            {
                detailMask = FindProperty("_DetailMask", properties, false);
                detailAlbedoMapScale = FindProperty("_DetailAlbedoMapScale", properties, false);
                detailAlbedoMap = FindProperty("_DetailAlbedoMap", properties, false);
                detailNormalMapScale = FindProperty("_DetailNormalMapScale", properties, false);
                detailNormalMap = FindProperty("_DetailNormalMap", properties, false);
            }
        }

        internal static class LitStyles
        {
            public static readonly GUIContent detailInputs = EditorGUIUtility.TrTextContent("Detail Inputs",
                "These settings define the surface details by tiling and overlaying additional maps on the surface.");

            public static readonly GUIContent detailMaskText = EditorGUIUtility.TrTextContent("Mask",
                "Select a mask for the Detail map. The mask uses the alpha channel of the selected texture. The Tiling and Offset settings have no effect on the mask.");

            public static readonly GUIContent detailAlbedoMapText = EditorGUIUtility.TrTextContent("Base Map",
                "Select the surface detail texture.The alpha of your texture determines surface hue and intensity.");

            public static readonly GUIContent detailNormalMapText = EditorGUIUtility.TrTextContent("Normal Map",
                "Designates a Normal Map to create the illusion of bumps and dents in the details of this Material's surface.");

            public static readonly GUIContent detailAlbedoMapScaleInfo = EditorGUIUtility.TrTextContent("Setting the scaling factor to a value other than 1 results in a less performant shader variant.");
            public static readonly GUIContent detailAlbedoMapFormatError = EditorGUIUtility.TrTextContent("This texture is not in linear space.");
        }

        static readonly string[] workflowModeNames = Enum.GetNames(typeof(LitGUI.WorkflowMode));

        private LitGUI.LitProperties litProperties;
        private LitProperties litDetailProperties;
        private LitExtGUI.LitExtProperties litExtProperties;
        private LitExtGUI.LitAudioLinkProperties litAudioLinkProperties;

        public override void FillAdditionalFoldouts(MaterialHeaderScopeList materialScopesList)
        {
            materialScopesList.RegisterHeaderScope(LitStyles.detailInputs, Expandable.Details, _ => DoDetailArea(litDetailProperties, materialEditor));
        }


        public static void DoDetailArea(LitProperties properties, MaterialEditor materialEditor)
        {
            materialEditor.TexturePropertySingleLine(LitStyles.detailMaskText, properties.detailMask);
            materialEditor.TexturePropertySingleLine(LitStyles.detailAlbedoMapText, properties.detailAlbedoMap,
                properties.detailAlbedoMap.textureValue != null ? properties.detailAlbedoMapScale : null);
            if (properties.detailAlbedoMapScale.floatValue != 1.0f)
            {
                EditorGUILayout.HelpBox(LitStyles.detailAlbedoMapScaleInfo.text, MessageType.Info, true);
            }

            var detailAlbedoTexture = properties.detailAlbedoMap.textureValue as Texture2D;
            if (detailAlbedoTexture != null && UnityEngine.Experimental.Rendering.GraphicsFormatUtility.IsSRGBFormat(detailAlbedoTexture.graphicsFormat))
            {
                EditorGUILayout.HelpBox(LitStyles.detailAlbedoMapFormatError.text, MessageType.Warning, true);
            }

            materialEditor.TexturePropertySingleLine(LitStyles.detailNormalMapText, properties.detailNormalMap,
                properties.detailNormalMap.textureValue != null ? properties.detailNormalMapScale : null);
            materialEditor.TextureScaleOffsetProperty(properties.detailAlbedoMap);
        }

        // collect properties from the material properties
        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            litProperties = new LitGUI.LitProperties(properties);
            litDetailProperties = new LitProperties(properties);
            litExtProperties = new LitExtGUI.LitExtProperties(properties);
            litAudioLinkProperties = new LitExtGUI.LitAudioLinkProperties(properties);
        }

        // material changed check
        public override void ValidateMaterial(Material material)
        {
            SetMaterialKeywords(material, LitGUI.SetMaterialKeywords, SetMaterialKeywords);
        }

        public static void SetMaterialKeywords(Material material)
        {
            if (material.HasProperty("_DetailAlbedoMap") && material.HasProperty("_DetailNormalMap") && material.HasProperty("_DetailAlbedoMapScale"))
            {
                bool isScaled = material.GetFloat("_DetailAlbedoMapScale") != 1.0f;
                bool hasDetailMap = material.GetTexture("_DetailAlbedoMap") || material.GetTexture("_DetailNormalMap");
                UnityEngine.Rendering.CoreUtils.SetKeyword(material, "_DETAIL_MULX2", !isScaled && hasDetailMap);
                UnityEngine.Rendering.CoreUtils.SetKeyword(material, "_DETAIL_SCALED", isScaled && hasDetailMap);
            }
        }

        // material main surface options
        public override void DrawSurfaceOptions(Material material)
        {
            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;

            if (litProperties.workflowMode != null)
                DoPopup(LitGUI.Styles.workflowModeText, litProperties.workflowMode, workflowModeNames);

            base.DrawSurfaceOptions(material);
        }

        // material main surface inputs
        public override void DrawSurfaceInputs(Material material)
        {
            base.DrawSurfaceInputs(material);
            LitGUI.Inputs(litProperties, materialEditor, material);
            DrawEmissionProperties(material, true);
            const int indent = MaterialEditor.kMiniTextureFieldLabelIndentLevel;
            EditorGUI.indentLevel += indent;
            LitExtGUI.DrawGIOptions(material, litExtProperties);
            LitExtGUI.DrawAudioLinkOptions(material, litAudioLinkProperties);
            EditorGUI.indentLevel -= indent;
            DrawTileOffset(materialEditor, baseMapProp);
        }

        // material main advanced options
        public override void DrawAdvancedOptions(Material material)
        {
            if (litProperties.reflections != null && litProperties.highlights != null)
            {
                materialEditor.ShaderProperty(litProperties.highlights, LitGUI.Styles.highlightsText);
                materialEditor.ShaderProperty(litProperties.reflections, LitGUI.Styles.reflectionsText);
            }

            base.DrawAdvancedOptions(material);
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            // _Emission property is lost after assigning Standard shader to the material
            // thus transfer it before assigning the new shader
            if (material.HasProperty("_Emission"))
            {
                material.SetColor("_EmissionColor", material.GetColor("_Emission"));
            }

            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            if (oldShader == null || !oldShader.name.Contains("Legacy Shaders/"))
            {
                SetupMaterialBlendMode(material);
                return;
            }

            SurfaceType surfaceType = SurfaceType.Opaque;
            BlendMode blendMode = BlendMode.Alpha;
            if (oldShader.name.Contains("/Transparent/Cutout/"))
            {
                surfaceType = SurfaceType.Opaque;
                material.SetFloat("_AlphaClip", 1);
            }
            else if (oldShader.name.Contains("/Transparent/"))
            {
                // NOTE: legacy shaders did not provide physically based transparency
                // therefore Fade mode
                surfaceType = SurfaceType.Transparent;
                blendMode = BlendMode.Alpha;
            }

            material.SetFloat("_Blend", (float)blendMode);

            material.SetFloat("_Surface", (float)surfaceType);
            if (surfaceType == SurfaceType.Opaque)
            {
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            if (oldShader.name.Equals("Standard (Specular setup)"))
            {
                material.SetFloat("_WorkflowMode", (float)LitGUI.WorkflowMode.Specular);
                Texture texture = material.GetTexture("_SpecGlossMap");
                if (texture != null)
                    material.SetTexture("_MetallicSpecGlossMap", texture);
            }
            else
            {
                material.SetFloat("_WorkflowMode", (float)LitGUI.WorkflowMode.Metallic);
                Texture texture = material.GetTexture("_MetallicGlossMap");
                if (texture != null)
                    material.SetTexture("_MetallicSpecGlossMap", texture);
            }
        }
    }
}
