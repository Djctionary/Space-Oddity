using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FalconPaintTools
{
    const string MaterialName = "Falcon_Black_Metallic";
    const string MaterialPath = "Assets/Falcon/Falcon_Black_Metallic.mat";
    const string EditableMaterialFolder = "Assets/Falcon/EditableMaterials";
    const string SourceMaterialName = "main_paint.019";

    [MenuItem("Tools/Space Oddity/Create Black Metallic Falcon Material")]
    public static void CreateBlackMetallicMaterial()
    {
        Material material = GetOrCreateBlackMetallicMaterial();
        Selection.activeObject = material;
        EditorGUIUtility.PingObject(material);
    }

    [MenuItem("Tools/Space Oddity/Apply Black Metallic To Falcon Paint")]
    public static void ApplyBlackMetallicToFalconPaint()
    {
        Material replacement = GetOrCreateBlackMetallicMaterial();
        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int replacedSlots = 0;

        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null)
                    continue;

                if (CleanName(materials[i].name) != SourceMaterialName)
                    continue;

                materials[i] = replacement;
                changed = true;
                replacedSlots++;
            }

            if (!changed)
                continue;

            Undo.RecordObject(renderer, "Apply Black Metallic Falcon Paint");
            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(renderer);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Applied {MaterialName} to {replacedSlots} material slot(s) named {SourceMaterialName}.");
    }

    [MenuItem("Tools/Space Oddity/Make Falcon Scene Materials Editable")]
    public static void MakeFalconSceneMaterialsEditable()
    {
        Directory.CreateDirectory(EditableMaterialFolder);

        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int replacedSlots = 0;
        int createdMaterials = 0;

        foreach (Renderer renderer in renderers)
        {
            if (!IsFalconRenderer(renderer))
                continue;

            Material[] materials = renderer.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < materials.Length; i++)
            {
                Material source = materials[i];
                if (source == null)
                    continue;

                Material editable = GetOrCreateEditableCopy(source, ref createdMaterials);
                if (editable == source)
                    continue;

                materials[i] = editable;
                changed = true;
                replacedSlots++;
            }

            if (!changed)
                continue;

            Undo.RecordObject(renderer, "Make Falcon Materials Editable");
            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(renderer);
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Made Falcon materials editable. Created {createdMaterials} material asset(s), replaced {replacedSlots} scene material slot(s).");
    }

    [MenuItem("Tools/Space Oddity/List Scene Material Names")]
    public static void ListSceneMaterialNames()
    {
        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var names = new System.Collections.Generic.SortedSet<string>();

        foreach (Renderer renderer in renderers)
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material != null)
                    names.Add(CleanName(material.name));
            }
        }

        Debug.Log("Scene materials:\n" + string.Join("\n", names));
    }

    static Material GetOrCreateBlackMetallicMaterial()
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (existing != null)
            return existing;

        Directory.CreateDirectory("Assets/Falcon");

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader)
        {
            name = MaterialName,
            color = new Color(0.005f, 0.005f, 0.006f, 1f)
        };

        SetIfPresent(material, "_BaseColor", new Color(0.005f, 0.005f, 0.006f, 1f));
        SetIfPresent(material, "_Color", new Color(0.005f, 0.005f, 0.006f, 1f));
        SetIfPresent(material, "_Metallic", 1f);
        SetIfPresent(material, "_Smoothness", 0.88f);
        SetIfPresent(material, "_Glossiness", 0.88f);

        AssetDatabase.CreateAsset(material, MaterialPath);
        AssetDatabase.SaveAssets();
        return material;
    }

    static Material GetOrCreateEditableCopy(Material source, ref int createdMaterials)
    {
        string cleanName = CleanName(source.name);
        string path = $"{EditableMaterialFolder}/{ToSafeFileName(cleanName)}.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            return existing;

        Material copy = new Material(source)
        {
            name = cleanName
        };

        AssetDatabase.CreateAsset(copy, path);
        createdMaterials++;
        return copy;
    }

    static bool IsFalconRenderer(Renderer renderer)
    {
        Transform current = renderer.transform;
        while (current != null)
        {
            string name = current.name.ToLowerInvariant();
            if (name.Contains("falcon") || name.Contains("spacecraft"))
                return true;

            current = current.parent;
        }

        return false;
    }

    static void SetIfPresent(Material material, string property, Color value)
    {
        if (material.HasProperty(property))
            material.SetColor(property, value);
    }

    static void SetIfPresent(Material material, string property, float value)
    {
        if (material.HasProperty(property))
            material.SetFloat(property, value);
    }

    static string CleanName(string materialName)
    {
        return materialName.Replace(" (Instance)", string.Empty);
    }

    static string ToSafeFileName(string value)
    {
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
            value = value.Replace(invalidChar, '_');

        return value;
    }
}
