using GameOverlay.Drawing;

namespace GenshinImpactOverlay.GraphicWorkers
{
	public class FontHandler
	{
		public string FontFamilyName { get; }

		public float Size { get; }

		public bool Bold { get; }

		public bool Italic { get; }

		public bool WordWrapping { get; }

		private Font? Font { get; set; }

		public bool IsInitialized => Font is not null;

		public FontHandler(string fontFamilyName, float size, bool bold = false, bool italic = false, bool wordWrapping = false)
		{
			FontFamilyName = fontFamilyName;
			Size = size;
			Bold = bold;
			Italic = italic;
			WordWrapping = wordWrapping;
		}

		public void Create(Graphics gfx) => Font = gfx.CreateFont(FontFamilyName, Size, Bold, Italic, WordWrapping);

		public void Dispose() => Font?.Dispose();

		public static implicit operator Font(FontHandler fontHandler) => 
			fontHandler.Font ?? throw new NullReferenceException($"{nameof(Font)} not initialized");
	}
}