using UnityEngine;
using Unity.Rendering.Universal;
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGraph;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering.Universal;

namespace OceanX
{
    public static class ShaderUtils
    {
        internal enum ShaderID
        {
            Unknown = -1,
            Lit = 0,
            SimpleLit = 1,
            Unlit = 2,
            TerrainLit = 3,
            ParticlesLit = 4,
            ParticlesSimpleLit = 5,
            ParticlesUnlit = 6,
            BakedLit = 7,
            SpeedTree7 = 8,
            SpeedTree7Billboard = 9,
            SpeedTree8 = 10,
            SG_Unlit = 1000,
            SG_Lit = 1001,
            SG_Decal = 1002,
            SG_SpriteUnlit = 1003,
            SG_SpriteLit = 1004,
            SG_SpriteCustomLit = 1005
        }

        internal enum MaterialUpdateType
        {
            CreatedNewMaterial,
            ChangedAssignedShader,
            ModifiedShader,
            ModifiedMaterial
        }

        internal static bool IsShaderGraph(this ShaderID id)
        {
            return id >= ShaderID.SG_Unlit;
        }

        internal static ShaderID GetShaderID(Shader shader)
        {
            //if (shader.IsShaderGraphAsset())
            //{
            //    if (!shader.TryGetMetadataOfType(out UniversalMetadata obj))
            //    {
            //        return ShaderID.Unknown;
            //    }

            //    return obj.shaderID;
            //}

            return (ShaderID)UnityEngine.Rendering.Universal.ShaderUtils.GetEnumFromPath(shader.name);
        }

        internal static void UpdateMaterial(Material material, MaterialUpdateType updateType, Object assetWithURPMetaData)
        {
            ShaderID shaderID = ShaderID.Unknown;
            //if (assetWithURPMetaData != null)
            //{
            //    string assetPath = AssetDatabase.GetAssetPath(assetWithURPMetaData);
            //    Object[] array = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            //    foreach (Object @object in array)
            //    {
            //        UniversalMetadata universalMetadata = @object as UniversalMetadata;
            //        if ((object)universalMetadata != null)
            //        {
            //            shaderID = universalMetadata.shaderID;
            //            break;
            //        }
            //    }
            //}

            UpdateMaterial(material, updateType, shaderID);
        }

        internal static void UpdateMaterial(Material material, MaterialUpdateType updateType, ShaderID shaderID = ShaderID.Unknown)
        {
            if (shaderID == ShaderID.Unknown)
            {
                shaderID = GetShaderID(material.shader);
            }

            switch (shaderID)
            {
                case ShaderID.Lit:
                    BaseShaderGUI.SetMaterialKeywords(material, LitGUI.SetMaterialKeywords);
                    break;
                case ShaderID.SimpleLit:
                    BaseShaderGUI.SetMaterialKeywords(material, SimpleLitGUI.SetMaterialKeywords);
                    break;
                case ShaderID.Unlit:
                    BaseShaderGUI.SetMaterialKeywords(material);
                    break;
                case ShaderID.ParticlesLit:
                    BaseShaderGUI.SetMaterialKeywords(material, LitGUI.SetMaterialKeywords, ParticleGUI.SetMaterialKeywords);
                    break;
                case ShaderID.ParticlesSimpleLit:
                    BaseShaderGUI.SetMaterialKeywords(material, SimpleLitGUI.SetMaterialKeywords, ParticleGUI.SetMaterialKeywords);
                    break;
                case ShaderID.ParticlesUnlit:
                    BaseShaderGUI.SetMaterialKeywords(material, null, ParticleGUI.SetMaterialKeywords);
                    break;
                case ShaderID.SpeedTree8:
                    //ShaderGraphLitGUI.UpdateMaterial(material, updateType);
                    break;
                case ShaderID.SG_Lit:
                    //ShaderGraphLitGUI.UpdateMaterial(material, updateType);
                    break;
                case ShaderID.SG_Unlit:
                    //ShaderGraphUnlitGUI.UpdateMaterial(material, updateType);
                    break;
            }
        }
    }
}
