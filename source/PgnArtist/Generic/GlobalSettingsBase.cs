using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PgnArtist.Generic
{
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
}
