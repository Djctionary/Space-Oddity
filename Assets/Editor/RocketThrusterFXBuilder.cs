using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public static class RocketThrusterFXBuilder
{
    const string SpacecraftName = "Spacecraft_Falcon";
    const string EffectName = "Rocket_Thruster_FX";
    const string MaterialFolder = "Assets/Cinematics/MajorTom/Materials";
    const string ParticleMaterialPath = MaterialFolder + "/Thruster_Particle.mat";

    [MenuItem("Tools/Space Oddity/Create Rocket Thruster FX")]
    public static void CreateRocketThrusterFX()
    {
        GameObject spacecraft = GameObject.Find(SpacecraftName);
        Transform parent = spacecraft != null ? spacecraft.transform : null;

        GameObject existing = parent != null ? FindDirectChild(parent, EffectName) : GameObject.Find(EffectName);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }

        GameObject root = new GameObject(EffectName);
        Undo.RegisterCreatedObjectUndo(root, "Create Rocket Thruster FX");

        if (parent != null)
        {
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(-1.25f, 0f, 0f);
            root.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            root.transform.localScale = Vector3.one;
        }
        else
        {
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, -90f, 0f));
        }

        CreateFlameCore(root.transform);
        CreateFlameGlow(root.transform);
        CreateHeatHaze(root.transform);
        CreateSparks(root.transform);
        CreateThrusterLight(root.transform);

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    [MenuItem("Tools/Space Oddity/Add Rocket Thruster FX To Selected Shot")]
    public static void AddRocketThrusterFXToSelectedShot()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogWarning("Select a Shot GameObject with a PlayableDirector first.");
            return;
        }

        PlayableDirector director = selected.GetComponent<PlayableDirector>();
        TimelineAsset timeline = director != null ? director.playableAsset as TimelineAsset : null;
        if (director == null || timeline == null)
        {
            Debug.LogWarning("The selected GameObject does not have a Timeline PlayableDirector.");
            return;
        }

        GameObject thruster = GameObject.Find(EffectName);
        if (thruster == null)
        {
            CreateRocketThrusterFX();
            thruster = GameObject.Find(EffectName);
        }

        if (thruster == null)
        {
            Debug.LogWarning("Rocket_Thruster_FX could not be created or found.");
            return;
        }

        ControlTrack track = GetOrCreateTrack<ControlTrack>(timeline, "FX - Rocket Thruster");
        TimelineClip clip = GetOrCreateControlClip(track, "Rocket Thruster FX");
        clip.start = 0.0;
        clip.duration = 6.0;

        ControlPlayableAsset asset = (ControlPlayableAsset)clip.asset;
        PropertyName referenceName = new PropertyName(selected.name + "_RocketThrusterFX");
        asset.sourceGameObject.exposedName = referenceName;
        asset.active = true;
        asset.updateParticle = true;
        asset.updateDirector = false;
        asset.updateITimeControl = false;
        asset.searchHierarchy = true;

        director.SetReferenceValue(referenceName, thruster);

        EditorUtility.SetDirty(asset);
        EditorUtility.SetDirty(timeline);
        EditorUtility.SetDirty(director);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Added Rocket_Thruster_FX to the selected Shot Timeline using a Control Track.");
    }

    static GameObject CreateFlameCore(Transform parent)
    {
        GameObject gameObject = CreateParticleObject(parent, "Flame_Core");
        ParticleSystem particleSystem = gameObject.GetComponent<ParticleSystem>();

        ParticleSystem.MainModule main = particleSystem.main;
        main.duration = 1f;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.26f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(7.5f, 13f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.18f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.15f, 0.55f, 1f, 0.95f),
            new Color(1f, 0.88f, 0.42f, 0.9f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 600;

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.rateOverTime = 260f;

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 6f;
        shape.radius = 0.055f;
        shape.length = 0.18f;

        ParticleSystem.ColorOverLifetimeModule color = particleSystem.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.9f, 0.96f, 1f), 0f),
                new GradientColorKey(new Color(0.18f, 0.55f, 1f), 0.38f),
                new GradientColorKey(new Color(1f, 0.48f, 0.12f), 1f),
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.85f, 0.45f),
                new GradientAlphaKey(0f, 1f),
            });
        color.color = gradient;

        ParticleSystem.SizeOverLifetimeModule size = particleSystem.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.72f, 1f, 0.9f));

        ParticleSystem.NoiseModule noise = particleSystem.noise;
        noise.enabled = true;
        noise.strength = 0.045f;
        noise.frequency = 2.6f;
        noise.scrollSpeed = 1.15f;

        ParticleSystem.VelocityOverLifetimeModule velocity = particleSystem.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        AnimationCurve xCurve = AnimationCurve.Linear(0f, -0.15f, 1f, 0.15f);
        AnimationCurve yCurve = AnimationCurve.Linear(0f, 0.15f, 1f, -0.15f);
        AnimationCurve zCurve = AnimationCurve.Linear(0f, -0.8f, 1f, 0.8f);
        velocity.x = new ParticleSystem.MinMaxCurve(1f, xCurve);
        velocity.y = new ParticleSystem.MinMaxCurve(1f, yCurve);
        velocity.z = new ParticleSystem.MinMaxCurve(1f, zCurve);

        ParticleSystemRenderer renderer = gameObject.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortMode = ParticleSystemSortMode.OldestInFront;
        renderer.minParticleSize = 0.003f;
        renderer.maxParticleSize = 0.08f;
        renderer.material = GetParticleMaterial();

        return gameObject;
    }

    static GameObject CreateFlameGlow(Transform parent)
    {
        GameObject gameObject = CreateParticleObject(parent, "Blue_Glow");
        ParticleSystem particleSystem = gameObject.GetComponent<ParticleSystem>();

        ParticleSystem.MainModule main = particleSystem.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.42f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.8f, 5.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.52f);
        main.startColor = new Color(0.12f, 0.48f, 1f, 0.22f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 180;

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.rateOverTime = 85f;

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 13f;
        shape.radius = 0.09f;
        shape.length = 0.18f;

        ParticleSystemRenderer renderer = gameObject.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.minParticleSize = 0.006f;
        renderer.maxParticleSize = 0.18f;
        renderer.material = GetParticleMaterial();
        return gameObject;
    }

    static GameObject CreateHeatHaze(Transform parent)
    {
        GameObject gameObject = CreateParticleObject(parent, "Soft_Heat_Haze");
        ParticleSystem particleSystem = gameObject.GetComponent<ParticleSystem>();

        ParticleSystem.MainModule main = particleSystem.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.75f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 4.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.55f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-3.14f, 3.14f);
        main.startColor = new Color(0.32f, 0.55f, 0.75f, 0.055f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 180;

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.rateOverTime = 32f;

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.13f;
        shape.length = 0.35f;

        ParticleSystem.ColorOverLifetimeModule color = particleSystem.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.55f, 0.78f, 1f), 0f),
                new GradientColorKey(new Color(0.15f, 0.28f, 0.42f), 1f),
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.08f, 0.35f),
                new GradientAlphaKey(0f, 1f),
            });
        color.color = gradient;

        ParticleSystem.SizeOverLifetimeModule size = particleSystem.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.65f, 1f, 1.8f));

        ParticleSystem.NoiseModule noise = particleSystem.noise;
        noise.enabled = true;
        noise.strength = 0.16f;
        noise.frequency = 0.75f;
        noise.scrollSpeed = 0.35f;

        ParticleSystemRenderer renderer = gameObject.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = GetParticleMaterial();
        return gameObject;
    }

    static GameObject CreateSparks(Transform parent)
    {
        GameObject gameObject = CreateParticleObject(parent, "Hot_Sparks");
        ParticleSystem particleSystem = gameObject.GetComponent<ParticleSystem>();

        ParticleSystem.MainModule main = particleSystem.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.65f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 9f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.045f);
        main.startColor = new Color(1f, 0.75f, 0.28f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 160;

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.rateOverTime = 22f;

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 24f;
        shape.radius = 0.08f;

        ParticleSystemRenderer renderer = gameObject.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 2.2f;
        renderer.velocityScale = 0.35f;
        renderer.material = GetParticleMaterial();
        return gameObject;
    }

    static void CreateThrusterLight(Transform parent)
    {
        GameObject gameObject = new GameObject("Thruster_Light");
        Undo.RegisterCreatedObjectUndo(gameObject, "Create Thruster Light");
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.localPosition = new Vector3(0f, 0f, -0.15f);

        Light light = gameObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.28f, 0.62f, 1f, 1f);
        light.intensity = 3.5f;
        light.range = 5f;
    }

    static GameObject CreateParticleObject(Transform parent, string name)
    {
        GameObject gameObject = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(gameObject, "Create " + name);
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.localPosition = Vector3.zero;
        gameObject.transform.localRotation = Quaternion.identity;
        gameObject.transform.localScale = Vector3.one;
        gameObject.AddComponent<ParticleSystem>();
        return gameObject;
    }

    static T GetOrCreateTrack<T>(TimelineAsset timeline, string trackName) where T : TrackAsset, new()
    {
        foreach (TrackAsset track in timeline.GetOutputTracks())
        {
            if (track is T typedTrack && track.name == trackName)
                return typedTrack;
        }

        return timeline.CreateTrack<T>(null, trackName);
    }

    static TimelineClip GetOrCreateControlClip(ControlTrack track, string clipName)
    {
        foreach (TimelineClip clip in track.GetClips())
        {
            if (clip.displayName == clipName)
                return clip;
        }

        TimelineClip newClip = track.CreateClip<ControlPlayableAsset>();
        newClip.displayName = clipName;
        return newClip;
    }

    static Material GetParticleMaterial()
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(ParticleMaterialPath);
        if (existing != null)
            return existing;

        Directory.CreateDirectory(MaterialFolder);

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material material = new Material(shader)
        {
            name = "Thruster_Particle"
        };

        if (material.HasProperty("_BaseColor"))
        material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_SoftParticlesEnabled"))
            material.SetFloat("_SoftParticlesEnabled", 1f);
        if (material.HasProperty("_SoftParticlesNearFadeDistance"))
            material.SetFloat("_SoftParticlesNearFadeDistance", 0.05f);
        if (material.HasProperty("_SoftParticlesFarFadeDistance"))
            material.SetFloat("_SoftParticlesFarFadeDistance", 1.5f);

        AssetDatabase.CreateAsset(material, ParticleMaterialPath);
        AssetDatabase.SaveAssets();
        return material;
    }

    static GameObject FindDirectChild(Transform parent, string childName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child.gameObject;
        }

        return null;
    }
}
