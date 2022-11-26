using GameOverlay.Drawing;
using SharpDX.Direct2D1;

namespace GenshinImpactOverlay.GraphicWorkers
{
	public class SolidBrushHandler : IBrush
	{
		public Color Color { get; }

		private SolidBrush? Brush { get; set; }
		Brush IBrush.Brush { get => ((IBrush)Brush).Brush; set => ((IBrush)Brush).Brush = value; }

		public SolidBrushHandler(Color color) => Color = color;

		public void Create(Graphics gfx) => Brush = gfx.CreateSolidBrush(Color);

		public void Dispose() => Brush?.Dispose();

		public static implicit operator SolidBrush(SolidBrushHandler handler) => handler.Brush ?? throw new NullReferenceException($"{nameof(Brush)} not initialized");
	}
}