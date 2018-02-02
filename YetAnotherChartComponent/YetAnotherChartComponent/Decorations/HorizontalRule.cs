using eScape.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region HorizontalRule
	/// <summary>
	/// Represents a horizontal "rule" on the chart, for a value not belonging to any data source value, e.g. a value computed "outside" the series itself (Average).
	/// </summary>
	public class HorizontalRule : ChartComponent, IProvideValueExtents, IProvideSeriesItemValues, IRequireChartTheme, IRequireEnterLeave, IRequireRender, IRequireTransforms {
		static LogTools.Flag _trace = LogTools.Add("HorizontalRule", LogTools.Level.Error);
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
		/// Binding path to the value axis value.
		/// </summary>
		public double Value { get { return (double)GetValue(ValueProperty); } set { SetValue(ValueProperty, value); } }
		/// <summary>
		/// Whether to clip geometry to the data region.
		/// When true, rule will NEVER display outside the data region.
		/// Default value is true.
		/// </summary>
		public bool ClipToDataRegion { get; set; } = true;
		/// <summary>
		/// Whether to expose the value to the value axis.
		/// When true, forces this rule's value to appear on the axis.
		/// Default value is True.
		/// </summary>
		public bool ShowOnAxis { get; set; } = true;
		/// <summary>
		/// Property for IProvideValueExtents.
		/// </summary>
		public double Minimum { get { return Value; } }
		/// <summary>
		/// Property for IProvideValueExtents.
		/// </summary>
		public double Maximum { get { return Value; } }
		/// <summary>
		/// The path to attach geometry et al.
		/// </summary>
		protected Path Path { get; set; }
		/// <summary>
		/// The geometry to use for this component.
		/// </summary>
		protected LineGeometry Rule { get; set; }
		/// <summary>
		/// Dereferenced value axis.
		/// </summary>
		protected IChartAxis ValueAxis { get; set; }
		/// <summary>
		/// The layer for components.
		/// </summary>
		protected IChartLayer Layer { get; set; }
		/// <summary>
		/// Provide a wrapper so labels can generate.
		/// </summary>
		IEnumerable<ISeriesItem> IProvideSeriesItemValues.SeriesItemValues {
			get {
				var sivc = new ItemState<Path>(0, 0, 1, Value, Path, 0);
				return new[] { sivc };
			}
		}
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="PathStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty PathStyleProperty = DependencyProperty.Register(nameof(PathStyle), typeof(Style), typeof(HorizontalRule), new PropertyMetadata(null));
		/// <summary>
		/// Value DP.
		/// </summary>
		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
			nameof(Value), typeof(double), typeof(HorizontalRule), new PropertyMetadata(null, new PropertyChangedCallback(ComponentPropertyChanged))
		);
		/// <summary>
		/// Generic DP property change handler.
		/// Calls DataSeries.ProcessData().
		/// </summary>
		/// <param name="d"></param>
		/// <param name="dpcea"></param>
		private static void ComponentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs dpcea) {
			HorizontalRule hr = d as HorizontalRule;
			if (dpcea.OldValue != dpcea.NewValue) {
				if (hr.ValueAxis == null) return;
				var aus = AxisUpdateState.None;
				if (hr.Value > hr.ValueAxis.Maximum || hr.Value < hr.ValueAxis.Minimum) {
					_trace.Verbose($"{hr.Name} axis-update-required");
					aus = AxisUpdateState.Value;
				}
				hr.Dirty = true;
				hr.Refresh(RefreshRequestType.ValueDirty, aus);
			}
		}
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		public HorizontalRule() {
			Rule = new LineGeometry();
			Path = new Path() {
				Data = Rule
			};
		}
		#endregion
		#region helpers
		void DoBindings(IChartEnterLeaveContext icelc) {
			AssignFromRef(icelc as IChartErrorInfo, NameOrType(), nameof(PathStyle), nameof(Theme.PathHorizontalRule),
				PathStyle == null, Theme != null, Theme.PathHorizontalRule != null,
				() => PathStyle = Theme.PathHorizontalRule
			);
			BindTo(this, nameof(PathStyle), Path, FrameworkElement.StyleProperty);
			ApplyBinding(this, nameof(Visibility), Path, UIElement.VisibilityProperty);
		}
		/// <summary>
		/// Resolve axis references.
		/// </summary>
		/// <param name="iccc">The context.</param>
		protected void EnsureAxes(IChartComponentContext iccc) {
			if (ValueAxis == null && !String.IsNullOrEmpty(ValueAxisName)) {
				ValueAxis = iccc.Find(ValueAxisName) as IChartAxis;
			} else {
				if (iccc is IChartErrorInfo icei) {
					icei.Report(new ChartValidationResult(NameOrType(), $"Value axis '{ValueAxisName}' was not found", new[] { nameof(ValueAxis), nameof(ValueAxisName) }));
				}
			}
		}
		#endregion
		#region extensions
		/// <summary>
		/// Add elements and attach bindings.
		/// </summary>
		/// <param name="icelc">The context.</param>
		void IRequireEnterLeave.Enter(IChartEnterLeaveContext icelc) {
			EnsureAxes(icelc as IChartComponentContext);
			Layer = icelc.CreateLayer(Path);
			_trace.Verbose($"enter v:{ValueAxisName}:{ValueAxis}");
			DoBindings(icelc);
		}
		/// <summary>
		/// Reverse effect of Enter.
		/// </summary>
		/// <param name="icelc">The context.</param>
		void IRequireEnterLeave.Leave(IChartEnterLeaveContext icelc) {
			icelc.DeleteLayer(Layer);
			Layer = null;
		}
		/// <summary>
		/// Rule coordinates:
		///		x: "normalized" [0..1] and scaled to the area-width
		///		y: "axis" scale
		/// </summary>
		/// <param name="icrc">The context.</param>
		void IRequireRender.Render(IChartRenderContext icrc) {
			if (ValueAxis == null) return;
			_trace.Verbose($"{Name} val:{Value}");
			var vx = ValueAxis.For(Value);
			Rule.StartPoint = new Point(0, vx);
			Rule.EndPoint = new Point(1, vx);
			Dirty = false;
		}
		/// <summary>
		/// rule coordinates (x:[0..1], y:axis)
		/// </summary>
		/// <param name="icrc">The context.</param>
		void IRequireTransforms.Transforms(IChartRenderContext icrc) {
			if (ValueAxis == null) return;
			var matx = MatrixSupport.TransformFor(icrc.SeriesArea, ValueAxis);
			_trace.Verbose($"transforms sy:{matx.M22:F3} matx:{matx} sa:{icrc.SeriesArea}");
			if (ClipToDataRegion) {
				Path.Clip = new RectangleGeometry() { Rect = icrc.SeriesArea };
			}
			Rule.Transform = new MatrixTransform() { Matrix = matx };
		}
		#endregion
	}
	#endregion
}
