using UnityEngine.Playables;

public sealed class ShotDirectorPlayableBehaviour : PlayableBehaviour
{
    public PlayableDirector ShotDirector { get; set; }
    public double Duration { get; set; }

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (ShotDirector == null)
            return;

        ShotDirector.time = 0.0;
        ShotDirector.Evaluate();
        ShotDirector.Play();
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (ShotDirector == null)
            return;

        double shotTime = playable.GetTime();
        if (Duration > 0.0)
            shotTime = System.Math.Min(shotTime, Duration);

        ShotDirector.time = shotTime;
        ShotDirector.Evaluate();
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        if (ShotDirector == null)
            return;

        ShotDirector.Pause();
    }
}
