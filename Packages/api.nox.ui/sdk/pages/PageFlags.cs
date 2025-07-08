using System;

namespace Nox.UI {
	[Flags]
	public enum PageFlags {
		None      = 0,
		IsNew     = 1,
		IsRestore = 2,
		IsBack    = 4,
		IsForward = 8
	}
}