using eScape.Core;
using System;
using System.Collections.Specialized;
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
		#region data source
		/// <summary>
		/// Identifies <see cref="DataSource"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty DataSourceProperty = DependencyProperty.Register(
			"DataSource", typeof(System.Collections.IEnumerable), typeof(DataSeries), new PropertyMetadata(null, new PropertyChangedCallback(DataSourcePropertyChanged))
		);
		private static void DataSourcePropertyChanged(DependencyObject dobj, DependencyPropertyChangedEventArgs dpcea) {
			DataSeries ds = dobj as DataSeries;
			if (dpcea.OldValue != dpcea.NewValue) {
				DetachDataSourceCollectionChanged(ds, dpcea.OldValue);
				AttachDataSourceCollectionChanged(ds, dpcea.NewValue);
				ds.Dirty = true;
				ds.ProcessData(dpcea.Property);
			}
		}
		private static void DetachDataSourceCollectionChanged(DataSeries ds, object dataSource) {
			if (dataSource is INotifyCollectionChanged) {
				(dataSource as INotifyCollectionChanged).CollectionChanged -= ds.DataSourceCollectionChanged;
			}
		}
		private static void AttachDataSourceCollectionChanged(DataSeries ds, object dataSource) {
			if (dataSource is INotifyCollectionChanged) {
				(dataSource as INotifyCollectionChanged).CollectionChanged += new NotifyCollectionChangedEventHandler(ds.DataSourceCollectionChanged);
			}
		}
		private void DataSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs nccea) {
			Dirty = true;
			ProcessData(DataSourceProperty);
		}
		#endregion
		#region category/value member path
		public static readonly DependencyProperty ValueMemberPathProperty = DependencyProperty.Register(
			"ValueMemberPath", typeof(string), typeof(DataSeries), new PropertyMetadata(null, new PropertyChangedCallback(DataSeriesPropertyChanged))
		);
		public static readonly DependencyProperty CategoryMemberPathProperty = DependencyProperty.Register(
			"CategoryMemberPath", typeof(string), typeof(DataSeries), new PropertyMetadata(null, new PropertyChangedCallback(DataSeriesPropertyChanged))
		);
		/// <summary>
		/// Generic DP property change handler.
		/// Calls DataSeries.ProcessData().
		/// </summary>
		/// <param name="d"></param>
		/// <param name="dpcea"></param>
		private static void DataSeriesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs dpcea) {
			DataSeries ds = d as DataSeries;
			ds.ProcessData(dpcea.Property);
		}
		#endregion
		#region properties
		/// <summary>
		/// Data source for the series.
		/// </summary>
		public System.Collections.IEnumerable DataSource { get { return (System.Collections.IEnumerable)GetValue(DataSourceProperty); } set { SetValue(DataSourceProperty, value); } }
		/// <summary>
		/// Binding path to the category axis value.
		/// MAY be NULL, in which case the data-index is used instead.
		/// </summary>
		public String CategoryMemberPath { get { return (String)GetValue(CategoryMemberPathProperty); } set { SetValue(CategoryMemberPathProperty, value); } }
		/// <summary>
		/// Binding path to the value axis value.
		/// </summary>
		public String ValueMemberPath { get { return (String)GetValue(ValueMemberPathProperty); } set { SetValue(ValueMemberPathProperty, value); } }
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
			if (dp == DataSourceProperty) return "DataSource";
			else if (dp == ValueMemberPathProperty) return "ValueMemberPath";
			else if (dp == CategoryMemberPathProperty) return "CategoryMemberPath";
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
		/// <param name="vx"></param>
		/// <param name="vy"></param>
		protected void UpdateLimits(double vx, double vy) {
			if (double.IsNaN(Minimum) || vy < Minimum) { Minimum = vy; }
			if (double.IsNaN(Maximum) || vy > Maximum) { Maximum = vy; }
			if (double.IsNaN(CategoryMinimum) || vx < CategoryMinimum) { CategoryMinimum = vx; }
			if (double.IsNaN(CategoryMaximum) || vx > CategoryMaximum) { CategoryMaximum = vx; }
		}
		/// <summary>
		/// Reset the value and category limits.
		/// </summary>
		protected void ResetLimits() {
			Minimum = double.NaN; Maximum = double.NaN;
			CategoryMinimum = double.NaN; CategoryMaximum = double.NaN;
		}
		#endregion
		#region extensions
		/// <summary>
		/// Iterate through the DataSource and compute visuals.
		/// </summary>
		/// <param name="dp">Triggering DP; if unknown, SHOULD be DataSourceProperty.</param>
		protected abstract void ProcessData(DependencyProperty dp);
		#endregion
	}
	#endregion
	#region LineSeries
	/// <summary>
	/// Data series that generates a Polyline visual.
	/// </summary>
	public class LineSeries : DataSeries {
		static LogTools.Flag _trace = LogTools.Add("LineSeries", LogTools.Level.Verbose);
		#region properties
		public int StrokeThickness { get; set; } = 1;
		public PenLineJoin StrokeLineJoin { get; set; } = PenLineJoin.Bevel;
		public PenLineCap StrokeStartLineCap { get; set; } = PenLineCap.Flat;
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
		public LineSeries() {
			Segments = new Path();
			Segments.StrokeThickness = 1;
			Geometry = new PathGeometry();
			Segments.Data = Geometry;
		}
		#endregion
		#region extensions
		public override void Enter(IChartEnterLeaveContext icelc) {
			_trace.Verbose($"enter v:{ValueAxisName} c:{ValueAxisName} d:{DataSource}");
			icelc.Add(Segments);
			BindTo(this, "Stroke", Segments, Path.StrokeProperty);
			Segments.StrokeThickness = StrokeThickness;
			Segments.StrokeEndLineCap = StrokeEndLineCap;
			Segments.StrokeLineJoin = StrokeLineJoin;
			Segments.StrokeStartLineCap = StrokeStartLineCap;
		}
		public override void Leave(IChartEnterLeaveContext icelc) {
			_trace.Verbose($"leave v:{ValueAxisName} c:{ValueAxisName} d:{DataSource}");
			icelc.Remove(Segments);
		}
		public override void Render(IChartRenderContext icrc) {
			EnsureAxes(icrc);
			_trace.Verbose($"render v:{ValueAxis} c:{CategoryAxis} d:{DataSource} dirty:{Dirty}");
			if (ValueAxis == null || CategoryAxis == null || DataSource == null) return;
			if (Dirty) {
				ProcessData(DataSourceProperty);
			}
			else {
				ValueAxis.For(Minimum);
				ValueAxis.For(Maximum);
				CategoryAxis.For(CategoryMinimum);
				CategoryAxis.For(CategoryMaximum);
			}
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
			var matx = new Matrix(scalex, 0, 0, -scaley, icrc.Area.Left + offsetx, icrc.Area.Top + icrc.Area.Height/2);
			_trace.Verbose($"scale {scalex:F3},{scaley:F3} mat:{matx}");
			Geometry.Transform = new MatrixTransform() { Matrix = matx };
		}
		/// <summary>
		/// Re-calculate visuals and clear Dirty flag.
		/// </summary>
		/// <param name="dp"></param>
		protected override void ProcessData(DependencyProperty dp) {
			_trace.Verbose($"process-data dp:{DPName(dp)}");
			if (ValueAxis == null || CategoryAxis == null || DataSource == null) return;
			if (String.IsNullOrEmpty(ValueMemberPath)) return;
			var by = new BindingEvaluator(ValueMemberPath);
			var bx = !String.IsNullOrEmpty(CategoryMemberPath) ? new BindingEvaluator(CategoryMemberPath) : null;
			int ix = 0;
			ResetLimits();
			Geometry.Figures.Clear();
			var pf = new PathFigure();
			foreach (var vx in DataSource) {
				// TODO handle datetime et al values that aren't double
				var valuey = (double)by.For(vx);
				var valuex = bx != null ? (double)bx.For(vx) : ix;
				UpdateLimits(valuex, valuey);
				var mappedy = ValueAxis.For(valuey);
				var mappedx = CategoryAxis.For(valuex);
				_trace.Verbose($"[{ix}] {valuey} ({mappedx},{mappedy})");
				if(ix == 0) {
					pf.StartPoint = new Point(mappedx, mappedy);
				}
				else {
					pf.Segments.Add(new LineSegment() { Point = new Point(mappedx, mappedy) });
				}
				ix++;
			}
			Geometry.Figures.Add(pf);
			Dirty = false;
		}
		#endregion
	}
	#endregion
	#region ColumnSeries
	/// <summary>
	/// If there's no CategoryMemberPath defined (i.e. using data index) this component reserves one "extra" cell on the Category Axis, to present the last column(s).
	/// Category axis cells start on the left and extend rightward (in device X-units).
	/// </summary>
	public class ColumnSeries : DataSeries {
		static LogTools.Flag _trace = LogTools.Add("ColumnSeries", LogTools.Level.Verbose);
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
		protected Path Segments { get; set; }
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
		public ColumnSeries() {
			Segments = new Path();
			Segments.StrokeThickness = 1;
			Geometry = new PathGeometry();
			Segments.Data = Geometry;
		}
		#endregion
		#region extensions
		public override void Enter(IChartEnterLeaveContext icelc) {
			_trace.Verbose($"enter v:{ValueAxisName} c:{ValueAxisName} d:{DataSource}");
			icelc.Add(Segments);
			BindTo(this, "Stroke", Segments, Path.StrokeProperty);
			BindTo(this, "Fill", Segments, Path.FillProperty);
		}
		public override void Leave(IChartEnterLeaveContext icelc) {
			_trace.Verbose($"leave v:{ValueAxisName} c:{ValueAxisName} d:{DataSource}");
			icelc.Remove(Segments);
		}
		public override void Render(IChartRenderContext icrc) {
			EnsureAxes(icrc);
			_trace.Verbose($"render v:{ValueAxis} c:{CategoryAxis} d:{DataSource} dirty:{Dirty}");
			if (ValueAxis == null || CategoryAxis == null || DataSource == null) return;
			if (Dirty) {
				ProcessData(DataSourceProperty);
			} else {
				ValueAxis.For(Minimum);
				ValueAxis.For(Maximum);
				CategoryAxis.For(CategoryMinimum);
				CategoryAxis.For(CategoryMaximum);
			}
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
			var matx = new Matrix(scalex, 0, 0, -scaley, icrc.Area.Left, icrc.Area.Top + icrc.Area.Height / 2);
			_trace.Verbose($"scale {scalex:F3},{scaley:F3} mat:{matx}");
			Geometry.Transform = new MatrixTransform() { Matrix = matx };
		}
		protected override void ProcessData(DependencyProperty dp) {
			_trace.Verbose($"process-data dp:{DPName(dp)}");
			if (ValueAxis == null || CategoryAxis == null || DataSource == null) return;
			if (String.IsNullOrEmpty(ValueMemberPath)) return;
			var by = new BindingEvaluator(ValueMemberPath);
			var bx = !String.IsNullOrEmpty(CategoryMemberPath) ? new BindingEvaluator(CategoryMemberPath) : null;
			int ix = 0;
			ResetLimits();
			Geometry.Figures.Clear();
			foreach (var vx in DataSource) {
				// TODO handle datetime et al values that aren't double
				var valuey = (double)by.For(vx);
				var valuex = bx != null ? (double)bx.For(vx) : ix;
				UpdateLimits(valuex, valuey);
				UpdateLimits(valuex, 0);
				var topy = ValueAxis.For(valuey);
				var bottomy = ValueAxis.For(0);
				var leftx = CategoryAxis.For(valuex) + BarOffset;
				var rightx = leftx + BarWidth;
				_trace.Verbose($"[{ix}] {valuey} ({leftx},{topy}) ({rightx},{bottomy})");
				var pf = PathHelper.Rectangle(leftx, Math.Max(topy, bottomy), rightx, Math.Min(topy, bottomy));
				Geometry.Figures.Add(pf);
				ix++;
			}
			if (bx == null) {
				// needs one extra "cell"
				CategoryAxis.For(ix);
			}
			//LastDataSourceCount = ix;
			Dirty = false;
		}
		#endregion
	}
	#endregion
}
