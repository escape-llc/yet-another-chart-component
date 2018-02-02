using System;
using System.ComponentModel.DataAnnotations;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region ValueAxisGrid
	/// <summary>
	/// Grid lines for the value axis.
	/// </summary>
	public class ValueAxisGrid : ChartComponent, IRequireChartTheme, IRequireEnterLeave, IRequireRender, IRequireTransforms {
		#region properties
		/// <summary>
		/// The style to use for Path geometry.
		/// </summary>
		public Style PathStyle { get { return (Style)GetValue(PathStyleProperty); } set { SetValue(PathStyleProperty, value); } }
		/// <summary>
		/// Holder for IRequireChartTheme interface.
		/// </summary>
		public IChartTheme Theme { get; set; }
		/// <summary>
		/// Component name of value axis.
		/// Referenced component MUST implement IChartAxis.
		/// </summary>
		public String ValueAxisName { get; set; }
		/// <summary>
		/// The dereferenced value axis.
		/// </summary>
		protected IChartAxis ValueAxis { get; set; }
		/// <summary>
		/// Path for the grid lines.
		/// </summary>
		protected Path Grid { get; set; }
		/// <summary>
		/// Geometry for the grid lines.
		/// </summary>
		protected GeometryGroup GridGeometry { get; set; }
		/// <summary>
		/// The layer for components.
		/// </summary>
		protected IChartLayer Layer { get; set; }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="PathStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty PathStyleProperty = DependencyProperty.Register(nameof(PathStyle), typeof(Style), typeof(ValueAxisGrid), new PropertyMetadata(null));
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// Initialize geometry and path.
		/// </summary>
		public ValueAxisGrid() {
			Grid = new Path();
			GridGeometry = new GeometryGroup();
			Grid.Data = GridGeometry;
		}
		#endregion
		#region helpers
		/// <summary>
		/// Dereference the ValueAxisName.
		/// </summary>
		/// <param name="iccc"></param>
		void EnsureAxes(IChartComponentContext iccc) {
			if (ValueAxis == null && !String.IsNullOrEmpty(ValueAxisName)) {
				ValueAxis = iccc.Find(ValueAxisName) as IChartAxis;
			} else {
				if (iccc is IChartErrorInfo icei) {
					icei.Report(new ChartValidationResult(NameOrType(), $"Value axis '{ValueAxisName}' was not found", new[] { nameof(ValueAxis), nameof(ValueAxisName) }));
				}
			}
		}
		/// <summary>
		/// Apply bindings to internal elements.
		/// </summary>
		/// <param name="icelc"></param>
		void DoBindings(IChartEnterLeaveContext icelc) {
			AssignFromRef(icelc as IChartErrorInfo, NameOrType(), nameof(PathStyle), nameof(Theme.PathGridValue),
				PathStyle == null, Theme != null, Theme.PathGridValue != null,
				() => PathStyle = Theme.PathGridValue
			);
			BindTo(this, nameof(PathStyle), Grid, FrameworkElement.StyleProperty);
			ApplyBinding(this, nameof(Visibility), Grid, UIElement.VisibilityProperty);
		}
		#endregion
		#region extensions
		void IRequireEnterLeave.Enter(IChartEnterLeaveContext icelc) {
			EnsureAxes(icelc as IChartComponentContext);
			Layer = icelc.CreateLayer(Grid);
			DoBindings(icelc);
		}
		void IRequireEnterLeave.Leave(IChartEnterLeaveContext icelc) {
			ValueAxis = null;
			icelc.DeleteLayer(Layer);
			Layer = null;
		}
		/// <summary>
		/// Grid coordinates:
		///		x: "normalized" [0..1] and scaled to the area-width
		///		y: "axis" scale
		/// </summary>
		/// <param name="icrc"></param>
		void IRequireRender.Render(IChartRenderContext icrc) {
			if (ValueAxis == null) return;
			if (double.IsNaN(ValueAxis.Maximum) || double.IsNaN(ValueAxis.Minimum)) return;
			// grid lines
			var tc = new TickCalculator(ValueAxis.Minimum, ValueAxis.Maximum);
			//_trace.Verbose($"grid range:{tc.Range} tintv:{tc.TickInterval}");
			GridGeometry.Children.Clear();
			foreach (var tick in tc.GetTicks()) {
				//_trace.Verbose($"grid vx:{tick}");
				var grid = new LineGeometry() { StartPoint = new Point(0, tick), EndPoint = new Point(1, tick) };
				GridGeometry.Children.Add(grid);
			}
			Dirty = false;
		}
		/// <summary>
		/// Grid-coordinates (x:[0..1], y:axis)
		/// </summary>
		/// <param name="icrc"></param>
		void IRequireTransforms.Transforms(IChartRenderContext icrc) {
			if (ValueAxis == null) return;
			var gmatx = MatrixSupport.TransformFor(icrc.SeriesArea, ValueAxis);
			GridGeometry.Transform = new MatrixTransform() { Matrix = gmatx };
		}
		#endregion
	}
	#endregion
}
