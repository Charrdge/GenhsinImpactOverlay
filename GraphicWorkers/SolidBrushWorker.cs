using GameOverlay.Drawing;

namespace GenshinImpactOverlay.GraphicWorkers
{
	public class SolidBrushWorker
	{
		public Color Color { get; }

		private SolidBrush? Brush { get; set; }

		public SolidBrushWorker(Color color) => Color = color;

		public void Create(Graphics gfx) => Brush = gfx.CreateSolidBrush(Color);

		public void Dispose() => Brush?.Dispose();

		public static implicit operator SolidBrush(SolidBrushWorker worker) => worker.Brush ?? throw new NullReferenceException($"{nameof(Brush)} not initialized");
	}
}