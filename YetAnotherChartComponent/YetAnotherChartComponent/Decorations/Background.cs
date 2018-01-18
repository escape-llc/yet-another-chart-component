using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region Background
	/// <summary>
	/// Background fill for the chart data area.
	/// </summary>
	public class Background : ChartComponent, IRequireEnterLeave, IRequireRender, IRequireTransforms {
		#region properties
		/// <summary>
		/// The style to use for Path geometry.
		/// </summary>
		public Style PathStyle { get { return (Style)GetValue(PathStyleProperty); } set { SetValue(PathStyleProperty, value); } }
		/// <summary>
		/// The path to attach geometry et al.
		/// </summary>
		protected Path Path { get; set; }
		/// <summary>
		/// The geometry to use for this component.
		/// </summary>
		protected RectangleGeometry Rectangle { get; set; }
		/// <summary>
		/// The layer to manage components.
		/// </summary>
		protected IChartLayer Layer { get; set; }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="PathStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty PathStyleProperty = DependencyProperty.Register("PathStyle", typeof(Style), typeof(Background), new PropertyMetadata(null));
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
			BindTo(this, "PathStyle", Path, Path.StyleProperty);
		}
		#endregion
		#region extensions
		/// <summary>
		/// Component is entering the chart.
		/// </summary>
		/// <param name="icelc">Context.</param>
		void IRequireEnterLeave.Enter(IChartEnterLeaveContext icelc) {
			Layer = icelc.CreateLayer(Path);
			DoBindings(icelc);
		}
		/// <summary>
		/// Component is leaving the chart.
		/// </summary>
		/// <param name="icelc">Context.</param>
		void IRequireEnterLeave.Leave(IChartEnterLeaveContext icelc) {
			icelc.DeleteLayer(Layer);
			Layer = null;
		}
		/// <summary>
		/// Render the background.
		/// Uses NDC coordinates.
		/// </summary>
		/// <param name="icrc">Context.</param>
		void IRequireRender.Render(IChartRenderContext icrc) {
			//if (!Dirty) return;
			Rectangle.Rect = new Windows.Foundation.Rect(0, 0, 1, 1);
		}
		/// <summary>
		/// Scale the NDC rectangle to the dimensions given.
		/// </summary>
		/// <param name="icrc">Context.</param>
		void IRequireTransforms.Transforms(IChartRenderContext icrc) {
			var matx = MatrixSupport.ProjectionFor(icrc.SeriesArea);
			Rectangle.Transform = new MatrixTransform() { Matrix = matx };
		}
		#endregion
	}
	#endregion
}
