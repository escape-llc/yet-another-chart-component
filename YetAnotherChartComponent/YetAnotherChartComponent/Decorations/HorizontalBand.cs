using eScape.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region HorizontalBand
	/// <summary>
	/// Represents a horizontal "rule" on the chart, for a value not belonging to any data source value, e.g. a value computed "outside" the series itself (Average).
	/// </summary>
	public class HorizontalBand : ChartComponent, IProvideValueExtents, IProvideSeriesItemValues, IRequireChartTheme, IRequireEnterLeave, IRequireRender, IRequireTransforms {
		static readonly LogTools.Flag _trace = LogTools.Add("HorizontalBand", LogTools.Level.Error);
		#region properties
		/// <summary>
		/// The style to use for "rules" Path geometry.
		/// </summary>
		public Style PathStyle { get { return (Style)GetValue(PathStyleProperty); } set { SetValue(PathStyleProperty, value); } }
		/// <summary>
		/// The style to use for the Value2 "rule" Path geometry.
		/// If NULL, falls back to <see cref="PathStyle"/>.
		/// </summary>
		public Style Value2PathStyle { get { return (Style)GetValue(Value2PathStyleProperty); } set { SetValue(Value2PathStyleProperty, value); } }
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
		/// <summary>
		/// Provide a wrapper so labels can generate.
		/// </summary>
		IEnumerable<ISeriesItem> IProvideSeriesItemValues.SeriesItemValues {
			get {
				var sis2 = new ISeriesItemValue[2];
				sis2[0] = new ItemState<Path>(0, 0, .5, Value1, Value1Path, 0);
				sis2[1] = new ItemState<Path>(0, 0, .5, Value2, Value2Path, 1);
				var sivc = new ItemStateMultiChannelCore(0, 0, 1, sis2);
				return new[] { sivc };
			}
		}
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="PathStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty PathStyleProperty = DependencyProperty.Register(
			nameof(PathStyle), typeof(Style), typeof(HorizontalBand), new PropertyMetadata(null)
		);
		/// <summary>
		/// Identifies <see cref="Value2PathStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty Value2PathStyleProperty = DependencyProperty.Register(
			nameof(Value2PathStyle), typeof(Style), typeof(HorizontalBand), new PropertyMetadata(null)
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
			Value1Rule = new LineGeometry {
				StartPoint = new Point(0, 0),
				EndPoint = new Point(1, 0)
			};
			Value1Path = new Path() {
				Data = Value1Rule
			};
			Value2Rule = new LineGeometry {
				StartPoint = new Point(0, 0),
				EndPoint = new Point(1, 0)
			};
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
			AssignFromRef(icelc as IChartErrorInfo, NameOrType(), nameof(PathStyle), nameof(Theme.PathHorizontalRule),
				PathStyle == null, Theme != null, Theme.PathHorizontalRule != null,
				() => PathStyle = Theme.PathHorizontalRule
			);
			AssignFromRef(icelc as IChartErrorInfo, NameOrType(), nameof(BandPathStyle), nameof(Theme.PathHorizontalBand),
				BandPathStyle == null, Theme != null, Theme.PathHorizontalBand != null,
				() => BandPathStyle = Theme.PathHorizontalBand
			);
			BindTo(this, nameof(PathStyle), Value1Path, FrameworkElement.StyleProperty);
			ApplyBinding(this, nameof(Visibility), Value1Path, UIElement.VisibilityProperty);
			BindTo(this, Value2PathStyle == null ? nameof(PathStyle) : nameof(Value2PathStyle), Value2Path, FrameworkElement.StyleProperty);
			ApplyBinding(this, nameof(Visibility), Value2Path, UIElement.VisibilityProperty);
			BindTo(this, BandPathStyle == null ? nameof(PathStyle) : nameof(BandPathStyle), BandPath, FrameworkElement.StyleProperty);
			ApplyBinding(this, nameof(Visibility), BandPath, UIElement.VisibilityProperty);
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
		#region IRequireEnterLeave
		/// <summary>
		/// Add elements and attach bindings.
		/// </summary>
		/// <param name="icelc">The context.</param>
		void IRequireEnterLeave.Enter(IChartEnterLeaveContext icelc) {
			EnsureAxes(icelc as IChartComponentContext);
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
		#endregion
		#region IRequireRender
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
#if false
			Value1Rule.StartPoint = new Point(0, mmin);
			Value1Rule.EndPoint = new Point(1, mmin);
			Value2Rule.StartPoint = new Point(0, mmax);
			Value2Rule.EndPoint = new Point(1, mmax);
			Band.Rect = new Rect(Value1Rule.StartPoint, Value2Rule.EndPoint);
#else
			Band.Rect = new Rect(new Point(0, 0), new Point(1, mmax - mmin));
#endif
			Dirty = false;
		}
		#endregion
		#region IRequireTransforms
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
#if true
			var vmin = ValueAxis.For(Value2);
			var vmax = ValueAxis.For(Value1);
			var mmin = DoMinMax ? Math.Min(vmin, vmax) : vmin;
			var mmax = DoMinMax ? Math.Max(vmin, vmax) : vmax;
			var offset1 = matx.Transform(new Point(0, vmin));
			Value1Path.SetValue(Canvas.TopProperty, offset1.Y);
			var offset2 = matx.Transform(new Point(0, vmax));
			Value2Path.SetValue(Canvas.TopProperty, offset2.Y);
			var matx2 = matx;
			matx2.OffsetY = 0;
			Value1Rule.Transform = new MatrixTransform() { Matrix = matx2 };
			Value2Rule.Transform = new MatrixTransform() { Matrix = matx2 };
			BandPath.SetValue(Canvas.TopProperty, offset1.Y);
			Band.Transform = new MatrixTransform() { Matrix = matx2 };
#else
			Value1Rule.Transform = new MatrixTransform() { Matrix = matx };
			Value2Rule.Transform = new MatrixTransform() { Matrix = matx };
			Band.Transform = new MatrixTransform() { Matrix = matx };
#endif
		}
		#endregion
	}
	#endregion
}
