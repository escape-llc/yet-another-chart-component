using eScape.Core;
using System;
using System.ComponentModel.DataAnnotations;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region HorizontalBand
	/// <summary>
	/// Represents a horizontal "rule" on the chart, for a value not belonging to any data source value, e.g. a value computed "outside" the series itself (Average).
	/// </summary>
	public class HorizontalBand : ChartComponent, IProvideValueExtents, IRequireChartTheme, IRequireEnterLeave, IRequireRender, IRequireTransforms {
		static LogTools.Flag _trace = LogTools.Add("HorizontalBand", LogTools.Level.Error);
		#region properties
		/// <summary>
		/// The style to use for "rules" Path geometry.
		/// </summary>
		public Style PathStyle { get { return (Style)GetValue(PathStyleProperty); } set { SetValue(PathStyleProperty, value); } }
		/// <summary>
		/// The style to use for "band" Path geometry.
		/// If NULL, falls back to <see cref="PathStyle"/>.
		/// </summary>
		public Style BandPathStyle { get { return (Style)GetValue(BandPathStyleProperty); } set { SetValue(BandPathStyleProperty, value); } }
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
		/// Binding path to the maximum value axis value.
		/// </summary>
		public double Value1 { get { return (double)GetValue(Value1Property); } set { SetValue(Value1Property, value); } }
		/// <summary>
		/// Binding path to the minimum value axis value.
		/// </summary>
		public double Value2 { get { return (double)GetValue(Value2Property); } set { SetValue(Value2Property, value); } }
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
		/// Whether to internally do a min/max on the two values.
		/// Default value is True.
		/// </summary>
		public bool DoMinMax { get; set; } = true;
		/// <summary>
		/// Property for IProvideValueExtents.
		/// </summary>
		public double Minimum { get { return Math.Min(Value1, Value2); } }
		/// <summary>
		/// Property for IProvideValueExtents.
		/// </summary>
		public double Maximum { get { return Math.Max(Value1, Value2); } }
		/// <summary>
		/// The path to attach geometry et al.
		/// </summary>
		protected Path Value1Path { get; set; }
		/// <summary>
		/// The geometry to use for this component.
		/// </summary>
		protected LineGeometry Value1Rule { get; set; }
		/// <summary>
		/// The path to attach geometry et al.
		/// </summary>
		protected Path Value2Path { get; set; }
		/// <summary>
		/// The geometry to use for this component.
		/// </summary>
		protected LineGeometry Value2Rule { get; set; }
		/// <summary>
		/// The path to attach geometry et al.
		/// </summary>
		protected Path BandPath { get; set; }
		/// <summary>
		/// The geometry to use for this component.
		/// </summary>
		protected RectangleGeometry Band { get; set; }
		/// <summary>
		/// Dereferenced value axis.
		/// </summary>
		protected IChartAxis ValueAxis { get; set; }
		/// <summary>
		/// The layer for components.
		/// </summary>
		protected IChartLayer Layer { get; set; }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="PathStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty PathStyleProperty = DependencyProperty.Register(
			nameof(PathStyle), typeof(Style), typeof(HorizontalBand), new PropertyMetadata(null)
		);
		/// <summary>
		/// Identifies <see cref="BandPathStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty BandPathStyleProperty = DependencyProperty.Register(
			nameof(BandPathStyle), typeof(Style), typeof(HorizontalBand), new PropertyMetadata(null)
		);
		/// <summary>
		/// <see cref="Value1"/> DP.
		/// </summary>
		public static readonly DependencyProperty Value1Property = DependencyProperty.Register(
			nameof(Value1), typeof(double), typeof(HorizontalBand), new PropertyMetadata(null, new PropertyChangedCallback(ComponentPropertyChanged))
		);
		/// <summary>
		/// <see cref="Value2"/> DP.
		/// </summary>
		public static readonly DependencyProperty Value2Property = DependencyProperty.Register(
			nameof(Value2), typeof(double), typeof(HorizontalBand), new PropertyMetadata(null, new PropertyChangedCallback(ComponentPropertyChanged))
		);
		/// <summary>
		/// Generic DP property change handler.
		/// Calls DataSeries.ProcessData().
		/// </summary>
		/// <param name="d"></param>
		/// <param name="dpcea"></param>
		private static void ComponentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs dpcea) {
			HorizontalBand hr = d as HorizontalBand;
			if (dpcea.OldValue != dpcea.NewValue) {
				if (hr.ValueAxis == null) return;
				var aus = AxisUpdateState.None;
				if (hr.Value1 > hr.ValueAxis.Maximum || hr.Value2 < hr.ValueAxis.Minimum) {
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
		public HorizontalBand() {
			Value1Rule = new LineGeometry();
			Value1Path = new Path() {
				Data = Value1Rule
			};
			Value2Rule = new LineGeometry();
			Value2Path = new Path() {
				Data = Value2Rule
			};
			Band = new RectangleGeometry();
			BandPath = new Path() {
				Data = Band
			};
		}
		#endregion
		#region helpers
		void DoBindings(IChartEnterLeaveContext icelc) {
			AssignFromSource(icelc as IChartErrorInfo, NameOrType(), nameof(PathStyle), nameof(Theme.PathHorizontalRule),
				PathStyle == null && Theme != null,
				Theme.PathHorizontalRule != null,
				() => PathStyle = Theme.PathHorizontalRule
			);
			AssignFromSource(icelc as IChartErrorInfo, NameOrType(), nameof(BandPathStyle), nameof(Theme.PathHorizontalBand),
				BandPathStyle == null && Theme != null,
				Theme.PathHorizontalBand != null,
				() => BandPathStyle = Theme.PathHorizontalBand
			);
			BindTo(this, "PathStyle", Value1Path, Path.StyleProperty);
			var bx = GetBindingExpression(UIElement.VisibilityProperty);
			if (bx != null) {
				Value1Path.SetBinding(UIElement.VisibilityProperty, bx.ParentBinding);
			} else {
				BindTo(this, "Visibility", Value1Path, Path.VisibilityProperty);
			}
			BindTo(this, "PathStyle", Value2Path, Path.StyleProperty);
			bx = GetBindingExpression(UIElement.VisibilityProperty);
			if (bx != null) {
				Value2Path.SetBinding(UIElement.VisibilityProperty, bx.ParentBinding);
			} else {
				BindTo(this, "Visibility", Value2Path, Path.VisibilityProperty);
			}
			BindTo(this, BandPathStyle == null ? "PathStyle" : "BandPathStyle", BandPath, Path.StyleProperty);
			bx = GetBindingExpression(UIElement.VisibilityProperty);
			if (bx != null) {
				BandPath.SetBinding(UIElement.VisibilityProperty, bx.ParentBinding);
			} else {
				BindTo(this, "Visibility", BandPath, Path.VisibilityProperty);
			}
		}
		/// <summary>
		/// Resolve axis references.
		/// </summary>
		/// <param name="icrc">The context.</param>
		protected void EnsureAxes(IChartRenderContext icrc) {
			if (ValueAxis == null && !String.IsNullOrEmpty(ValueAxisName)) {
				ValueAxis = icrc.Find(ValueAxisName) as IChartAxis;
			} else {
				// TODO report the error
				ValidationResult vr = new ValidationResult($"{Name}.{nameof(ValueAxis)}: Axis '{ValueAxisName}' is missing or name is empty", new[] { nameof(ValueAxis), nameof(ValueAxisName) });
			}
		}
		#endregion
		#region extensions
		/// <summary>
		/// Add elements and attach bindings.
		/// </summary>
		/// <param name="icelc">The context.</param>
		void IRequireEnterLeave.Enter(IChartEnterLeaveContext icelc) {
			EnsureAxes(icelc);
			Layer = icelc.CreateLayer(BandPath, Value1Path, Value2Path);
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
			_trace.Verbose($"{Name} max:{Value1} min:{Value2}");
			var vmin = ValueAxis.For(Value2);
			var vmax = ValueAxis.For(Value1);
			var mmin = DoMinMax ? Math.Min(vmin, vmax) : vmin;
			var mmax = DoMinMax ? Math.Max(vmin, vmax) : vmax;
			Value1Rule.StartPoint = new Point(0, mmin);
			Value1Rule.EndPoint = new Point(1, mmin);
			Value2Rule.StartPoint = new Point(0, mmax);
			Value2Rule.EndPoint = new Point(1, mmax);
			Band.Rect = new Rect(Value1Rule.StartPoint, Value2Rule.EndPoint);
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
				Value1Path.Clip = new RectangleGeometry() { Rect = icrc.SeriesArea };
				Value2Path.Clip = new RectangleGeometry() { Rect = icrc.SeriesArea };
				BandPath.Clip = new RectangleGeometry() { Rect = icrc.SeriesArea };
			}
			Value1Rule.Transform = new MatrixTransform() { Matrix = matx };
			Value2Rule.Transform = new MatrixTransform() { Matrix = matx };
			Band.Transform = new MatrixTransform() { Matrix = matx };
		}
		#endregion
	}
	#endregion
}
