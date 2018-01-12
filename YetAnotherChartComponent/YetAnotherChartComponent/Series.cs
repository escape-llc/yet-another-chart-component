#define EXPERIMENTAL_MARKER
using eScape.Core;
using eScapeLLC.UWP.Charts;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region DataSeries
	/// <summary>
	/// Base class of components that represent a data series.
	/// </summary>
	public abstract class DataSeries : ChartComponent, IProvideValueExtents, IProvideCategoryExtents {
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
			ds.Refresh(RefreshRequestType.ValueDirty, AxisUpdateState.Unknown);
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
		/// Whether to clip geometry to the data region.
		/// Default value is true.
		/// </summary>
		public bool ClipToDataRegion { get; set; } = true;
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
	}
	#endregion
	#region ItemState
	/// <summary>
	/// Basic item state.
	/// This is used when there are multiple paths generated, so they can be re-adjusted in Transforms et al.
	/// </summary>
	public class ItemState {
		/// <summary>
		/// The generated path.
		/// </summary>
		public Path Path { get; set; }
		/// <summary>
		/// The x value.
		/// </summary>
		public double XValue { get; set; }
		/// <summary>
		/// The y value.
		/// </summary>
		public double YValue { get; set; }
	}
	/// <summary>
	/// Item state with transformation matrix.
	/// </summary>
	public class ItemState_Matrix : ItemState {
		/// <summary>
		/// Alternate matrix for the M matrix
		/// </summary>
		public Matrix World { get; set; }
	}
	/// <summary>
	/// Item state with matrix and geometry.
	/// </summary>
	/// <typeparam name="G">Type of geometry.</typeparam>
	public class ItemState_MatrixAndGeometry<G> : ItemState_Matrix where G: Geometry {
		/// <summary>
		/// The geometry.
		/// If you can use Path.Data to reference geometry, choose <see cref="ItemState_Matrix"/> or <see cref="ItemState"/> instead.
		/// </summary>
		public G Geometry { get; set; }
	}
	#endregion
	#region LineSeries
	/// <summary>
	/// Data series that generates a Polyline visual.
	/// </summary>
	public class LineSeries : DataSeries, IDataSourceRenderer, IProvideLegend, IRequireEnterLeave, IRequireTransforms {
		static LogTools.Flag _trace = LogTools.Add("LineSeries", LogTools.Level.Error);
		#region properties
		/// <summary>
		/// The title for the series.
		/// </summary>
		public String Title { get { return (String)GetValue(TitleProperty); } set { SetValue(TitleProperty, value); } }
		/// <summary>
		/// The style to use for Path geometry.
		/// </summary>
		public Style PathStyle { get { return (Style)GetValue(PathStyleProperty); } set { SetValue(PathStyleProperty, value); } }
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
		protected IChartLayer Layer { get; set; }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="PathStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty PathStyleProperty = DependencyProperty.Register("PathStyle", typeof(Style), typeof(LineSeries), new PropertyMetadata(null));
		/// <summary>
		/// Identifies <see cref="Title"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(String), typeof(LineSeries), new PropertyMetadata("Title"));
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
		public void Enter(IChartEnterLeaveContext icelc) {
			EnsureAxes(icelc);
			Layer = icelc.CreateLayer(Segments);
			_trace.Verbose($"enter v:{ValueAxisName}:{ValueAxis} c:{CategoryAxisName}:{CategoryAxis} d:{DataSourceName}");
			BindTo(this, "PathStyle", Segments, Path.StyleProperty);
		}
		/// <summary>
		/// Undo effects of Enter().
		/// </summary>
		/// <param name="icelc"></param>
		public void Leave(IChartEnterLeaveContext icelc) {
			_trace.Verbose($"leave");
			ValueAxis = null;
			CategoryAxis = null;
			icelc.DeleteLayer(Layer);
			Layer = null;
		}
		/// <summary>
		/// Adjust transforms for the various components.
		/// Geometry: scaled to actual values in cartesian coordinates as indicated by axes.
		/// </summary>
		/// <param name="icrc"></param>
		public void Transforms(IChartRenderContext icrc) {
			if (CategoryAxis == null || ValueAxis == null) return;
			var matx = MatrixSupport.TransformFor(icrc.Area, CategoryAxis, ValueAxis);
			_trace.Verbose($"{Name} mat:{matx} clip:{icrc.SeriesArea}");
			Geometry.Transform = new MatrixTransform() { Matrix = matx };
			if (ClipToDataRegion) {
				Segments.Clip = new RectangleGeometry() { Rect = icrc.SeriesArea };
			}
		}
		#endregion
		#region IProvideLegend
		private Legend _legend;
		Legend IProvideLegend.Legend {
			get { if (_legend == null) _legend = Legend(); return _legend; }
		}
		Legend Legend() {
			return new Legend() { Title = Title, Fill = Segments.Stroke, Stroke = Segments.Stroke };
		}
		#endregion
		#region IDataSourceRenderer
		class State {
			internal BindingEvaluator bx;
			internal BindingEvaluator by;
			internal BindingEvaluator bl;
			internal PathFigure pf;
			internal int ix;
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
			valuex += CategoryAxisOffset;
			UpdateLimits(valuex, valuey);
			var mappedy = ValueAxis.For(valuey);
			var mappedx = st.bl == null ? CategoryAxis.For(valuex) : CategoryAxis.For(new Tuple<double, String>(valuex, st.bl.For(item).ToString()));
			_trace.Verbose($"{Name}[{index}] v:({valuex},{valuey}) m:({mappedx},{mappedy})");
			if (index == 0) {
				st.pf.StartPoint = new Point(mappedx, mappedy);
			} else {
				st.pf.Segments.Add(new LineSegment() { Point = new Point(mappedx, mappedy) });
			}
			st.ix = index;
		}
		void IDataSourceRenderer.RenderComplete(object state) {
			var st = state as State;
			if (st.bx == null) {
				// needs one extra "cell"
				UpdateLimits(st.ix + 1, 0);
			}
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
	public class MarkerSeries : DataSeries, IDataSourceRenderer, IProvideLegend, IRequireEnterLeave, IRequireTransforms {
		static LogTools.Flag _trace = LogTools.Add("MarkerSeries", LogTools.Level.Error);
		#region properties
		/// <summary>
		/// The title for the series.
		/// </summary>
		public String Title { get { return (String)GetValue(TitleProperty); } set { SetValue(TitleProperty, value); } }
		/// <summary>
		/// The style to use for Path geometry.
		/// </summary>
		public Style PathStyle { get { return (Style)GetValue(PathStyleProperty); } set { SetValue(PathStyleProperty, value); } }
		/// <summary>
		/// Geometry template for marker.
		/// Currently MUST be EllipseGeometry.
		/// </summary>
		public DataTemplate MarkerTemplate { get { return (DataTemplate)GetValue(MarkerTemplateProperty); } set { SetValue(MarkerTemplateProperty, value); } }
		/// <summary>
		/// Marker Offset in Category axis units [0..1].
		/// This is normalized to the Category axis unit.
		/// </summary>
		public double MarkerOffset { get; set; }
		/// <summary>
		/// Marker Origin is where the "center" of the marker geometry is (in NDC).
		/// Default value is (.5,.5)
		/// </summary>
		public Point MarkerOrigin { get; set; } = new Point(.5, .5);
		/// <summary>
		/// Marker Width/Height in Category axis units [0..1].
		/// Marker coordinates form a square in NDC.
		/// </summary>
		public double MarkerWidth { get; set; }
		/// <summary>
		/// The layer we're drawing into.
		/// </summary>
		protected IChartLayer Layer { get; set; }
		/// <summary>
		/// Data needed for current markers
		/// </summary>
		protected List<MarkerItemState> MarkerState { get; set; }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="PathStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty PathStyleProperty = DependencyProperty.Register("PathStyle", typeof(Style), typeof(MarkerSeries), new PropertyMetadata(null));
		/// <summary>
		/// Identifies <see cref="Title"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(String), typeof(MarkerSeries), new PropertyMetadata("Title"));
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
			MarkerState = new List<MarkerItemState>();
		}
		#endregion
		#region extensions
		protected class MarkerItemState : ItemState_Matrix { }
		/// <summary>
		/// Initialize after entering VT.
		/// </summary>
		/// <param name="icelc"></param>
		public void Enter(IChartEnterLeaveContext icelc) {
			EnsureAxes(icelc);
			Layer = icelc.CreateLayer();
			_trace.Verbose($"enter v:{ValueAxisName}:{ValueAxis} c:{CategoryAxisName}:{CategoryAxis} d:{DataSourceName}");
		}
		/// <summary>
		/// Undo effects of Enter().
		/// </summary>
		/// <param name="icelc"></param>
		public void Leave(IChartEnterLeaveContext icelc) {
			_trace.Verbose($"leave");
			ValueAxis = null;
			CategoryAxis = null;
			icelc.DeleteLayer(Layer);
			Layer = null;
		}
		/// <summary>
		/// Adjust transforms for the various components.
		/// Geometry: scaled to actual values in cartesian coordinates as indicated by axes.
		/// </summary>
		/// <param name="icrc"></param>
		public void Transforms(IChartRenderContext icrc) {
			if (CategoryAxis == null || ValueAxis == null) return;
			// put the P matrix on everything
			var proj = MatrixSupport.ProjectionFor(icrc.Area);
			var world = MatrixSupport.ModelFor(CategoryAxis, ValueAxis);
			// get the local marker matrix
			var marker = MatrixSupport.LocalFor(world, MarkerWidth, icrc.Area, -MarkerOrigin.X, -MarkerOrigin.Y);
			foreach(var state in MarkerState) {
				if(state.World == default(Matrix)) {
				// TODO this can go in axis finalization
					state.World = MatrixSupport.Translate(world, state.XValue, state.YValue);
				}
				// assemble Mk * M * P transform for this path
				var model = MatrixSupport.Multiply(state.World, marker);
				var matx = MatrixSupport.Multiply(proj, model);
				state.Path.Data.Transform = new MatrixTransform() { Matrix = matx };
				if (ClipToDataRegion) {
					state.Path.Clip = new RectangleGeometry() { Rect = icrc.SeriesArea };
				}
			}
			_trace.Verbose($"{Name} mat:{world} clip:{icrc.SeriesArea}");
		}
		#endregion
		#region IProvideLegend
		private Legend _legend;
		Legend IProvideLegend.Legend {
			get { if (_legend == null) _legend = Legend(); return _legend; }
		}
		Legend Legend() {
			return new Legend() { Title = Title, Fill = PathStyle.Find<Brush>(Path.FillProperty), Stroke = PathStyle.Find<Brush>(Path.StrokeProperty) };
		}
		#endregion
		#region IDataSourceRenderer
		class State {
			internal BindingEvaluator bx;
			internal BindingEvaluator by;
			internal BindingEvaluator bl;
			internal List<MarkerItemState> ms;
			internal Recycler<Path> recycler;
			internal IEnumerator<Path> paths;
			internal Path NextPath() {
				if (paths.MoveNext()) return paths.Current;
				else return null;
			}
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
		/// <summary>
		/// Path factory for recycler.
		/// </summary>
		/// <returns></returns>
		Path CreatePath() {
			var path = new Path();
			BindTo(this, "PathStyle", path, Path.StyleProperty);
			return path;
		}
		object IDataSourceRenderer.Preamble(IChartRenderContext icrc) {
			if (ValueAxis == null || CategoryAxis == null) return null;
			if (String.IsNullOrEmpty(ValuePath)) return null;
			var by = new BindingEvaluator(ValuePath);
			// TODO report the binding error
			if (by == null) return null;
			ResetLimits();
			var paths = MarkerState.Select(ms => ms.Path);
			var recycler = new Recycler<Path>(paths, CreatePath);
			return new State() {
				bx = !String.IsNullOrEmpty(CategoryPath) ? new BindingEvaluator(CategoryPath) : null,
				bl = !String.IsNullOrEmpty(CategoryLabelPath) ? new BindingEvaluator(CategoryLabelPath) : null,
				by = by,
				ms = new List<MarkerItemState>(),
				recycler = recycler,
				paths = recycler.Items().GetEnumerator()
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
			mappedx += MarkerOffset;
			_trace.Verbose($"[{index}] {valuey} ({mappedx},{mappedy})");
			var mk = MarkerTemplate.LoadContent() as Geometry;
			// no path yet
			var path = st.NextPath();
			if(path == null) return;
			path.Data = mk;
			st.ms.Add(new MarkerItemState() { XValue = mappedx, YValue = mappedy, Path = path });
		}
		void IDataSourceRenderer.RenderComplete(object state) {
		}
		void IDataSourceRenderer.Postamble(object state) {
			var st = state as State;
			MarkerState = st.ms;
			#if EXPERIMENTAL_MARKER
			Layer.Remove(st.recycler.Unused);
			Layer.Add(st.recycler.Created);
			#endif
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
	public class ColumnSeries : DataSeries, IDataSourceRenderer, IProvideLegend, IRequireEnterLeave, IRequireTransforms {
		static LogTools.Flag _trace = LogTools.Add("ColumnSeries", LogTools.Level.Error);
		static LogTools.Flag _traceg = LogTools.Add("ColumnSeriesPaths", LogTools.Level.Off);
		#region properties
		/// <summary>
		/// The title for the series.
		/// </summary>
		public String Title { get { return (String)GetValue(TitleProperty); } set { SetValue(TitleProperty, value); } }
		/// <summary>
		/// The style to use for Path geometry.
		/// </summary>
		public Style PathStyle { get { return (Style)GetValue(PathStyleProperty); } set { SetValue(PathStyleProperty, value); } }
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
		/// Whether to display debug paths.
		/// Should only be on for ONE series for best results.
		/// </summary>
		public bool EnableDebugPaths { get; set; }
		/// <summary>
		/// Path for the column bars.
		/// </summary>
		protected Path Segments { get; set; }
		/// <summary>
		/// Geometry for the column bars.
		/// </summary>
		protected PathGeometry Geometry { get; set; }
		/// <summary>
		/// Geometry for debug: clip region.
		/// </summary>
		protected GeometryGroup DebugClip { get; set; }
		/// <summary>
		/// Path for the debug graphics.
		/// </summary>
		protected Path DebugSegments { get; set; }
		protected IChartLayer Layer { get; set; }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="PathStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty PathStyleProperty = DependencyProperty.Register("PathStyle", typeof(Style), typeof(ColumnSeries), new PropertyMetadata(null));
		/// <summary>
		/// Identifies <see cref="Title"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(String), typeof(ColumnSeries), new PropertyMetadata("Title"));
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
		public void Enter(IChartEnterLeaveContext icelc) {
			EnsureAxes(icelc);
			Layer = icelc.CreateLayer(Segments);
			_trace.Verbose($"{Name} enter v:{ValueAxisName} {ValueAxis} c:{CategoryAxisName} {CategoryAxis} d:{DataSourceName}");
			if (EnableDebugPaths) {
				_traceg.Verbose(() => {
					DebugClip = new GeometryGroup();
					DebugSegments = new Path() {
						StrokeThickness = 1,
						Fill = new SolidColorBrush(Color.FromArgb(32, Colors.LimeGreen.R, Colors.LimeGreen.G, Colors.LimeGreen.B)),
						Stroke = new SolidColorBrush(Colors.White),
						Data = DebugClip
					};
					return "Created Debug path";
				});
			}
			if (DebugSegments != null) {
				Layer.Add(DebugSegments);
			}
			BindTo(this, "PathStyle", Segments, Path.StyleProperty);
		}
		/// <summary>
		/// Undo effects of Enter().
		/// </summary>
		/// <param name="icelc"></param>
		public void Leave(IChartEnterLeaveContext icelc) {
			_trace.Verbose($"{Name} leave");
			ValueAxis = null;
			CategoryAxis = null;
			icelc.DeleteLayer(Layer);
			Layer = null;
		}
		/// <summary>
		/// Adjust transforms for the various components.
		/// Geometry: scaled to actual values in cartesian coordinates as indicated by axes.
		/// </summary>
		/// <param name="icrc"></param>
		public void Transforms(IChartRenderContext icrc) {
			if (CategoryAxis == null || ValueAxis == null) return;
			var matx = MatrixSupport.TransformFor(icrc.Area, CategoryAxis, ValueAxis);
			_trace.Verbose($"{Name} mat:{matx} clip:{icrc.SeriesArea}");
			if (ClipToDataRegion) {
				Segments.Clip = new RectangleGeometry() { Rect = icrc.SeriesArea };
			}
			var mt = new MatrixTransform() { Matrix = matx };
			Geometry.Transform = mt;
			if (DebugClip != null) {
				DebugClip.Children.Clear();
				//DebugClip.Children.Add(new RectangleGeometry() { Rect = clip });
				DebugClip.Children.Add(new RectangleGeometry() { Rect = new Rect(icrc.Area.Left, icrc.Area.Top, matx.M11, ValueAxis.Range/2*matx.M22) });
				//_trace.Verbose($"{Name} rmat:{DebugClip.Transform}");
			}
		}
		#endregion
		#region IProvideLegend
		private Legend _legend;
		Legend IProvideLegend.Legend {
			get { if (_legend == null) _legend = Legend(); return _legend; }
		}
		Legend Legend() {
			return new Legend() { Title = Title, Fill = Segments.Fill, Stroke = Segments.Stroke };
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
			var y1 = ValueAxis.For(valuey);
			var y2 = ValueAxis.For(0);
			var topy = Math.Min(y1, y2);
			var bottomy = Math.Max(y1, y2);
			var leftx = (st.bl == null ? CategoryAxis.For(valuex) : CategoryAxis.For(new Tuple<double, String>(valuex, st.bl.For(item).ToString()))) + BarOffset;
			var rightx = leftx + BarWidth;
			_trace.Verbose($"{Name}[{index}] {valuey} ({leftx},{topy}) ({rightx},{bottomy})");
			var pf = PathHelper.Rectangle(leftx, topy, rightx, bottomy);
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
				UpdateLimits(st.ix + 1, 0);
			}
		}
		void IDataSourceRenderer.Postamble(object state) {
			Dirty = false;
		}
		#endregion
	}
	#endregion
}
