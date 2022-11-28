using System.Windows.Forms;

namespace GenshinImpactOverlay.EventsArgs
{
	internal class OnKeyUpEventArgs : EventArgs
	{
		public InputPriority InputPriority { get; }

		public Keys Key { get; }

		public string? System { get; }

		public OnKeyUpEventArgs(Keys key, InputPriority inputPriority, string? system = null)
		{
			Key = key;
			InputPriority = inputPriority;
			if (inputPriority == InputPriority.System)
			{
				if (system is not null) System = system;
				else InputPriority = InputPriority.Normal;
			}
		}
	}
}
