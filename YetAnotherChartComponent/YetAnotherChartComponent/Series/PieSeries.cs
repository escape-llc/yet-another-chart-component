using eScape.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	/// <summary>
	/// Pie-wedges series.
	/// The geometry is a circle of radius 10 centered at (0,0).
	/// </summary>
	public class PieSeries : DataSeries, IDataSourceRenderer, IProvideSeriesItemValues, IProvideCustomTransform, IProvideLegendDynamic, IRequireChartTheme, IRequireEnterLeave, IRequireTransforms {
		static LogTools.Flag _trace = LogTools.Add("PieSeries", LogTools.Level.Error);
		const double TO_RADS = Math.PI / 180.0;
		const double RADIUS = 10.0;
		#region SeriesItemState
		/// <summary>
		/// Isolate common features for the different implementations of item state.
		/// </summary>
		protected interface ISeriesItemCommon {
			/// <summary>
			/// Percentage.
			/// </summary>
			double Percent { get; set; }
			/// <summary>
			/// Label text.
			/// </summary>
			String Label { get; set; }
			/// <summary>
			/// Used for layout information.
			/// </summary>
			double PlacementAngle { get; set; }
			/// <summary>
			/// Create the item data.
			/// </summary>
			/// <returns>New instance.</returns>
			ISeriesItem Unwrap();
		}
		/// <summary>
		/// The item state.
		/// </summary>
		protected class SeriesItemState : ItemState<Path>, ISeriesItemCommon {
			/// <summary>
			/// See <see cref="ISeriesItemCommon.Percent"/>.
			/// </summary>
			public double Percent { get; set; }
			/// <summary>
			/// See <see cref="ISeriesItemCommon.Label"/>.
			/// </summary>
			public String Label { get; set; }
			/// <summary>
			/// See <see cref="ISeriesItemCommon.PlacementAngle"/>.
			/// </summary>
			public double PlacementAngle { get; set; }
			/// <summary>
			/// See <see cref="ISeriesItemCommon.Unwrap"/>.
			/// </summary>
			public ISeriesItem Unwrap() {
				var sis2 = new ISeriesItemValue[2];
				sis2[0] = new ChannelItemState(Index, XValue, XValueAfterOffset, Value, PlacementAngle, RADIUS / 2, Element, 0);
				sis2[1] = new ChannelItemState(Index, XValue, XValueAfterOffset, Percent, PlacementAngle, RADIUS / 2, Element, 1);
				var sivc = new ItemStateMultiChannelCore(Index, XValue, XValueAfterOffset, sis2);
				return sivc;
			}
			internal SeriesItemState(int idx, double xv, double xvo, double yv, Path ele, int ch) : base(idx, xv, xvo, yv, ele, ch) { }
		}
		/// <summary>
		/// Custom state version.
		/// </summary>
		protected class SeriesItemState_Custom : ItemStateCustom<Path>, ISeriesItemCommon {
			/// <summary>
			/// See <see cref="ISeriesItemCommon.Percent"/>.
			/// </summary>
			public double Percent { get; set; }
			/// <summary>
			/// See <see cref="ISeriesItemCommon.Label"/>.
			/// </summary>
			public String Label { get; set; }
			/// <summary>
			/// See <see cref="ISeriesItemCommon.PlacementAngle"/>.
			/// </summary>
			public double PlacementAngle { get; set; }
			/// <summary>
			/// See <see cref="ISeriesItemCommon.Unwrap"/>.
			/// </summary>
			public ISeriesItem Unwrap() {
				var sis2 = new ISeriesItemValue[2];
				sis2[0] = new ChannelItemState_Custom(Index, XValue, XValueAfterOffset, Value, PlacementAngle, RADIUS / 2, CustomValue, Element, 0);
				sis2[1] = new ChannelItemState_Custom(Index, XValue, XValueAfterOffset, Percent, PlacementAngle, RADIUS / 2, CustomValue, Element, 1);
				var sivc = new ItemStateMultiChannelCore(Index, XValue, XValueAfterOffset, sis2);
				return sivc;
			}
			internal SeriesItemState_Custom(int idx, double xv, double xvo, double yv, object cs, Path ele, int ch) : base(idx, xv, xvo, yv, cs, ele, ch) { }
		}
		/// <summary>
		/// Wrapper for the channel items.
		/// </summary>
		protected class ChannelItemState : ItemStateWithPlacement<Path> {
			readonly double hr;
			readonly double angle;
			/// <summary>
			/// Extract the geometry information and create placement.
			/// Cannot derive it from the ArcSegment so store it in <see cref="angle"/>.
			/// </summary>
			/// <returns></returns>
			protected override Placement CreatePlacement() {
				var dir = new Point(Math.Cos(angle), Math.Sin(angle));
				return new MidpointPlacement(new Point(dir.X*hr, dir.Y*hr), dir, hr);
			}
			internal ChannelItemState(int idx, double xv, double xvo, double yv, double degs, double hr, Path ele, int ch)
				: base(idx, xv, xvo, yv, ele, ch) { this.angle = degs * TO_RADS; this.hr = hr; }
		}
		/// <summary>
		/// Custom version.
		/// </summary>
		protected class ChannelItemState_Custom : ItemStateCustomWithPlacement<Path> {
			readonly double hr;
			readonly double angle;
			/// <summary>
			/// Extract the geometry information and create placement.
			/// Cannot derive it from the ArcSegment so store it in <see cref="angle"/>.
			/// </summary>
			/// <returns></returns>
			protected override Placement CreatePlacement() {
				var dir = new Point(Math.Cos(angle), Math.Sin(angle));
				return new MidpointPlacement(new Point(dir.X * hr, dir.Y * hr), dir, hr);
			}
			internal ChannelItemState_Custom(int idx, double xv, double xvo, double yv, double degs, double hr, object cs, Path ele, int ch)
				: base(idx, xv, xvo, yv, cs, ele, ch) { this.angle = degs * TO_RADS; this.hr = hr; }
		}
		#endregion
		#region properties
		/// <summary>
		/// Holder for IRequireChartTheme interface.
		/// </summary>
		public IChartTheme Theme { get; set; }
		/// <summary>
		/// Binding path to the value label.
		/// If ValueLabelrPath is NULL, the data-index is used.
		/// MAY be NULL, in which case no labels are used.
		/// </summary>
		public String ValueLabelPath { get { return (String)GetValue(ValueLabelPathProperty); } set { SetValue(ValueLabelPathProperty, value); } }
		/// <summary>
		/// Binding path to the value axis value.
		/// </summary>
		public String ValuePath { get { return (String)GetValue(ValuePathProperty); } set { SetValue(ValuePathProperty, value); } }
		/// <summary>
		/// The style generator for pie slices.
		/// </summary>
		public StyleGenerator Generator { get { return (StyleGenerator)GetValue(GeneratorProperty); } set { SetValue(GeneratorProperty, value); } }
		/// <summary>
		/// Provide item values.
		/// </summary>
		public IEnumerable<ISeriesItem> SeriesItemValues => UnwrapItemState(ItemState.AsReadOnly());
		/// <summary>
		/// The current set of legend items.
		/// </summary>
		public IEnumerable<Legend> LegendItems { get { return InternalLegendItems; } }
		/// <summary>
		/// The layer for components.
		/// </summary>
		protected IChartLayer Layer { get; set; }
		/// <summary>
		/// Data needed for current state.
		/// </summary>
		protected List<ItemState<Path>> ItemState { get; set; }
		/// <summary>
		/// Internal legend items.
		/// </summary>
		protected List<Legend> InternalLegendItems { get; set; }
		#endregion
		#region events
		/// <summary>
		/// Notify interested parties of changes in the legend items.
		/// </summary>
		public event TypedEventHandler<ChartComponent, LegendDynamicEventArgs> LegendChanged;
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="ValueLabelPath"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ValueLabelPathProperty = DependencyProperty.Register(
			nameof(ValueLabelPath), typeof(string), typeof(PieSeries), new PropertyMetadata(null, new PropertyChangedCallback(PropertyChanged_ValueDirty))
		);
		/// <summary>
		/// ValuePath DP.
		/// </summary>
		public static readonly DependencyProperty ValuePathProperty = DependencyProperty.Register(
			nameof(ValuePath), typeof(string), typeof(PieSeries), new PropertyMetadata(null, new PropertyChangedCallback(PropertyChanged_ValueDirty))
		);
		/// <summary>
		/// Identifies the <see cref="Generator"/> DP.
		/// </summary>
		public static readonly DependencyProperty GeneratorProperty = DependencyProperty.Register(
			nameof(Generator), typeof(StyleGenerator), typeof(PieSeries), new PropertyMetadata(null, new PropertyChangedCallback(PropertyChanged_ValueDirty))
		);
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		public PieSeries() {
			ItemState = new List<ItemState<Path>>();
			InternalLegendItems = new List<Legend>();
		}
		#endregion
		#region helpers
		IEnumerable<ISeriesItem> UnwrapItemState(IEnumerable<ItemState<Path>> siss) {
			foreach (var state in siss) {
				if (state is ISeriesItemCommon sis) {
					yield return sis.Unwrap();
				}
			}
		}
		/// <summary>
		/// Refresh the current state of the legend items.
		/// </summary>
		void UpdateLegend() {
			List<Legend> legend = new List<Legend>();
			foreach(var state in ItemState) {
				if (state is ISeriesItemCommon sic) {
					var leg = new Legend() {
						Title = sic.Label,
						Fill = state.Element.Style.Find<Brush>(Path.FillProperty),
						Stroke = state.Element.Style.Find<Brush>(Path.StrokeProperty)
					};
					legend.Add(leg);
				}
			}
			LegendChanged?.Invoke(this, new LegendDynamicEventArgs(InternalLegendItems, legend));
			InternalLegendItems = legend;
		}
		#endregion
		#region IRequireEnterLeave
		/// <summary>
		/// Initialize after entering VT.
		/// </summary>
		/// <param name="icelc"></param>
		void IRequireEnterLeave.Enter(IChartEnterLeaveContext icelc) {
			Layer = icelc.CreateLayer();
			_trace.Verbose($"enter d:{DataSourceName}");
			if(Generator == null) {
				// TODO error info
				Generator = new IdentityStyleGenerator() { BaseStyle = Theme.PathColumnSeries };
			}
		}
		/// <summary>
		/// Undo effects of Enter().
		/// </summary>
		/// <param name="icelc"></param>
		void IRequireEnterLeave.Leave(IChartEnterLeaveContext icelc) {
			_trace.Verbose($"leave");
			icelc.DeleteLayer(Layer);
			Layer = null;
		}
		#endregion
		#region IDataSourceRenderer
		/// <summary>
		/// Path factory for recycler.
		/// </summary>
		/// <returns></returns>
		Path CreatePath(ItemState<Path> ist) {
			var path = new Path {
				Style = Generator.NextStyle()
			};
			return path;
		}
		class State : RenderState_ValueAndLabel<ItemState<Path>, Path> {
			internal double totalv;
			internal BindingEvaluator bl;
			internal State(List<ItemState<Path>> sis, Recycler<Path, ItemState<Path>> rc, params BindingEvaluator[] bes) : base(sis, rc, new Evaluators(bes[0], bes[2], bes[3])) {
				bl = bes[1];
			}
		}
		object IDataSourceRenderer.Preamble(IChartRenderContext icrc) {
			if (String.IsNullOrEmpty(ValuePath)) return null;
			var by = new BindingEvaluator(ValuePath);
			// TODO report the binding error
			if (by == null) return null;
			var paths = ItemState.Select(ms => ms.Element);
			var recycler = new Recycler<Path, ItemState<Path>>(paths, CreatePath);
			Generator.Reset();
			return new State(new List<ItemState<Path>>(), recycler,
				null,
				!String.IsNullOrEmpty(ValueLabelPath) ? new BindingEvaluator(ValueLabelPath) : null,
				by,
				!String.IsNullOrEmpty(ValueLabelPath) ? new BindingEvaluator(ValueLabelPath) : null
			);
		}
		void IDataSourceRenderer.Render(object state, int index, object item) {
			var st = state as State;
			st.ix = index;
			var valuey = st.evs.ValueFor(item);
			var valuex = st.evs.CategoryFor(item, index);
			if (double.IsNaN(valuey)) {
				return;
			}
			st.totalv += Math.Abs(valuey);
			var label = st.bl == null ? String.Empty : st.bl.For(item).ToString();
			_trace.Verbose($"{Name}[{index}] {valuey}");
			var path = st.recycler.Next(null);
			if (path == null) return;
			// start with empty geometry
			path.Item2.Data = new PathGeometry();
			var cs = st.evs.LabelFor(item);
			if (cs == null) {
				st.itemstate.Add(new SeriesItemState(index, valuex, valuex, valuey, path.Item2, index) { Label = label });
			}
			else {
				st.itemstate.Add(new SeriesItemState_Custom(index, valuex, valuex, valuey, cs, path.Item2, index) { Label = label });
			}
		}
		PathFigure CreateSegment(double startangle, double degs) {
			var startx = Math.Cos(startangle * TO_RADS) * RADIUS;
			var starty = Math.Sin(startangle * TO_RADS) * RADIUS;
			var endx = Math.Cos((startangle + degs) * TO_RADS) * RADIUS;
			var endy = Math.Sin((startangle + degs) * TO_RADS) * RADIUS;
			_trace.Verbose($"{Name}.segment s:({startx:F2},{starty:F2}) e:({endx:F2},{endy:F2})");
			var pf = new PathFigure() { StartPoint = new Point(0, 0), IsClosed = true };
			var leg1 = new LineSegment() { Point = new Point(startx, starty) };
			var seg = new ArcSegment() { Size = new Size(RADIUS, RADIUS), Point = new Point(endx, endy), IsLargeArc = degs >= 180, SweepDirection = SweepDirection.Clockwise };
			pf.Segments.Add(leg1);
			pf.Segments.Add(seg);
			return pf;
		}
		void IDataSourceRenderer.RenderComplete(object state) {
			var st = state as State;
			var startangle = 0.0;
			foreach (var sta in st.itemstate) {
				var percent = Math.Abs(sta.Value) / st.totalv;
				var pg = sta.Element.Data as PathGeometry;
				if (sta is ISeriesItemCommon sic) {
					sic.Percent = percent;
					// lay out geometry
					var degs = sic.Percent * 360;
					sic.PlacementAngle = startangle + degs / 2;
					_trace.Verbose($"{Name}[{sta.Index}] {sta.Value} sa:{startangle:F2} degs:{degs:F2}");
					var pf = CreateSegment(startangle, degs);
					pg.Figures.Add(pf);
					startangle += degs;
				}
			}
		}
		void IDataSourceRenderer.Postamble(object state) {
			var st = state as State;
			ItemState = st.itemstate;
			Layer.Remove(st.recycler.Unused);
			Layer.Add(st.recycler.Created);
			Dirty = false;
			UpdateLegend();
		}
		#endregion
		#region IProvideCustomTransform
		/// <summary>
		/// The geometry is a circle of radius 10 centered at (0,0).
		/// </summary>
		/// <param name="area">Projection area.</param>
		/// <returns>MP matrix for this component's geometry.</returns>
		Matrix IProvideCustomTransform.TransformFor(Rect area) {
			var range = 2.0 * RADIUS;
			var mmatx = new Matrix(1.0 / range, 0, 0, -1.0 / range, RADIUS / range, RADIUS / range);
			var pmatx = MatrixSupport.ProjectionFor(area);
			// lock aspect ratio to smallest dimension so it's a circle
			var scale = Math.Min(pmatx.M11, pmatx.M22);
			pmatx.M11 = pmatx.M22 = scale;
			// TODO adjust offset so it's centered
			var matx = MatrixSupport.Multiply(pmatx, mmatx);
			_trace.Verbose($"{Name} mat:{matx} M:{mmatx} P:{pmatx}");
			return matx;
		}
		#endregion
		#region IRequireTransforms
		void IRequireTransforms.Transforms(IChartRenderContext icrc) {
			if (ItemState.Count == 0) return;
			var matx = (this as IProvideCustomTransform).TransformFor(icrc.Area);
			foreach (var sis in ItemState) {
				var pg = sis.Element.Data as PathGeometry;
				pg.Transform = new MatrixTransform() { Matrix = matx };
			}
		}
		#endregion
	}
}
