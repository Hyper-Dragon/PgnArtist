namespace PgnArtist.Generic;

public abstract record GlobalSettingsBase
{
    public virtual bool ShouldDisplayHeader { get; } = true;
    public virtual bool ShouldBeepOnEnd { get; } = true;
    public virtual bool ShouldDisplayParams { get; } = true;
    public virtual bool ShouldDisplaySectionHeaders { get; } = true;
    public virtual bool ShouldExecutePreRun { get; } = true;
    public virtual bool ShouldExecutePostRun { get; } = true;
    public virtual bool ShouldForceSilentMode { get; } = false;

}

