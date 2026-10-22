namespace CursorHub.Core;
internal enum PlaybackPhase { Stopped, Starting, Playing, Stopping, Faulted }
internal sealed class PlaybackState
{
    private readonly object gate = new();
    private long revision;
    private bool wantsPlay;
    private PlaybackPhase phase;
    public (long Revision, bool Requested, PlaybackPhase Phase) Snapshot { get { lock(gate)return(revision,wantsPlay,phase); } }
    public long Request(bool play) { lock(gate){wantsPlay=play;phase=play?PlaybackPhase.Starting:PlaybackPhase.Stopping;return ++revision;} }
    public void Complete(long expected,bool active) { lock(gate){if(expected==revision)phase=active?PlaybackPhase.Playing:PlaybackPhase.Stopped;} }
    public void Fail(long expected) { lock(gate){if(expected==revision){wantsPlay=false;phase=PlaybackPhase.Faulted;}} }
}
