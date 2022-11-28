using System.Windows.Forms;

namespace GenshinImpactOverlay.EventsArgs
{
	internal class OnKeyUpEventArgs : EventArgs
	{
		public InputPriorityEnum InputPriority { get; }

		public Keys Key { get; }

		public string? System { get; }

		public OnKeyUpEventArgs(Keys key, InputPriorityEnum inputPriority, string? system = null)
		{
			Key = key;
			InputPriority = inputPriority;
			if (inputPriority == InputPriorityEnum.System)
			{
				if (system is not null) System = system;
				else InputPriority = InputPriorityEnum.Normal;
			}
		}
	}
}
