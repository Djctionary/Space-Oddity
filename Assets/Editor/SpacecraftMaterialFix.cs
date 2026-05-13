using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SpacecraftMaterialFix
{
    const string SpacecraftName = "Spacecraft_Falcon";
    const string EditableMaterialFolder = "Assets/Falcon/EditableMaterials";

    [MenuItem("Tools/Space Oddity/Make Spacecraft Materials Editable")]
    public static void MakeSpacecraftMaterialsEditable()
    {
        GameObject spacecraft = GameObject.Find(SpacecraftName);
        if (spacecraft == null)
        {
            Debug.LogWarning("Spacecraft_Falcon was not found in the open scene.");
            return;
        }

        Directory.CreateDirectory(EditableMaterialFolder);

        int replacedSlots = 0;
        Renderer[] renderers = spacecraft.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null)
                    continue;

                Material editable = GetOrCreateEditableCopy(material);
                if (editable == material)
                    continue;

                materials[i] = editable;
                changed = true;
                replacedSlots++;
            }

            if (!changed)
                continue;

            Undo.RecordObject(renderer, "Make Spacecraft Materials Editable");
            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(renderer);
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Made Spacecraft_Falcon materials editable. Replaced {replacedSlots} material slot(s).");
    }

    static Material GetOrCreateEditableCopy(Material source)
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
        return copy;
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
