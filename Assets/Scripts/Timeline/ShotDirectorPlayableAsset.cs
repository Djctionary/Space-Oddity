using System;
using UnityEngine;
using UnityEngine.Playables;

[Serializable]
public sealed class ShotDirectorPlayableAsset : PlayableAsset
{
    public ExposedReference<PlayableDirector> shotDirector;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        ScriptPlayable<ShotDirectorPlayableBehaviour> playable = ScriptPlayable<ShotDirectorPlayableBehaviour>.Create(graph);
        ShotDirectorPlayableBehaviour behaviour = playable.GetBehaviour();
        behaviour.ShotDirector = shotDirector.Resolve(graph.GetResolver());
        behaviour.Duration = duration;
        return playable;
    }
}
