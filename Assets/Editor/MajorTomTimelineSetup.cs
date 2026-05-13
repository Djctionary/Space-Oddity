using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public static class MajorTomTimelineSetup
{
    const string RigName = "Major Tom Timeline Rig";
    const string TimelineFolder = "Assets/Cinematics/MajorTom";
    const string ShotTimelineFolder = TimelineFolder + "/Shots";
    const string MasterTimelinePath = TimelineFolder + "/MajorTom_Master.playable";

    static readonly string[] ShotNames =
    {
        "Shot_01_Exterior_Approach",
        "Shot_02_Cabin",
        "Shot_03_Hatch_Opens",
        "Shot_04_Half_Outside",
        "Shot_05_Floating",
        "Shot_06_Pull_Away",
        "Shot_07_Stars",
        "Shot_08_Silent_Wide",
    };

    [MenuItem("Tools/Space Oddity/Create Editable Timeline Rig")]
    public static void CreateEditableTimelineRig()
    {
        Directory.CreateDirectory(TimelineFolder);
        Directory.CreateDirectory(ShotTimelineFolder);

        GameObject rig = GetOrCreateGameObject(RigName);
        Transform actorsRoot = GetOrCreateChild(rig.transform, "Actors");
        Transform shotsRoot = GetOrCreateChild(rig.transform, "Shots");

        GameObject spacecraft = FindOrCreateActor("Spacecraft_Falcon", actorsRoot);
        GameObject astronaut = FindOrCreateActor("Astronaut", actorsRoot);
        GameObject door = FindOrCreateActor("HatchDoor_Pivot", actorsRoot);
        GameObject earth = FindOrCreateActor("Earth_Cinematic", actorsRoot);

        CreateShots(shotsRoot, spacecraft, astronaut, door, earth);
        CreateMasterTimeline(rig, shotsRoot);

        Selection.activeGameObject = rig;
        EditorGUIUtility.PingObject(rig);
        EditorUtility.SetDirty(rig);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    [MenuItem("Tools/Space Oddity/Clear Generated Shot Animation Clips")]
    public static void ClearGeneratedShotAnimationClips()
    {
        int removedClips = 0;
        foreach (string shotName in ShotNames)
        {
            TimelineAsset timeline = GetOrCreateShotTimeline(shotName);
            foreach (TrackAsset track in timeline.GetOutputTracks())
            {
                if (!(track is AnimationTrack))
                    continue;

                var clipsToDelete = new System.Collections.Generic.List<TimelineClip>();
                foreach (TimelineClip clip in track.GetClips())
                {
                    if (IsGeneratedAnimationClip(clip, shotName))
                        clipsToDelete.Add(clip);
                }

                foreach (TimelineClip clip in clipsToDelete)
                {
                    track.DeleteClip(clip);
                    removedClips++;
                }
            }

            EditorUtility.SetDirty(timeline);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Removed {removedClips} generated shot animation clip(s). Empty Animation Tracks can now use Timeline record mode.");
    }

    [MenuItem("Tools/Space Oddity/Fix Shot Cameras For Game View")]
    public static void FixShotCamerasForGameView()
    {
        GameObject rig = GameObject.Find(RigName);
        if (rig == null)
        {
            Debug.LogWarning("Major Tom Timeline Rig was not found.");
            return;
        }

        Transform shotsRoot = rig.transform.Find("Shots");
        if (shotsRoot == null)
        {
            Debug.LogWarning("Shots root was not found under Major Tom Timeline Rig.");
            return;
        }

        int fixedCameras = 0;
        foreach (string shotName in ShotNames)
        {
            Transform shotRoot = shotsRoot.Find(shotName);
            if (shotRoot == null)
                continue;

            Transform cameraTransform = shotRoot.Find("Camera");
            if (cameraTransform == null)
                continue;

            GameObject cameraObject = cameraTransform.gameObject;
            Undo.RecordObject(cameraObject, "Fix Shot Camera Game View");
            cameraObject.SetActive(true);

            Camera camera = GetOrAddComponent<Camera>(cameraObject);
            Undo.RecordObject(camera, "Fix Shot Camera Game View");
            camera.enabled = true;
            camera.targetDisplay = 0;
            camera.rect = new Rect(0f, 0f, 1f, 1f);
            camera.depth = 10 + fixedCameras;

            EditorUtility.SetDirty(cameraObject);
            EditorUtility.SetDirty(camera);
            fixedCameras++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Fixed {fixedCameras} shot camera(s) for Display 1 Game View preview.");
    }

    [MenuItem("Tools/Space Oddity/Preview Selected Shot Camera Only")]
    public static void PreviewSelectedShotCameraOnly()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogWarning("Select a Shot GameObject first.");
            return;
        }

        Transform selectedShot = FindShotRoot(selected.transform);
        if (selectedShot == null)
        {
            Debug.LogWarning("The selection is not inside a Major Tom shot.");
            return;
        }

        GameObject rig = GameObject.Find(RigName);
        Transform shotsRoot = rig != null ? rig.transform.Find("Shots") : null;
        if (shotsRoot == null)
        {
            Debug.LogWarning("Shots root was not found under Major Tom Timeline Rig.");
            return;
        }

        int cameraCount = 0;
        foreach (string shotName in ShotNames)
        {
            Transform shotRoot = shotsRoot.Find(shotName);
            if (shotRoot == null)
                continue;

            Transform cameraTransform = shotRoot.Find("Camera");
            if (cameraTransform == null)
                continue;

            GameObject cameraObject = cameraTransform.gameObject;
            bool shouldEnable = shotRoot == selectedShot;
            Undo.RecordObject(cameraObject, "Preview Selected Shot Camera Only");
            cameraObject.SetActive(shouldEnable);

            Camera camera = GetOrAddComponent<Camera>(cameraObject);
            Undo.RecordObject(camera, "Preview Selected Shot Camera Only");
            camera.enabled = shouldEnable;
            camera.targetDisplay = 0;
            camera.depth = shouldEnable ? 100f : -100f;

            EditorUtility.SetDirty(cameraObject);
            EditorUtility.SetDirty(camera);
            cameraCount++;
        }

        Selection.activeTransform = selectedShot;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Previewing only {selectedShot.name}. Updated {cameraCount} shot camera(s).");
    }

    static void CreateMasterTimeline(GameObject rig, Transform shotsRoot)
    {
        Transform masterRoot = GetOrCreateChild(rig.transform, "Master Timeline");
        PlayableDirector masterDirector = GetOrAddComponent<PlayableDirector>(masterRoot.gameObject);
        TimelineAsset masterTimeline = GetOrCreateMasterTimeline();

        masterDirector.playableAsset = masterTimeline;
        masterDirector.playOnAwake = false;
        masterDirector.timeUpdateMode = DirectorUpdateMode.GameTime;

        ShotDirectorTrack shotTrack = GetOrCreateTrack<ShotDirectorTrack>(masterTimeline, "Shots");
        EnsureMasterShotClips(shotTrack, masterDirector, shotsRoot);

        EditorUtility.SetDirty(masterTimeline);
        EditorUtility.SetDirty(masterDirector);
    }

    static void CreateShots(Transform shotsRoot, GameObject spacecraft, GameObject astronaut, GameObject door, GameObject earth)
    {
        for (int i = 0; i < ShotNames.Length; i++)
        {
            string shotName = ShotNames[i];
            Transform shotRoot = GetOrCreateChild(shotsRoot, shotName);
            GameObject shotCamera = GetOrCreateShotCamera(shotRoot, i);
            PlayableDirector director = GetOrAddComponent<PlayableDirector>(shotRoot.gameObject);
            TimelineAsset timeline = GetOrCreateShotTimeline(shotName);

            director.playableAsset = timeline;
            director.playOnAwake = false;
            director.timeUpdateMode = DirectorUpdateMode.GameTime;

            ConfigureShotTimeline(timeline, director, shotName, shotCamera, spacecraft, astronaut, door, earth);
            EditorUtility.SetDirty(timeline);
            EditorUtility.SetDirty(director);
        }
    }

    static TimelineAsset GetOrCreateShotTimeline(string shotName)
    {
        string path = ShotTimelineFolder + "/" + shotName + ".playable";
        TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
        if (timeline != null)
            return timeline;

        timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        AssetDatabase.CreateAsset(timeline, path);
        return timeline;
    }

    static TimelineAsset GetOrCreateMasterTimeline()
    {
        TimelineAsset existing = AssetDatabase.LoadAssetAtPath<TimelineAsset>(MasterTimelinePath);
        if (existing != null)
            AssetDatabase.DeleteAsset(MasterTimelinePath);

        TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(MasterTimelinePath);
        timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        AssetDatabase.CreateAsset(timeline, MasterTimelinePath);
        return timeline;
    }

    static void EnsureMasterShotClips(ShotDirectorTrack shotTrack, PlayableDirector masterDirector, Transform shotsRoot)
    {
        double shotDuration = 6.0;
        for (int i = 0; i < ShotNames.Length; i++)
        {
            string shotName = ShotNames[i];
            Transform shotRoot = shotsRoot.Find(shotName);
            if (shotRoot == null)
                continue;

            PlayableDirector shotDirector = shotRoot.GetComponent<PlayableDirector>();
            if (shotDirector == null)
                continue;

            TimelineClip clip = GetOrCreateShotDirectorClip(shotTrack, shotName);
            clip.displayName = shotName;
            clip.start = i * shotDuration;
            clip.duration = shotDuration;

            ShotDirectorPlayableAsset shotAsset = (ShotDirectorPlayableAsset)clip.asset;
            PropertyName referenceName = new PropertyName("Master_" + shotName);
            shotAsset.shotDirector.exposedName = referenceName;

            masterDirector.SetReferenceValue(referenceName, shotDirector);
            EditorUtility.SetDirty(shotAsset);
        }
    }

    static TimelineClip GetOrCreateShotDirectorClip(ShotDirectorTrack shotTrack, string clipName)
    {
        foreach (TimelineClip clip in shotTrack.GetClips())
        {
            if (clip.displayName == clipName)
                return clip;
        }

        return shotTrack.CreateClip<ShotDirectorPlayableAsset>();
    }

    static bool IsGeneratedAnimationClip(TimelineClip clip, string shotName)
    {
        if (clip == null || clip.animationClip == null)
            return false;

        string clipName = clip.animationClip.name;
        return clipName == "Camera_" + shotName
            || clipName == "Spacecraft_" + shotName
            || clipName == "Astronaut_" + shotName
            || clipName == "Door_" + shotName
            || clipName == "Earth_" + shotName;
    }

    static void ConfigureShotTimeline(TimelineAsset timeline, PlayableDirector director, string shotName, GameObject shotCamera, GameObject spacecraft, GameObject astronaut, GameObject door, GameObject earth)
    {
        AnimationTrack cameraTrack = GetOrCreateTrack<AnimationTrack>(timeline, "Camera");
        director.SetGenericBinding(cameraTrack, GetOrAddComponent<Animator>(shotCamera));

        AnimationTrack spacecraftTrack = GetOrCreateTrack<AnimationTrack>(timeline, "Animation - Spacecraft");
        director.SetGenericBinding(spacecraftTrack, GetOrAddComponent<Animator>(spacecraft));

        AnimationTrack astronautTrack = GetOrCreateTrack<AnimationTrack>(timeline, "Animation - Astronaut");
        director.SetGenericBinding(astronautTrack, GetOrAddComponent<Animator>(astronaut));

        AnimationTrack doorTrack = GetOrCreateTrack<AnimationTrack>(timeline, "Animation - Door");
        director.SetGenericBinding(doorTrack, GetOrAddComponent<Animator>(door));

        AnimationTrack earthTrack = GetOrCreateTrack<AnimationTrack>(timeline, "Animation - Earth");
        director.SetGenericBinding(earthTrack, GetOrAddComponent<Animator>(earth));

        GetOrCreateTrack<AudioTrack>(timeline, "Music");
        GetOrCreateTrack<AudioTrack>(timeline, "SFX");

        ActivationTrack activationTrack = GetOrCreateTrack<ActivationTrack>(timeline, "Activation - Camera");
        director.SetGenericBinding(activationTrack, shotCamera);
        EnsureDefaultClip(activationTrack, "Enable " + shotName + " Camera");
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

    static void EnsureDefaultClip(TrackAsset track, string clipName)
    {
        if (HasClip(track, clipName))
            return;

        TimelineClip clip = track.CreateDefaultClip();
        clip.displayName = clipName;
        clip.start = 0.0;
        clip.duration = 6.0;
    }

    static bool HasClip(TrackAsset track, string clipName)
    {
        foreach (TimelineClip clip in track.GetClips())
        {
            if (clip.displayName == clipName)
                return true;
        }

        return false;
    }

    static GameObject GetOrCreateShotCamera(Transform shotRoot, int shotIndex)
    {
        GameObject cameraObject = GetOrCreateChild(shotRoot, "Camera").gameObject;
        Camera camera = GetOrAddComponent<Camera>(cameraObject);
        camera.enabled = true;
        camera.fieldOfView = 45f;
        camera.nearClipPlane = 0.03f;
        camera.farClipPlane = 2000f;
        camera.targetDisplay = 0;
        camera.rect = new Rect(0f, 0f, 1f, 1f);
        camera.depth = 10 + shotIndex;
        cameraObject.transform.localPosition = new Vector3(0f, 2f, -10f - shotIndex);
        cameraObject.transform.localRotation = Quaternion.Euler(8f, 25f, 0f);
        cameraObject.SetActive(true);
        return cameraObject;
    }

    static GameObject FindOrCreateActor(string name, Transform parent)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
        {
            existing.transform.SetParent(parent, true);
            return existing;
        }

        GameObject actor = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(actor, "Create " + name);
        actor.transform.SetParent(parent, false);
        return actor;
    }

    static GameObject GetOrCreateGameObject(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
            return existing;

        GameObject gameObject = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(gameObject, "Create " + name);
        return gameObject;
    }

    static Transform GetOrCreateChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing;

        GameObject child = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(child, "Create " + name);
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    static Transform FindShotRoot(Transform start)
    {
        Transform current = start;
        while (current != null)
        {
            for (int i = 0; i < ShotNames.Length; i++)
            {
                if (current.name == ShotNames[i])
                    return current;
            }

            current = current.parent;
        }

        return null;
    }

    static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component != null)
            return component;

        return Undo.AddComponent<T>(gameObject);
    }
}
