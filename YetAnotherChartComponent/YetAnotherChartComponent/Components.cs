using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	/// <summary>
	/// Background fill for the chart data area.
	/// </summary>
	public class Background : ChartComponent {
		#region properties
		/// <summary>
		/// The fill brush to use.
		/// </summary>
		public Brush Fill { get { return (Brush)GetValue(FillProperty); } set { SetValue(FillProperty, value); } }
		/// <summary>
		/// The path to attach geometry et al.
		/// </summary>
		protected Path Path { get; set; }
		/// <summary>
		/// The geometry to use for this component.
		/// </summary>
		protected RectangleGeometry Rectangle { get; set; }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="Fill"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty FillProperty = DependencyProperty.Register("Fill", typeof(Brush), typeof(Background), new PropertyMetadata(null));
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		public Background() {
			Rectangle = new RectangleGeometry();
			Path = new Path() {
				Data = Rectangle
			};
		}
		#endregion
		#region helpers
		void DoBindings(IChartEnterLeaveContext icelc) {
			BindTo(this, "Fill", Path, Path.FillProperty);
			#if false
			BindTo(this, "GridStroke", Grid, Path.StrokeProperty);
			BindTo(this, "GridStrokeThickness", Grid, Path.StrokeThicknessProperty);
			var bx = GetBindingExpression(GridVisibilityProperty);
			if (bx != null) {
				Grid.SetBinding(UIElement.VisibilityProperty, bx.ParentBinding);
			} else {
				BindTo(this, "GridVisibility", Grid, Path.VisibilityProperty);
			}
			#endif
		}
		#endregion
		/// <summary>
		/// Component is entering the chart.
		/// </summary>
		/// <param name="icelc">Context.</param>
		public override void Enter(IChartEnterLeaveContext icelc) {
			icelc.Add(Path);
			DoBindings(icelc);
		}
		/// <summary>
		/// Component is leaving the chart.
		/// </summary>
		/// <param name="icelc">Context.</param>
		public override void Leave(IChartEnterLeaveContext icelc) {
			icelc.Remove(Path);
		}
		/// <summary>
		/// Render the background.
		/// Uses NDC coordinates.
		/// </summary>
		/// <param name="icrc">Context.</param>
		public override void Render(IChartRenderContext icrc) {
			//if (!Dirty) return;
			Rectangle.Rect = new Windows.Foundation.Rect(0, 0, 1, 1);
		}
		/// <summary>
		/// Scale the NDC rectangle to the dimensions given.
		/// </summary>
		/// <param name="icrc">Context.</param>
		public override void Transforms(IChartRenderContext icrc) {
			var matx = new Matrix(icrc.SeriesArea.Width, 0, 0, icrc.SeriesArea.Height, icrc.SeriesArea.Left, icrc.SeriesArea.Top);
			Rectangle.Transform = new MatrixTransform() { Matrix = matx };
		}
	}
}
