using eScape.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	/// <summary>
	/// Pie-wedges series.
	/// The geometry is a circle of radius 10 centered at (0,0).
	/// TODO need a DataSeries subclass without axis information.
	/// </summary>
	public class PieSeries : DataSeries, IDataSourceRenderer, IProvideSeriesItemValues, IProvideCustomTransform, IRequireChartTheme, IRequireEnterLeave, IRequireTransforms {
		static LogTools.Flag _trace = LogTools.Add("PieSeries", LogTools.Level.Error);
		const double TO_RADS = Math.PI / 180.0;
		const double RADIUS = 10.0;
		#region SeriesItemState
		/// <summary>
		/// The item state.
		/// </summary>
		protected class SeriesItemState : ItemState<Path> {
			internal double Percent { get; set; }
			internal String Label { get; set; }
			internal double PlacementAngle { get; set; }
			internal SeriesItemState(int idx, double xv, double xvo, double yv, Path ele, int ch) : base(idx, xv, xvo, yv, ele, ch) { }
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
		#endregion
		#region properties
		/// <summary>
		/// Holder for IRequireChartTheme interface.
		/// </summary>
		public IChartTheme Theme { get; set; }
		/// <summary>
		/// Binding path to the value axis value.
		/// </summary>
		public String ValuePath { get { return (String)GetValue(ValuePathProperty); } set { SetValue(ValuePathProperty, value); } }
		/// <summary>
		/// The style generator for pie slices.
		/// </summary>
		public StyleGenerator Generator { get { return (StyleGenerator)GetValue(GeneratorProperty); } set { SetValue(GeneratorProperty, value); } }
		/// <summary>
		/// The layer for components.
		/// </summary>
		protected IChartLayer Layer { get; set; }
		/// <summary>
		/// Data needed for current markers
		/// </summary>
		protected List<SeriesItemState> ItemState { get; set; }
		/// <summary>
		/// Provide item values.
		/// </summary>
		public IEnumerable<ISeriesItem> SeriesItemValues => UnwrapItemState(ItemState.AsReadOnly());
		#endregion
		#region DPs
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
			ItemState = new List<SeriesItemState>();
		}
		#endregion
		#region helpers
		IEnumerable<ISeriesItem> UnwrapItemState(IEnumerable<SeriesItemState> siss) {
			foreach (var sis in siss) {
				var sis2 = new ISeriesItemValue[2];
				sis2[0] = new ChannelItemState(sis.Index, sis.XValueIndex, sis.XValueOffset, sis.YValue, sis.PlacementAngle, RADIUS/2, sis.Element, 0);
				sis2[1] = new ChannelItemState(sis.Index, sis.XValueIndex, sis.XValueOffset, sis.Percent, sis.PlacementAngle, RADIUS/2, sis.Element, 1);
				var sivc = new ItemStateMultiChannelCore(sis.Index, sis.XValueIndex, sis.XValueOffset, sis2);
				yield return sivc;
			}
		}
		#endregion
		#region IRequireEnterLeave
		/// <summary>
		/// Initialize after entering VT.
		/// </summary>
		/// <param name="icelc"></param>
		void IRequireEnterLeave.Enter(IChartEnterLeaveContext icelc) {
			//EnsureAxes(icelc as IChartComponentContext);
			Layer = icelc.CreateLayer();
			_trace.Verbose($"enter v:{ValueAxisName}:{ValueAxis} c:{CategoryAxisName}:{CategoryAxis} d:{DataSourceName}");
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
			ValueAxis = null;
			CategoryAxis = null;
			icelc.DeleteLayer(Layer);
			Layer = null;
		}
		#endregion
		#region IDataSourceRenderer
		/// <summary>
		/// Path factory for recycler.
		/// </summary>
		/// <returns></returns>
		Path CreatePath() {
			var path = new Path {
				Style = Generator.NextStyle()
			};
			return path;
		}
		class State : RenderState_ValueAndLabel<SeriesItemState, Path> {
			internal double totalv;
			internal State(List<SeriesItemState> sis, Recycler<Path> rc, params BindingEvaluator[] bes) : base(sis, rc, bes[0], bes[1], bes[2]) { }
		}
		object IDataSourceRenderer.Preamble(IChartRenderContext icrc) {
			if (String.IsNullOrEmpty(ValuePath)) return null;
			var by = new BindingEvaluator(ValuePath);
			// TODO report the binding error
			if (by == null) return null;
			var paths = ItemState.Select(ms => ms.Element);
			var recycler = new Recycler<Path>(paths, CreatePath);
			Generator.Reset();
			return new State(new List<SeriesItemState>(), recycler,
				!String.IsNullOrEmpty(CategoryPath) ? new BindingEvaluator(CategoryPath) : null,
				!String.IsNullOrEmpty(CategoryLabelPath) ? new BindingEvaluator(CategoryLabelPath) : null,
				by
			);
		}
		void IDataSourceRenderer.Render(object state, int index, object item) {
			var st = state as State;
			st.ix = index;
			var valuey = CoerceValue(item, st.by);
			var valuex = st.bx != null ? (double)st.bx.For(item) : index;
			if (double.IsNaN(valuey)) {
				return;
			}
			st.totalv += Math.Abs(valuey);
			var label = st.bl == null ? String.Empty : st.bl.For(item).ToString();
			_trace.Verbose($"{Name}[{index}] {valuey}");
			var path = st.NextElement();
			if (path == null) return;
			// start with empty geometry
			path.Data = new PathGeometry();
			st.itemstate.Add(new SeriesItemState(index, valuex, valuex, valuey, path, index) { Label = label });
		}
		void IDataSourceRenderer.RenderComplete(object state) {
			var st = state as State;
			var startangle = 0.0;
			foreach (var sis in st.itemstate) {
				sis.Percent = Math.Abs(sis.YValue) / st.totalv;
				// lay out geometry
				var degs = sis.Percent*360;
				var pg = sis.Element.Data as PathGeometry;
				sis.PlacementAngle = startangle + degs / 2;
				var startx = Math.Cos(startangle*TO_RADS) * RADIUS;
				var starty = Math.Sin(startangle*TO_RADS) * RADIUS;
				var endx = Math.Cos((startangle + degs)*TO_RADS) * RADIUS;
				var endy = Math.Sin((startangle + degs)*TO_RADS) * RADIUS;
				_trace.Verbose($"{Name}[{sis.Index}] {sis.YValue} sa:{startangle:F2} degs:{degs:F2} s:({startx:F2},{starty:F2}) e:({endx:F2},{endy:F2})");
				var pf = new PathFigure() { StartPoint = new Point(0, 0), IsClosed = true };
				var leg1 = new LineSegment() { Point = new Point(startx, starty) };
				var seg = new ArcSegment() { Size = new Size(RADIUS, RADIUS), Point = new Point(endx, endy), IsLargeArc = degs >= 180, SweepDirection = SweepDirection.Clockwise };
				pf.Segments.Add(leg1);
				pf.Segments.Add(seg);
				pg.Figures.Add(pf);
				startangle += degs;
			}
		}
		void IDataSourceRenderer.Postamble(object state) {
			var st = state as State;
			ItemState = st.itemstate;
			Layer.Remove(st.recycler.Unused);
			Layer.Add(st.recycler.Created);
			Dirty = false;
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
