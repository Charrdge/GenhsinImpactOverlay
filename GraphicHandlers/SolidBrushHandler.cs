using GameOverlay.Drawing;

namespace GenshinImpactOverlay.GraphicWorkers
{
	public class SolidBrushHandler
	{
		public Color Color { get; }

		private SolidBrush? Brush { get; set; }

		public SolidBrushHandler(Color color) => Color = color;

		public void Create(Graphics gfx) => Brush = gfx.CreateSolidBrush(Color);

		public void Dispose() => Brush?.Dispose();

		public static implicit operator SolidBrush(SolidBrushHandler worker) => worker.Brush ?? throw new NullReferenceException($"{nameof(Brush)} not initialized");
	}
}