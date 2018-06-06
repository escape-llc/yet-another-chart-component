using eScape.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	/// <summary>
	/// Represents an individual item in the stack.
	/// </summary>
	public class ColumnStackItem : DependencyObject {
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
		/// Binding path to the value axis value.
		/// </summary>
		public String ValuePath { get { return (String)GetValue(ValuePathProperty); } set { SetValue(ValuePathProperty, value); } }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="PathStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty PathStyleProperty = DependencyProperty.Register(
			nameof(PathStyle), typeof(Style), typeof(ColumnStackItem), new PropertyMetadata(null)
		);
		/// <summary>
		/// Identifies <see cref="Title"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
			nameof(Title), typeof(String), typeof(ColumnStackItem), new PropertyMetadata("Title")
		);
		/// <summary>
		/// ValuePath DP.
		/// </summary>
		public static readonly DependencyProperty ValuePathProperty = DependencyProperty.Register(
			nameof(ValuePath), typeof(string), typeof(ColumnStackItem), new PropertyMetadata(null, new PropertyChangedCallback(ItemPropertyChanged))
		);
		/// <summary>
		/// Generic DP property change handler.
		/// Cascade up to the series.
		/// </summary>
		/// <param name="d"></param>
		/// <param name="dpcea"></param>
		protected static void ItemPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs dpcea) {
			ColumnStackItem csi = d as ColumnStackItem;
			//ds.Refresh(RefreshRequestType.ValueDirty, AxisUpdateState.Unknown);
		}
		#endregion
	}
	/// <summary>
	/// Formality for XAML happiness.
	/// </summary>
	public class ColumnStackItemCollection : ObservableCollection<ColumnStackItem> { }
	/// <summary>
	/// Stacked column series.
	/// Plots multiple series values on a stacked arrangement.
	/// </summary>
	public class StackedColumnSeries :DataSeriesWithAxes, IProvideLegend, IProvideSeriesItemValues, IRequireChartTheme, IRequireEnterLeave, IDataSourceRenderer, IRequireTransforms {
		static LogTools.Flag _trace = LogTools.Add("StackedColumnSeries", LogTools.Level.Error);
		#region SeriesItemState
		/// <summary>
		/// Item state.
		/// </summary>
		protected class SeriesItemState : ItemStateCore {
			/// <summary>
			/// Track the "low" end of the column.
			/// </summary>
			public double Min { get; set; }
			/// <summary>
			/// Track the "high" end of the column.
			/// </summary>
			public double Max { get; set; }
			/// <summary>
			/// The list of paths created for the column.
			/// </summary>
			public List<Tuple<double, Path>> Elements { get; private set; } = new List<Tuple<double, Path>>();
			/// <summary>
			/// Bookkeeping for Min/Max values.
			/// </summary>
			/// <param name="vx"></param>
			public void UpdateLimits(double vx) {
				if (double.IsNaN(vx)) return;
				if (vx < Min) Min = vx;
				if (vx > Max) Max = vx;
			}
			/// <summary>
			/// Ctor.
			/// </summary>
			/// <param name="idx"></param>
			/// <param name="xv"></param>
			/// <param name="xvo"></param>
			public SeriesItemState(int idx, double xv, double xvo) :base(idx, xv, xvo){ }
		}
		/// <summary>
		/// Custom state version.
		/// </summary>
		protected class SeriesItemState_Custom : SeriesItemState {
			/// <summary>
			/// The custom state.
			/// </summary>
			public object CustomValue { get; private set; }
			/// <summary>
			/// Ctor.
			/// </summary>
			/// <param name="idx"></param>
			/// <param name="xv"></param>
			/// <param name="xvo"></param>
			/// <param name="cs"></param>
			public SeriesItemState_Custom(int idx, double xv, double xvo, object cs) :base(idx, xv, xvo) { CustomValue = cs; }
		}
		/// <summary>
		/// Wrapper for the channel items.
		/// </summary>
		protected class ChannelItemState : ItemStateWithPlacement<Path> {
			/// <summary>
			/// Extract the rectangle geometry and create placement.
			/// </summary>
			/// <returns></returns>
			protected override Placement CreatePlacement() { return new RectanglePlacement(Value >= 0 ? Placement.UP_RIGHT : Placement.DOWN_RIGHT, (Element.Data as RectangleGeometry).Rect); }
			internal ChannelItemState(int idx, double xv, double xvo, double yv, Path ele, int ch) : base(idx, xv, xvo, yv, ele, ch) { }
		}
		/// <summary>
		/// Wrapper for the channel items.
		/// </summary>
		protected class ChannelItemState_Custom : ItemStateCustomWithPlacement<Path> {
			/// <summary>
			/// Extract the rectangle geometry and create placement.
			/// </summary>
			/// <returns></returns>
			protected override Placement CreatePlacement() { return new RectanglePlacement(Value >= 0 ? Placement.UP_RIGHT : Placement.DOWN_RIGHT, (Element.Data as RectangleGeometry).Rect); }
			internal ChannelItemState_Custom(int idx, double xv, double xvo, double yv, object cs, Path ele, int ch) : base(idx, xv, xvo, yv, cs, ele, ch) { }
		}
		#endregion
		#region properties
		/// <summary>
		/// Holder for IRequireChartTheme interface.
		/// </summary>
		public IChartTheme Theme { get; set; }
		/// <summary>
		/// The stack of column items.
		/// </summary>
		public ColumnStackItemCollection ColumnStack { get; private set; }
		/// <summary>
		/// Binding path to the value axis label.
		/// MAY be NULL.
		/// If specified, this value will augment the one used for All Channels in <see cref="ISeriesItemValue"/>.
		/// </summary>
		public String ValueLabelPath { get { return (String)GetValue(ValueLabelPathProperty); } set { SetValue(ValueLabelPathProperty, value); } }
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
		/// Provide item values.
		/// </summary>
		public IEnumerable<ISeriesItem> SeriesItemValues => UnwrapItemState(ItemState.AsReadOnly());
		/// <summary>
		/// The layer for components.
		/// </summary>
		protected IChartLayer Layer { get; set; }
		/// <summary>
		/// Data needed for current markers
		/// </summary>
		protected List<SeriesItemState> ItemState { get; set; }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="ValueLabelPath"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ValueLabelPathProperty = DependencyProperty.Register(
			nameof(ValueLabelPath), typeof(string), typeof(StackedColumnSeries), new PropertyMetadata(null, new PropertyChangedCallback(PropertyChanged_ValueDirty))
		);
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		public StackedColumnSeries() {
			ColumnStack = new ColumnStackItemCollection();
			ItemState = new List<SeriesItemState>();
		}
		#endregion
		#region extensions
		/// <summary>
		/// Implement for this class.
		/// </summary>
		protected override void ReconfigureLimits() {
			ResetLimits();
			for (int ix = 0; ix < ItemState.Count; ix++) {
				UpdateLimits(ItemState[ix].XValue, ItemState[ix].Elements.Select(xx => xx.Item1));
			}
		}
		#endregion
		#region helpers
		IEnumerable<ISeriesItem> UnwrapItemState(IEnumerable<SeriesItemState> siss) {
			foreach (var sis in siss) {
				// IST check Custom first it's a subclass!
				if (sis is SeriesItemState_Custom sisc) {
					var sis2 = new ISeriesItemValue[sis.Elements.Count];
					for (int idx = 0; idx < sis.Elements.Count; idx++) {
						sis2[idx] = new ChannelItemState_Custom(sis.Index, sis.XValue, sis.XValueAfterOffset, sis.Elements[idx].Item1, sisc.CustomValue, sis.Elements[idx].Item2, idx);
					}
					var sivc = new ItemStateMultiChannelCore(sis.Index, sis.XValue, sis.XValueAfterOffset, sis2);
					yield return sivc;
				} else {
					var sis2 = new ISeriesItemValue[sis.Elements.Count];
					for (int idx = 0; idx < sis.Elements.Count; idx++) {
						sis2[idx] = new ChannelItemState(sis.Index, sis.XValue, sis.XValueAfterOffset, sis.Elements[idx].Item1, sis.Elements[idx].Item2, idx);
					}
					var sivc = new ItemStateMultiChannelCore(sis.Index, sis.XValue, sis.XValueAfterOffset, sis2);
					yield return sivc;
				}
			}
		}
		#endregion
		#region IProvideLegend
		private Legend[] _legend;
		IEnumerable<Legend> IProvideLegend.LegendItems {
			get { if (_legend == null) _legend = Legend(); return _legend; }
		}
		Legend[] Legend() {
			var items = new List<Legend>();
			foreach(var stk in ColumnStack) {
				items.Add(new Legend() { Title = stk.Title, Fill = stk.PathStyle.Find<Brush>(Path.FillProperty), Stroke = stk.PathStyle.Find<Brush>(Path.StrokeProperty) });
			}
			return items.ToArray();
		}
		#endregion
		#region IRequireEnterLeave
		void IRequireEnterLeave.Enter(IChartEnterLeaveContext icelc) {
			EnsureAxes(icelc as IChartComponentContext);
			Layer = icelc.CreateLayer();
			_trace.Verbose($"{Name} enter v:{ValueAxisName} {ValueAxis} c:{CategoryAxisName} {CategoryAxis} d:{DataSourceName}");
			for (int ix = 0; ix < ColumnStack.Count; ix++) {
				if(String.IsNullOrEmpty(ColumnStack[ix].ValuePath)) {
					if(icelc is IChartErrorInfo icei) {
						icei.Report(new ChartValidationResult(NameOrType(), $"ValuePath[{ix}] was not set, NO values are generated", new[] { $"ValuePath[{ix}]" }));
					}
				}
			}
		}
		void IRequireEnterLeave.Leave(IChartEnterLeaveContext icelc) {
			_trace.Verbose($"{Name} leave");
			ValueAxis = null;
			CategoryAxis = null;
			icelc.DeleteLayer(Layer);
			Layer = null;
		}
		#endregion
		#region IRequireTransforms
		void IRequireTransforms.Transforms(IChartRenderContext icrc) {
			if (CategoryAxis == null || ValueAxis == null) return;
			if (ItemState.Count == 0) return;
			var matx = MatrixSupport.TransformFor(icrc.Area, CategoryAxis, ValueAxis);
			_trace.Verbose($"{Name} mat:{matx} clip:{icrc.SeriesArea}");
			var mt = new MatrixTransform() { Matrix = matx };
			foreach (var ss in ItemState) {
				foreach (var el in ss.Elements) {
					el.Item2.Data.Transform = mt;
					if (ClipToDataRegion) {
						var cg = new RectangleGeometry() { Rect = icrc.SeriesArea };
						el.Item2.Clip = cg;
					}
				}
			}
		}
		#endregion
		#region IDataSourceRenderer
		class State : RenderStateCore<SeriesItemState, Path> {
			internal readonly BindingEvaluator bx;
			internal readonly BindingEvaluator[] bys;
			internal readonly BindingEvaluator byl;
			internal State(List<SeriesItemState> sis, Recycler<Path, SeriesItemState> rc, BindingEvaluator bx, BindingEvaluator byl, BindingEvaluator[] bys) :base(sis, rc) {
				this.bx = bx;
				this.byl = byl;
				this.bys = bys;
			}
		}
		/// <summary>
		/// Path factory for recycler.
		/// </summary>
		/// <returns></returns>
		Path CreatePath(SeriesItemState sis) {
			var path = new Path();
			return path;
		}
		object IDataSourceRenderer.Preamble(IChartRenderContext icrc) {
			if (ValueAxis == null || CategoryAxis == null) return null;
			var bys = new BindingEvaluator[ColumnStack.Count];
			for (int ix = 0; ix < ColumnStack.Count; ix++) {
				bys[ix] = String.IsNullOrEmpty(ColumnStack[ix].ValuePath) ? null : new BindingEvaluator(ColumnStack[ix].ValuePath);
			}
			ResetLimits();
			var paths = ItemState.Select(ms => ms.Elements).SelectMany(el=>el).Select(el=>el.Item2);
			var recycler = new Recycler<Path, SeriesItemState>(paths, CreatePath);
			return new State(new List<SeriesItemState>(), recycler,
				!String.IsNullOrEmpty(CategoryPath) ? new BindingEvaluator(CategoryPath) : null,
				!String.IsNullOrEmpty(ValueLabelPath) ? new BindingEvaluator(ValueLabelPath) : null,
				bys
			);
		}
		void IDataSourceRenderer.Render(object state, int index, object item) {
			var st = state as State;
			var valuex = st.bx != null ? (double)st.bx.For(item) : index;
			st.ix = index;
			var leftx = CategoryAxis.For(valuex);
			var barx = leftx + BarOffset;
			var rightx = barx + BarWidth;
			var sis = st.byl == null ? new SeriesItemState(index, leftx, barx) : new SeriesItemState_Custom(index, leftx, barx, st.byl.For(item));
			for (int ix = 0; ix < st.bys.Length; ix++) {
				var valuey = CoerceValue(item, st.bys[ix]);
				if (double.IsNaN(valuey)) {
					continue;
				}
				var colbase = valuey >= 0 ? sis.Max : sis.Min;
				var colend = colbase + valuey;
				var y1 = ValueAxis.For(colend);
				var y2 = ValueAxis.For(colbase);
				var topy = Math.Max(y1, y2);
				var bottomy = Math.Min(y1, y2);
				sis.UpdateLimits(y1);
				_trace.Verbose($"{Name}[{index},{ix}] {valuey} ({barx},{topy}) ({rightx},{bottomy}) sis ({sis.Min},{sis.Max})");
				var path = st.recycler.Next(null);
				if (path == null) return;
				var rg = new RectangleGeometry() { Rect = new Rect(new Point(barx, topy), new Point(rightx, bottomy)) };
				path.Item2.Data = rg;
				BindTo(ColumnStack[ix], "PathStyle", path.Item2, FrameworkElement.StyleProperty);
				UpdateLimits(valuex, sis.Min, sis.Max);
				sis.Elements.Add(new Tuple<double, Path>(y1, path.Item2));
			}
			st.itemstate.Add(sis);
		}

		void IDataSourceRenderer.RenderComplete(object state) {
			var st = state as State;
			if (st.bx == null) {
				// needs one extra "cell"
				UpdateLimits(st.ix + 1, double.NaN);
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
	}
}
