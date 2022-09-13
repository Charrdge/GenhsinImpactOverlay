using GameOverlay.Drawing;

namespace GenshinImpactOverlay.GraphicWorkers
{
	public class FontWorker
	{
		public string FontFamilyName { get; }

		public float Size { get; }

		public bool Bold { get; }

		public bool Italic { get; }

		public bool WordWrapping { get; }

		private Font? Font { get; set; }

		public FontWorker(string fontFamilyName, float size, bool bold = false, bool italic = false, bool wordWrapping = false)
		{
			FontFamilyName = fontFamilyName;
			Size = size;
			Bold = bold;
			Italic = italic;
			WordWrapping = wordWrapping;
		}

		public void Create(Graphics gfx) => Font = gfx.CreateFont(FontFamilyName, Size, Bold, Italic, WordWrapping);

		public void Dispose() => Font?.Dispose();

		public static implicit operator Font(FontWorker fontWorker) => fontWorker.Font ?? throw new NullReferenceException($"{nameof(Font)} not initialized");
	}
}