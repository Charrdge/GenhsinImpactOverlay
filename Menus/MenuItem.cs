using System.Windows.Forms;

using GameOverlay.Drawing;

namespace GenshinImpactOverlay.Menus
{
	public class MenuItem
	{
		public string Name { get; init; }

		private Image? Icon { get; set; }

		public Dictionary<Keys, Action>? Hotkeys { get; init; }

		public List<MenuItem>? ChildMenus { get; init; }

		public MenuItem(string name, string iconPath, Dictionary<Keys, Action>? hotkeys = null, List<MenuItem>? childMenus = null)
		{
			Hotkeys = hotkeys;
			ChildMenus = childMenus;
			Name = name ?? throw new ArgumentNullException(nameof(name));
			_iconPath = iconPath ?? throw new ArgumentNullException(nameof(iconPath));
		}

		private string _iconPath;
		public void DrawIcon(Graphics graphics, Rectangle rect, float opacity = 1)
		{
			Icon ??= new Image(graphics, _iconPath);

			graphics.DrawImage(Icon, rect, opacity);
		}
	}
}