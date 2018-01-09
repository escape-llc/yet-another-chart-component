using eScape.Core;
using System;
using Windows.Foundation;
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
			#if false
			var bx = GetBindingExpression(GridVisibilityProperty);
			if (bx != null) {
				Grid.SetBinding(UIElement.VisibilityProperty, bx.ParentBinding);
			} else {
				BindTo(this, "GridVisibility", Grid, Path.VisibilityProperty);
			}
			#endif
		}
		#endregion
		#region extensions
		/// <summary>
		/// Component is entering the chart.
		/// </summary>
		/// <param name="icelc">Context.</param>
		void IRequireEnterLeave.Enter(IChartEnterLeaveContext icelc) {
			icelc.Add(Path);
			DoBindings(icelc);
		}
		/// <summary>
		/// Component is leaving the chart.
		/// </summary>
		/// <param name="icelc">Context.</param>
		void IRequireEnterLeave.Leave(IChartEnterLeaveContext icelc) {
			icelc.Remove(Path);
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
			var matx = new Matrix(icrc.SeriesArea.Width, 0, 0, icrc.SeriesArea.Height, icrc.SeriesArea.Left, icrc.SeriesArea.Top);
			Rectangle.Transform = new MatrixTransform() { Matrix = matx };
		}
		#endregion
	}
	#endregion
	#region HorizontalRule
	/// <summary>
	/// Represents a horizontal "rule" on the chart, for a value not belonging to any data source value, e.g. a value computed "outside" the series itself (Average).
	/// </summary>
	public class HorizontalRule : ChartComponent, IProvideValueExtents, IRequireEnterLeave, IRequireRender, IRequireTransforms/*, IRequireAfterRenderComplete*/ {
		static LogTools.Flag _trace = LogTools.Add("HorizontalRule", LogTools.Level.Error);
		#region properties
		/// <summary>
		/// The style to use for Path geometry.
		/// </summary>
		public Style PathStyle { get { return (Style)GetValue(PathStyleProperty); } set { SetValue(PathStyleProperty, value); } }
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
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="PathStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty PathStyleProperty = DependencyProperty.Register("PathStyle", typeof(Style), typeof(HorizontalRule), new PropertyMetadata(null));
		/// <summary>
		/// Value DP.
		/// </summary>
		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
			"Value", typeof(double), typeof(HorizontalRule), new PropertyMetadata(null, new PropertyChangedCallback(ComponentPropertyChanged))
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
				if(hr.Value > hr.ValueAxis.Maximum || hr.Value < hr.ValueAxis.Minimum) {
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
			BindTo(this, "PathStyle", Path, Path.StyleProperty);
			var bx = GetBindingExpression(UIElement.VisibilityProperty);
			if (bx != null) {
				Path.SetBinding(UIElement.VisibilityProperty, bx.ParentBinding);
			} else {
				BindTo(this, "Visibility", Path, Path.VisibilityProperty);
			}
		}
		/// <summary>
		/// Resolve axis references.
		/// </summary>
		/// <param name="icrc">The context.</param>
		protected void EnsureAxes(IChartRenderContext icrc) {
			if (ValueAxis == null && !String.IsNullOrEmpty(ValueAxisName)) {
				ValueAxis = icrc.Find(ValueAxisName) as IChartAxis;
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
			_trace.Verbose($"enter v:{ValueAxisName}:{ValueAxis}");
			icelc.Add(Path);
			DoBindings(icelc);
		}
		/// <summary>
		/// Reverse effect of Enter.
		/// </summary>
		/// <param name="icelc">The context.</param>
		void IRequireEnterLeave.Leave(IChartEnterLeaveContext icelc) {
			icelc.Remove(Path);
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
		/// Propagate axis update for the value.
		/// </summary>
		#if false
		void IRequireAfterRenderComplete.RenderComplete() {
			if (ValueAxis == null) return;
			if (ShowOnAxis) {
				ValueAxis.UpdateLimits(Value);
			}
		}
		#endif
		/// <summary>
		/// rule coordinates (x:[0..1], y:axis)
		/// </summary>
		/// <param name="icrc">The context.</param>
		void IRequireTransforms.Transforms(IChartRenderContext icrc) {
			if (ValueAxis == null) return;
			var scaley = icrc.SeriesArea.Height / ValueAxis.Range;
			var matx = new Matrix(icrc.SeriesArea.Width, 0, 0, -scaley, icrc.SeriesArea.Left, icrc.SeriesArea.Top + ValueAxis.Maximum * scaley);
			_trace.Verbose($"transforms sy:{scaley:F3} matx:{matx} sa:{icrc.SeriesArea}");
			if (ClipToDataRegion) {
				Path.Clip = new RectangleGeometry() { Rect = icrc.SeriesArea };
			}
			Rule.Transform = new MatrixTransform() { Matrix = matx };
		}
		#endregion
	}
	#endregion
	#region HorizontalBand
	/// <summary>
	/// Represents a horizontal "rule" on the chart, for a value not belonging to any data source value, e.g. a value computed "outside" the series itself (Average).
	/// </summary>
	public class HorizontalBand : ChartComponent, IProvideValueExtents, IRequireEnterLeave, IRequireRender, IRequireTransforms/*, IRequireAfterRenderComplete*/ {
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
		/// Component name of value axis.
		/// Referenced component MUST implement IChartAxis.
		/// </summary>
		public String ValueAxisName { get; set; }
		/// <summary>
		/// Binding path to the maximum value axis value.
		/// </summary>
		public double ValueMaximum { get { return (double)GetValue(ValueMaximumProperty); } set { SetValue(ValueMaximumProperty, value); } }
		/// <summary>
		/// Binding path to the minimum value axis value.
		/// </summary>
		public double ValueMinimum { get { return (double)GetValue(ValueMinimumProperty); } set { SetValue(ValueMinimumProperty, value); } }
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
		public double Minimum { get { return ValueMinimum; } }
		/// <summary>
		/// Property for IProvideValueExtents.
		/// </summary>
		public double Maximum { get { return ValueMaximum; } }
		/// <summary>
		/// The path to attach geometry et al.
		/// </summary>
		protected Path MinimumPath { get; set; }
		/// <summary>
		/// The geometry to use for this component.
		/// </summary>
		protected LineGeometry MinimumRule { get; set; }
		/// <summary>
		/// The path to attach geometry et al.
		/// </summary>
		protected Path MaximumPath { get; set; }
		/// <summary>
		/// The geometry to use for this component.
		/// </summary>
		protected LineGeometry MaximumRule { get; set; }
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
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="PathStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty PathStyleProperty = DependencyProperty.Register(
			"PathStyle", typeof(Style), typeof(HorizontalBand), new PropertyMetadata(null)
		);
		/// <summary>
		/// Identifies <see cref="BandPathStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty BandPathStyleProperty = DependencyProperty.Register(
			"BandPathStyle", typeof(Style), typeof(HorizontalBand), new PropertyMetadata(null)
		);
		/// <summary>
		/// Value DP.
		/// </summary>
		public static readonly DependencyProperty ValueMaximumProperty = DependencyProperty.Register(
			"ValueMaximum", typeof(double), typeof(HorizontalBand), new PropertyMetadata(null, new PropertyChangedCallback(ComponentPropertyChanged))
		);
		public static readonly DependencyProperty ValueMinimumProperty = DependencyProperty.Register(
			"ValueMinimum", typeof(double), typeof(HorizontalBand), new PropertyMetadata(null, new PropertyChangedCallback(ComponentPropertyChanged))
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
				if (hr.ValueMaximum > hr.ValueAxis.Maximum || hr.ValueMinimum < hr.ValueAxis.Minimum) {
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
			MinimumRule = new LineGeometry();
			MinimumPath = new Path() {
				Data = MinimumRule
			};
			MaximumRule = new LineGeometry();
			MaximumPath = new Path() {
				Data = MaximumRule
			};
			Band = new RectangleGeometry();
			BandPath = new Path() {
				Data = Band
			};
		}
		#endregion
		#region helpers
		void DoBindings(IChartEnterLeaveContext icelc) {
			BindTo(this, "PathStyle", MinimumPath, Path.StyleProperty);
			var bx = GetBindingExpression(UIElement.VisibilityProperty);
			if (bx != null) {
				MinimumPath.SetBinding(UIElement.VisibilityProperty, bx.ParentBinding);
			} else {
				BindTo(this, "Visibility", MinimumPath, Path.VisibilityProperty);
			}
			BindTo(this, "PathStyle", MaximumPath, Path.StyleProperty);
			bx = GetBindingExpression(UIElement.VisibilityProperty);
			if (bx != null) {
				MaximumPath.SetBinding(UIElement.VisibilityProperty, bx.ParentBinding);
			} else {
				BindTo(this, "Visibility", MaximumPath, Path.VisibilityProperty);
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
			_trace.Verbose($"enter v:{ValueAxisName}:{ValueAxis}");
			icelc.Add(BandPath);
			icelc.Add(MinimumPath);
			icelc.Add(MaximumPath);
			DoBindings(icelc);
		}
		/// <summary>
		/// Reverse effect of Enter.
		/// </summary>
		/// <param name="icelc">The context.</param>
		void IRequireEnterLeave.Leave(IChartEnterLeaveContext icelc) {
			icelc.Remove(MaximumPath);
			icelc.Remove(MinimumPath);
			icelc.Remove(BandPath);
		}
		/// <summary>
		/// Rule coordinates:
		///		x: "normalized" [0..1] and scaled to the area-width
		///		y: "axis" scale
		/// </summary>
		/// <param name="icrc">The context.</param>
		void IRequireRender.Render(IChartRenderContext icrc) {
			if (ValueAxis == null) return;
			_trace.Verbose($"{Name} max:{ValueMaximum} min:{ValueMinimum}");
			var vmin = ValueAxis.For(ValueMinimum);
			var vmax = ValueAxis.For(ValueMaximum);
			var mmin = DoMinMax ? Math.Min(vmin, vmax) : vmin;
			var mmax = DoMinMax ? Math.Max(vmin, vmax) : vmax;
			MinimumRule.StartPoint = new Point(0, mmin);
			MinimumRule.EndPoint = new Point(1, mmin);
			MaximumRule.StartPoint = new Point(0, mmax);
			MaximumRule.EndPoint = new Point(1, mmax);
			Band.Rect = new Rect(MinimumRule.StartPoint, MaximumRule.EndPoint);
			Dirty = false;
		}
		/// <summary>
		/// rule coordinates (x:[0..1], y:axis)
		/// </summary>
		/// <param name="icrc">The context.</param>
		void IRequireTransforms.Transforms(IChartRenderContext icrc) {
			if (ValueAxis == null) return;
			var scaley = icrc.SeriesArea.Height / ValueAxis.Range;
			var matx = new Matrix(icrc.SeriesArea.Width, 0, 0, -scaley, icrc.SeriesArea.Left, icrc.SeriesArea.Top + ValueAxis.Maximum * scaley);
			_trace.Verbose($"transforms sy:{scaley:F3} matx:{matx} sa:{icrc.SeriesArea}");
			if (ClipToDataRegion) {
				MinimumPath.Clip = new RectangleGeometry() { Rect = icrc.SeriesArea };
				MaximumPath.Clip = new RectangleGeometry() { Rect = icrc.SeriesArea };
				BandPath.Clip = new RectangleGeometry() { Rect = icrc.SeriesArea };
			}
			MinimumRule.Transform = new MatrixTransform() { Matrix = matx };
			MaximumRule.Transform = new MatrixTransform() { Matrix = matx };
			Band.Transform = new MatrixTransform() { Matrix = matx };
		}
		#endregion
	}
	#endregion
	#region ValueAxisGrid
	/// <summary>
	/// Grid lines for the value axis.
	/// </summary>
	public class ValueAxisGrid : ChartComponent, IRequireEnterLeave, IRequireRender, IRequireTransforms {
		#region properties
		/// <summary>
		/// The style to use for Path geometry.
		/// </summary>
		public Style PathStyle { get { return (Style)GetValue(PathStyleProperty); } set { SetValue(PathStyleProperty, value); } }
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
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="PathStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty PathStyleProperty = DependencyProperty.Register("PathStyle", typeof(Style), typeof(ValueAxisGrid), new PropertyMetadata(null));
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
		/// <param name="icrc"></param>
		void EnsureAxes(IChartRenderContext icrc) {
			if (ValueAxis == null && !String.IsNullOrEmpty(ValueAxisName)) {
				ValueAxis = icrc.Find(ValueAxisName) as IChartAxis;
			}
		}
		/// <summary>
		/// Apply bindings to internal elements.
		/// </summary>
		/// <param name="icelc"></param>
		void DoBindings(IChartEnterLeaveContext icelc) {
			BindTo(this, "PathStyle", Grid, Path.StyleProperty);
			var bx = GetBindingExpression(UIElement.VisibilityProperty);
			if (bx != null) {
				Grid.SetBinding(UIElement.VisibilityProperty, bx.ParentBinding);
			} else {
				BindTo(this, "Visibility", Grid, Path.VisibilityProperty);
			}
		}
		#endregion
		#region extensions
		void IRequireEnterLeave.Enter(IChartEnterLeaveContext icelc) {
			EnsureAxes(icelc);
			icelc.Add(Grid);
			DoBindings(icelc);
		}
		void IRequireEnterLeave.Leave(IChartEnterLeaveContext icelc) {
			ValueAxis = null;
			icelc.Remove(Grid);
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
			var gscaley = icrc.SeriesArea.Height / ValueAxis.Range;
			var gmatx = new Matrix(icrc.SeriesArea.Width, 0, 0, -gscaley, icrc.SeriesArea.Left, icrc.SeriesArea.Top + ValueAxis.Maximum * gscaley);
			GridGeometry.Transform = new MatrixTransform() { Matrix = gmatx };
		}
		#endregion
	}
	#endregion
}
