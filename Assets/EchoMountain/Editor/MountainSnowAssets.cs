using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace EchoMountain.EditorTools
{
    public static class MountainSnowAssets
    {
        private const string Directory = "Assets/EchoMountain/Resources";
        private const string Path = Directory + "/EchoMountainSnowSpray.mat";

        [MenuItem("Echo Mountain/Create snow spray material (once)")]
        public static void Create()
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(Path)) return;
            if (System.IO.File.Exists(Path)) throw new InvalidOperationException("Snow spray asset exists and will not be overwritten.");
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (!shader) throw new InvalidOperationException("URP particle shader is unavailable.");
            if (!AssetDatabase.IsValidFolder(Directory)) AssetDatabase.CreateFolder("Assets/EchoMountain", "Resources");
            var material = new Material(shader) { name = "Echo Mountain snow spray", renderQueue = (int)RenderQueue.Transparent };
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            AssetDatabase.CreateAsset(material, Path);
            Undo.RegisterCreatedObjectUndo(material, "Create Echo Mountain snow spray material");
            AssetDatabase.SaveAssets();
            Debug.Log("Created owned snow spray material: " + Path);
        }
    }
}
