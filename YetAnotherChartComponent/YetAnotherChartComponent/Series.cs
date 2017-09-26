using eScape.Core;
using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region DataSeries
	/// <summary>
	/// Base class of components that represent a data series.
	/// </summary>
	public abstract class DataSeries : ChartComponent {
		#region DPs
		/// <summary>
		/// DataSourceName DP.
		/// </summary>
		public static readonly DependencyProperty DataSourceNameProperty = DependencyProperty.Register(
			"DataSourceName", typeof(string), typeof(DataSeries), new PropertyMetadata(null, new PropertyChangedCallback(DataSeriesPropertyChanged))
		);
		/// <summary>
		/// ValuePath DP.
		/// </summary>
		public static readonly DependencyProperty ValuePathProperty = DependencyProperty.Register(
			"ValuePath", typeof(string), typeof(DataSeries), new PropertyMetadata(null, new PropertyChangedCallback(DataSeriesPropertyChanged))
		);
		/// <summary>
		/// CategoryPath DP.
		/// </summary>
		public static readonly DependencyProperty CategoryPathProperty = DependencyProperty.Register(
			"CategoryPath", typeof(string), typeof(DataSeries), new PropertyMetadata(null, new PropertyChangedCallback(DataSeriesPropertyChanged))
		);
		/// <summary>
		/// CategoryLabelPath DP.
		/// </summary>
		public static readonly DependencyProperty CategoryLabelPathProperty = DependencyProperty.Register(
			"CategoryLabelPath", typeof(string), typeof(DataSeries), new PropertyMetadata(null, new PropertyChangedCallback(DataSeriesPropertyChanged))
		);
		/// <summary>
		/// Generic DP property change handler.
		/// Calls DataSeries.ProcessData().
		/// </summary>
		/// <param name="d"></param>
		/// <param name="dpcea"></param>
		private static void DataSeriesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs dpcea) {
			DataSeries ds = d as DataSeries;
			ds.Dirty = true;
			ds.Refresh();
		}
		#endregion
		#region properties
		/// <summary>
		/// The name of the data source in the DataSources collection.
		/// </summary>
		public String DataSourceName { get { return (String)GetValue(DataSourceNameProperty); } set { SetValue(DataSourceNameProperty, value); } }
		/// <summary>
		/// Binding path to the category axis value.
		/// MAY be NULL, in which case the data-index is used instead.
		/// </summary>
		public String CategoryPath { get { return (String)GetValue(CategoryPathProperty); } set { SetValue(CategoryPathProperty, value); } }
		/// <summary>
		/// Binding path to the category axis label.
		/// If multiple series are presenting the same data source, only one MUST HAVE this property set.
		/// If CategoryMemberPath is NULL, the data-index is used.
		/// MAY be NULL, in which case no labels are used on category axis.
		/// </summary>
		public String CategoryLabelPath { get { return (String)GetValue(CategoryLabelPathProperty); } set { SetValue(CategoryLabelPathProperty, value); } }
		/// <summary>
		/// Binding path to the value axis value.
		/// </summary>
		public String ValuePath { get { return (String)GetValue(ValuePathProperty); } set { SetValue(ValuePathProperty, value); } }
		/// <summary>
		/// Component name of value axis.
		/// Referenced component MUST implement IChartAxis.
		/// </summary>
		public String ValueAxisName { get; set; }
		/// <summary>
		/// Component name of category axis.
		/// Referenced component MUST implement IChartAxis.
		/// </summary>
		public String CategoryAxisName { get; set; }
		/// <summary>
		/// The minimum value seen.
		/// Only valid after ProcessData() is called.
		/// </summary>
		public double Minimum { get; protected set; } = double.NaN;
		/// <summary>
		/// The maximum value seen.
		/// Only valid after ProcessData() is called.
		/// </summary>
		public double Maximum { get; protected set; } = double.NaN;
		/// <summary>
		/// The minimum category (value) seen.
		/// Only valid after ProcessData() is called.
		/// </summary>
		public double CategoryMinimum { get; protected set; } = double.NaN;
		/// <summary>
		/// The maximum category (value) seen.
		/// Only valid after ProcessData() is called.
		/// </summary>
		public double CategoryMaximum { get; protected set; } = double.NaN;
		/// <summary>
		/// Range of the values or NaN if ProcessData() was never called.
		/// </summary>
		public double Range { get { return double.IsNaN(Minimum) || double.IsNaN(Maximum) ? double.NaN : Maximum - Minimum + 1; } }
		/// <summary>
		/// Dereferenced value axis.
		/// </summary>
		protected IChartAxis ValueAxis { get; set; }
		/// <summary>
		/// Dereferenced category axis.
		/// </summary>
		protected IChartAxis CategoryAxis { get; set; }
		#endregion
		#region helpers
		/// <summary>
		/// Provide a readable name for DP update diagnostics.
		/// </summary>
		/// <param name="dp"></param>
		/// <returns></returns>
		protected virtual String DPName(DependencyProperty dp) {
			if (dp == ValuePathProperty) return "ValuePath";
			else if (dp == CategoryPathProperty) return "CategoryPath";
			else if (dp == CategoryLabelPathProperty) return "CategoryLabelPath";
			else if (dp == DataSourceNameProperty) return "DataSourceName";
			return dp.ToString();
		}
		/// <summary>
		/// Resolve axis references.
		/// </summary>
		/// <param name="icrc">The context.</param>
		protected void EnsureAxes(IChartRenderContext icrc) {
			if (ValueAxis == null && !String.IsNullOrEmpty(ValueAxisName)) {
				ValueAxis = icrc.Find(ValueAxisName) as IChartAxis;
			}
			if (CategoryAxis == null && !String.IsNullOrEmpty(CategoryAxisName)) {
				CategoryAxis = icrc.Find(CategoryAxisName) as IChartAxis;
			}
		}
		/// <summary>
		/// Register the series limits with the axes.
		/// </summary>
		protected void ApplyLimitsToAxes() {
			if(ValueAxis != null) {
				ValueAxis.UpdateLimits(Minimum);
				ValueAxis.UpdateLimits(Maximum);
			}
			if(CategoryAxis != null) {
				CategoryAxis.UpdateLimits(CategoryMinimum);
				CategoryAxis.UpdateLimits(CategoryMaximum);
			}
		}
		/// <summary>
		/// Update value and category limits.
		/// </summary>
		/// <param name="vx">category</param>
		/// <param name="vy">value</param>
		protected void UpdateLimits(double vx, double vy) {
			if (double.IsNaN(Minimum) || vy < Minimum) { Minimum = vy; }
			if (double.IsNaN(Maximum) || vy > Maximum) { Maximum = vy; }
			if (double.IsNaN(CategoryMinimum) || vx < CategoryMinimum) { CategoryMinimum = vx; }
			if (double.IsNaN(CategoryMaximum) || vx > CategoryMaximum) { CategoryMaximum = vx; }
		}
		/// <summary>
		/// Reset the value and category limits.
		/// Sets Dirty = true.
		/// </summary>
		protected void ResetLimits() {
			Minimum = double.NaN; Maximum = double.NaN;
			CategoryMinimum = double.NaN; CategoryMaximum = double.NaN;
			Dirty = true;
		}
		#endregion
		#region extensions
		/// <summary>
		/// Update axes if not dirty.
		/// This is no longer called for DataSeries; it's handled by DataSource.Render().
		/// </summary>
		/// <param name="icrc"></param>
		public override void Render(IChartRenderContext icrc) {
			//_trace.Verbose($"render v:{ValueAxis} c:{CategoryAxis} d:{DataSourceName} dirty:{Dirty}");
			if (ValueAxis == null || CategoryAxis == null) return;
			if (!Dirty) {
				ValueAxis.For(Minimum);
				ValueAxis.For(Maximum);
				CategoryAxis.For(CategoryMinimum);
				CategoryAxis.For(CategoryMaximum);
			}
		}
		#endregion
	}
	#endregion
	#region LineSeries
	/// <summary>
	/// Data series that generates a Polyline visual.
	/// </summary>
	public class LineSeries : DataSeries, IDataSourceRenderer {
		static LogTools.Flag _trace = LogTools.Add("LineSeries", LogTools.Level.Error);
		#region properties
		/// <summary>
		/// Series path stroke thickness.
		/// Default value is 1.
		/// </summary>
		public int StrokeThickness { get; set; } = 1;
		/// <summary>
		/// Series path line join.
		/// Default value is Bevel.
		/// </summary>
		public PenLineJoin StrokeLineJoin { get; set; } = PenLineJoin.Bevel;
		/// <summary>
		/// Series Start line cap.
		/// Default value is Flat.
		/// </summary>
		public PenLineCap StrokeStartLineCap { get; set; } = PenLineCap.Flat;
		/// <summary>
		/// Series End line cap.
		/// Default value is Flat.
		/// </summary>
		public PenLineCap StrokeEndLineCap { get; set; } = PenLineCap.Flat;
		/// <summary>
		/// The brush for the series.
		/// </summary>
		public Brush Stroke { get { return (Brush)GetValue(StrokeProperty); } set { SetValue(StrokeProperty, value); } }
		/// <summary>
		/// Offset in Category axis offset in [0..1].
		/// Use with ColumnSeries to get the "points" to align with the column(s) layout in their cells.
		/// </summary>
		public double CategoryAxisOffset { get; set; }
		/// <summary>
		/// The series drawing attributes etc. on the Canvas.
		/// </summary>
		protected Path Segments { get; set; }
		/// <summary>
		/// The series geometry.
		/// </summary>
		protected PathGeometry Geometry { get; set; }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="Stroke"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register("Stroke", typeof(Brush), typeof(LineSeries), new PropertyMetadata(null));
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		public LineSeries() {
			Geometry = new PathGeometry();
			Segments = new Path() {
				Data = Geometry
			};
		}
		#endregion
		#region extensions
		/// <summary>
		/// Initialize after entering VT.
		/// </summary>
		/// <param name="icelc"></param>
		public override void Enter(IChartEnterLeaveContext icelc) {
			EnsureAxes(icelc);
			_trace.Verbose($"enter v:{ValueAxisName}:{ValueAxis} c:{ValueAxisName}:{ValueAxis} d:{DataSourceName}");
			icelc.Add(Segments);
			BindTo(this, "Stroke", Segments, Path.StrokeProperty);
			Segments.StrokeThickness = StrokeThickness;
			Segments.StrokeEndLineCap = StrokeEndLineCap;
			Segments.StrokeLineJoin = StrokeLineJoin;
			Segments.StrokeStartLineCap = StrokeStartLineCap;
		}
		/// <summary>
		/// Undo effects of Enter().
		/// </summary>
		/// <param name="icelc"></param>
		public override void Leave(IChartEnterLeaveContext icelc) {
			_trace.Verbose($"leave");
			ValueAxis = null;
			CategoryAxis = null;
			icelc.Remove(Segments);
		}
		/// <summary>
		/// Adjust transforms for the various components.
		/// Geometry: scaled to actual values in cartesian coordinates as indicated by axes.
		/// </summary>
		/// <param name="icrc"></param>
		public override void Transforms(IChartRenderContext icrc) {
			base.Transforms(icrc);
			if (CategoryAxis == null || ValueAxis == null) return;
			var scalex = icrc.Area.Width / CategoryAxis.Range;
			var scaley = icrc.Area.Height / ValueAxis.Range;
			var offsetx = scalex * CategoryAxisOffset;
			var matx = new Matrix(scalex, 0, 0, -scaley, icrc.Area.Left + offsetx, icrc.Area.Top + ValueAxis.Maximum*scaley);
			_trace.Verbose($"scale {scalex:F3},{scaley:F3} mat:{matx}");
			Geometry.Transform = new MatrixTransform() { Matrix = matx };
		}
		#endregion
		#region IDataSourceRenderer
		class State {
			internal BindingEvaluator bx;
			internal BindingEvaluator by;
			internal BindingEvaluator bl;
			internal PathFigure pf;
		}
		object IDataSourceRenderer.Preamble(IChartRenderContext icrc) {
			if (ValueAxis == null || CategoryAxis == null) return null;
			if (String.IsNullOrEmpty(ValuePath)) return null;
			var by = new BindingEvaluator(ValuePath);
			// TODO report the binding error
			if (by == null) return null;
			ResetLimits();
			return new State() {
				bx = !String.IsNullOrEmpty(CategoryPath) ? new BindingEvaluator(CategoryPath) : null,
				bl = !String.IsNullOrEmpty(CategoryLabelPath) ? new BindingEvaluator(CategoryLabelPath) : null,
				by = by,
				pf = new PathFigure()
			};
		}
		void IDataSourceRenderer.Render(object state, int index, object item) {
			var st = state as State;
			// TODO handle datetime et al values that aren't double
			var valuey = (double)st.by.For(item);
			var valuex = st.bx != null ? (double)st.bx.For(item) : index;
			UpdateLimits(valuex, valuey);
			var mappedy = ValueAxis.For(valuey);
			var mappedx = st.bl == null ? CategoryAxis.For(valuex) : CategoryAxis.For(new Tuple<double, String>(valuex, st.bl.For(item).ToString()));
			_trace.Verbose($"[{index}] {valuey} ({mappedx},{mappedy})");
			if (index == 0) {
				st.pf.StartPoint = new Point(mappedx, mappedy);
			} else {
				st.pf.Segments.Add(new LineSegment() { Point = new Point(mappedx, mappedy) });
			}
		}
		void IDataSourceRenderer.RenderComplete(object state) {
			ApplyLimitsToAxes();
		}
		void IDataSourceRenderer.Postamble(object state) {
			var st = state as State;
			Geometry.Figures.Clear();
			if (st.pf.Segments.Count > 0) {
				Geometry.Figures.Add(st.pf);
			}
			Dirty = false;
		}
		#endregion
	}
	#endregion
	#region MarkerSeries
	/// <summary>
	/// Series that places the given marker at each point.
	/// </summary>
	public class MarkerSeries : DataSeries, IDataSourceRenderer {
		static LogTools.Flag _trace = LogTools.Add("MarkerSeries", LogTools.Level.Error);
		#region properties
		/// <summary>
		/// The brush for the series.
		/// </summary>
		public Brush Stroke { get { return (Brush)GetValue(StrokeProperty); } set { SetValue(StrokeProperty, value); } }
		/// <summary>
		/// The fill brush for the series.
		/// </summary>
		public Brush Fill { get { return (Brush)GetValue(FillProperty); } set { SetValue(FillProperty, value); } }
		/// <summary>
		/// Geometry template for marker.
		/// Currently MUST be EllipseGeometry.
		/// </summary>
		public DataTemplate MarkerTemplate { get { return (DataTemplate)GetValue(MarkerTemplateProperty); } set { SetValue(MarkerTemplateProperty, value); } }
		/// <summary>
		/// Offset in Category axis offset in [0..1].
		/// Use with ColumnSeries to get the "points" to align with the column(s) layout in their cells.
		/// </summary>
		public double CategoryAxisOffset { get; set; }
		/// <summary>
		/// The series drawing attributes etc. on the Canvas.
		/// </summary>
		protected Path Segments { get; set; }
		/// <summary>
		/// The series geometry.
		/// </summary>
		protected GeometryGroup Geometry { get; set; }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="Stroke"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register("Stroke", typeof(Brush), typeof(MarkerSeries), new PropertyMetadata(null));
		/// <summary>
		/// Identifies <see cref="Fill"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty FillProperty = DependencyProperty.Register("Fill", typeof(Brush), typeof(MarkerSeries), new PropertyMetadata(null));
		/// <summary>
		/// Identifies <see cref="MarkerTemplate"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty MarkerTemplateProperty = DependencyProperty.Register("MarkerTemplate", typeof(DataTemplate), typeof(MarkerSeries), new PropertyMetadata(null));
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		public MarkerSeries() {
			Geometry = new GeometryGroup();
			Segments = new Path() {
				Data = Geometry
			};
		}
		#endregion
		#region extensions
		/// <summary>
		/// Initialize after entering VT.
		/// </summary>
		/// <param name="icelc"></param>
		public override void Enter(IChartEnterLeaveContext icelc) {
			EnsureAxes(icelc);
			_trace.Verbose($"enter v:{ValueAxisName}:{ValueAxis} c:{ValueAxisName}:{ValueAxis} d:{DataSourceName}");
			icelc.Add(Segments);
			BindTo(this, "Stroke", Segments, Path.StrokeProperty);
			BindTo(this, "Fill", Segments, Path.FillProperty);
#if false
			Segments.StrokeThickness = StrokeThickness;
			Segments.StrokeEndLineCap = StrokeEndLineCap;
			Segments.StrokeLineJoin = StrokeLineJoin;
			Segments.StrokeStartLineCap = StrokeStartLineCap;
#else
			Segments.StrokeThickness = 1;
#endif
		}
		/// <summary>
		/// Undo effects of Enter().
		/// </summary>
		/// <param name="icelc"></param>
		public override void Leave(IChartEnterLeaveContext icelc) {
			_trace.Verbose($"leave");
			ValueAxis = null;
			CategoryAxis = null;
			icelc.Remove(Segments);
		}
		/// <summary>
		/// Adjust transforms for the various components.
		/// Geometry: scaled to actual values in cartesian coordinates as indicated by axes.
		/// </summary>
		/// <param name="icrc"></param>
		public override void Transforms(IChartRenderContext icrc) {
			base.Transforms(icrc);
			if (CategoryAxis == null || ValueAxis == null) return;
			var scalex = icrc.Area.Width / CategoryAxis.Range;
			var scaley = icrc.Area.Height / ValueAxis.Range;
			var offsetx = scalex * CategoryAxisOffset;
			var matx = new Matrix(scalex, 0, 0, -scaley, icrc.Area.Left + offsetx, icrc.Area.Top + ValueAxis.Maximum * scaley);
			_trace.Verbose($"scale {scalex:F3},{scaley:F3} mat:{matx}");
			Geometry.Transform = new MatrixTransform() { Matrix = matx };
			// TODO must counter-scale (in Y-axis) the markers to preserve aspect ratio
			foreach (var gx in Geometry.Children) {
				TransformMarker(gx, scalex, scaley);
			}
		}
		/// <summary>
		/// Counter-scale the marker's Y-axis to preserve aspect ratio.
		/// </summary>
		/// <param name="mk">The marker.</param>
		/// <param name="scalex">X-axis scale.</param>
		/// <param name="scaley">Y-axis scale.</param>
		protected void TransformMarker(Geometry mk, double scalex, double scaley) {
			if(mk is EllipseGeometry) {
				var eg = mk as EllipseGeometry;
				var matx = new Matrix(1, 0, 0, 1 / scaley, 0, eg.Center.Y);
				mk.Transform = new MatrixTransform() { Matrix = matx };
				var xpx = eg.RadiusX * scalex;
				//var yv = xpx / scaley;
				eg.RadiusY = /*yv*/ xpx;
			}
		}
		#endregion
		#region IDataSourceRenderer
		class State {
			internal BindingEvaluator bx;
			internal BindingEvaluator by;
			internal BindingEvaluator bl;
		}
		/// <summary>
		/// Initialize the new marker coordinates.
		/// </summary>
		/// <param name="mappedx"></param>
		/// <param name="mappedy"></param>
		/// <param name="mk"></param>
		void InitializeMarker(double mappedx, double mappedy, Geometry mk) {
			if (mk is EllipseGeometry) {
				var eg = mk as EllipseGeometry;
				eg.Center = new Point(mappedx, mappedy);
			}
		}
		object IDataSourceRenderer.Preamble(IChartRenderContext icrc) {
			if (ValueAxis == null || CategoryAxis == null) return null;
			if (String.IsNullOrEmpty(ValuePath)) return null;
			var by = new BindingEvaluator(ValuePath);
			// TODO report the binding error
			if (by == null) return null;
			ResetLimits();
			Geometry.Children.Clear();
			return new State() {
				bx = !String.IsNullOrEmpty(CategoryPath) ? new BindingEvaluator(CategoryPath) : null,
				bl = !String.IsNullOrEmpty(CategoryLabelPath) ? new BindingEvaluator(CategoryLabelPath) : null,
				by = by
			};
		}
		void IDataSourceRenderer.Render(object state, int index, object item) {
			var st = state as State;
			// TODO handle datetime et al values that aren't double
			var valuey = (double)st.by.For(item);
			var valuex = st.bx != null ? (double)st.bx.For(item) : index;
			UpdateLimits(valuex, valuey);
			var mappedy = ValueAxis.For(valuey);
			var mappedx = st.bl == null ? CategoryAxis.For(valuex) : CategoryAxis.For(new Tuple<double, String>(valuex, st.bl.For(item).ToString()));
			_trace.Verbose($"[{index}] {valuey} ({mappedx},{mappedy})");
			var mk = MarkerTemplate.LoadContent() as Geometry;
			InitializeMarker(mappedx, mappedy, mk);
			Geometry.Children.Add(mk);
		}
		void IDataSourceRenderer.RenderComplete(object state) {
			ApplyLimitsToAxes();
		}
		void IDataSourceRenderer.Postamble(object state) {
			var st = state as State;
			Dirty = false;
		}
		#endregion
	}
	#endregion
	#region ColumnSeries
	/// <summary>
	/// Data series that generates a series of Rectangles on a single Path.
	/// If there's no CategoryMemberPath defined (i.e. using data index) this component reserves one "extra" cell on the Category Axis, to present the last column(s).
	/// Category axis cells start on the left and extend positive-X (in device units).  Each cell is one unit long.
	/// </summary>
	public class ColumnSeries : DataSeries, IDataSourceRenderer {
		static LogTools.Flag _trace = LogTools.Add("ColumnSeries", LogTools.Level.Error);
		#region properties
		/// <summary>
		/// The fill brush for the series.
		/// </summary>
		public Brush Fill { get { return (Brush)GetValue(FillProperty); } set { SetValue(FillProperty, value); } }
		/// <summary>
		/// The stroke brush for the series.
		/// </summary>
		public Brush Stroke { get { return (Brush)GetValue(StrokeProperty); } set { SetValue(StrokeProperty, value); } }
		/// <summary>
		/// Fractional offset into the "cell" of the category axis.
		/// BarOffset + BarWidth &lt;= 1.0
		/// </summary>
		public double BarOffset { get; set; } = 0.25;
		/// <summary>
		/// Fractional width in the "cell" of the category axis.
		/// BarOffset + BarWidth &lt;= 1.0
		/// </summary>
		public double BarWidth { get; set; } = 0.5;
		/// <summary>
		/// Path for the column bars.
		/// </summary>
		protected Path Segments { get; set; }
		/// <summary>
		/// Geometry for the column bars.
		/// </summary>
		protected PathGeometry Geometry { get; set; }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="Fill"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty FillProperty = DependencyProperty.Register("Fill", typeof(Brush), typeof(ColumnSeries), new PropertyMetadata(null));
		/// <summary>
		/// Identifies <see cref="Stroke"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register("Stroke", typeof(Brush), typeof(ColumnSeries), new PropertyMetadata(null));
		#endregion
		#region ctor
		/// <summary>
		/// Default ctor.
		/// </summary>
		public ColumnSeries() {
			Geometry = new PathGeometry();
			Segments = new Path() {
				StrokeThickness = 1,
				Data = Geometry
		};
		}
		#endregion
		#region extensions
		/// <summary>
		/// Initialize after entering VT.
		/// </summary>
		/// <param name="icelc"></param>
		public override void Enter(IChartEnterLeaveContext icelc) {
			EnsureAxes(icelc);
			_trace.Verbose($"enter v:{ValueAxisName} {ValueAxis} c:{CategoryAxisName} {CategoryAxis} d:{DataSourceName}");
			icelc.Add(Segments);
			BindTo(this, "Stroke", Segments, Path.StrokeProperty);
			BindTo(this, "Fill", Segments, Path.FillProperty);
		}
		/// <summary>
		/// Undo effects of Enter().
		/// </summary>
		/// <param name="icelc"></param>
		public override void Leave(IChartEnterLeaveContext icelc) {
			_trace.Verbose($"leave");
			ValueAxis = null;
			CategoryAxis = null;
			icelc.Remove(Segments);
		}
		/// <summary>
		/// Adjust transforms for the various components.
		/// Geometry: scaled to actual values in cartesian coordinates as indicated by axes.
		/// </summary>
		/// <param name="icrc"></param>
		public override void Transforms(IChartRenderContext icrc) {
			base.Transforms(icrc);
			if (CategoryAxis == null || ValueAxis == null) return;
			var scalex = icrc.Area.Width / CategoryAxis.Range;
			var scaley = icrc.Area.Height / ValueAxis.Range;
			var matx = new Matrix(scalex, 0, 0, -scaley, icrc.Area.Left, icrc.Area.Top + ValueAxis.Maximum * scaley);
			_trace.Verbose($"scale {scalex:F3},{scaley:F3} mat:{matx}");
			Geometry.Transform = new MatrixTransform() { Matrix = matx };
			Segments.Clip = new RectangleGeometry() { Rect = new Rect(0,0, icrc.Area.Width, icrc.Area.Height) };
		}
		#endregion
		#region IDataSourceRenderer
		class State {
			internal BindingEvaluator bx;
			internal BindingEvaluator by;
			internal BindingEvaluator bl;
			internal int ix;
		}
		object IDataSourceRenderer.Preamble(IChartRenderContext icrc) {
			if (ValueAxis == null || CategoryAxis == null) return null;
			if (String.IsNullOrEmpty(ValuePath)) return null;
			var by = new BindingEvaluator(ValuePath);
			// TODO report the binding error
			if (by == null) return null;
			ResetLimits();
			Geometry.Figures.Clear();
			return new State() {
				bx = !String.IsNullOrEmpty(CategoryPath) ? new BindingEvaluator(CategoryPath) : null,
				bl = !String.IsNullOrEmpty(CategoryLabelPath) ? new BindingEvaluator(CategoryLabelPath) : null,
				by = by
			};
		}
		void IDataSourceRenderer.Render(object state, int index, object item) {
			var st = state as State;
			var valuey = (double)st.by.For(item);
			var valuex = st.bx != null ? (double)st.bx.For(item) : index;
			UpdateLimits(valuex, valuey);
			UpdateLimits(valuex, 0);
			var topy = ValueAxis.For(valuey);
			var bottomy = ValueAxis.For(0);
			var leftx = (st.bl == null ? CategoryAxis.For(valuex) : CategoryAxis.For(new Tuple<double, String>(valuex, st.bl.For(item).ToString()))) + BarOffset;
			var rightx = leftx + BarWidth;
			_trace.Verbose($"[{index}] {valuey} ({leftx},{topy}) ({rightx},{bottomy})");
			var pf = PathHelper.Rectangle(leftx, Math.Max(topy, bottomy), rightx, Math.Min(topy, bottomy));
			Geometry.Figures.Add(pf);
			st.ix = index;
		}
		/// <summary>
		/// Have to perform update here and not in Postamble because we are altering axis limits.
		/// </summary>
		/// <param name="state"></param>
		void IDataSourceRenderer.RenderComplete(object state) {
			var st = state as State;
			if (st.bx == null) {
				// needs one extra "cell"
				CategoryAxis.For(st.ix + 1);
				UpdateLimits(st.ix + 1, 0);
			}
			ApplyLimitsToAxes();
		}
		void IDataSourceRenderer.Postamble(object state) {
			Dirty = false;
		}
		#endregion
	}
	#endregion
}
