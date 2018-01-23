﻿using eScape.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	/// <summary>
	/// Represents an individual item in the stack.
	/// </summary>
	public class ColumnStackItem : FrameworkElement {
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
	public class StackedColumnSeries :DataSeries, IProvideLegend, IRequireChartTheme, IRequireEnterLeave, IDataSourceRenderer, IRequireTransforms {
		static LogTools.Flag _trace = LogTools.Add("StackedColumnSeries", LogTools.Level.Error);
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
		}
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
		/// The layer for components.
		/// </summary>
		protected IChartLayer Layer { get; set; }
		/// <summary>
		/// Data needed for current markers
		/// </summary>
		protected List<SeriesItemState> ItemState { get; set; }
		#endregion
		#region DPs
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
		class State {
			internal BindingEvaluator bx;
			internal BindingEvaluator[] bys;
			internal BindingEvaluator bl;
			internal int ix;
			internal List<SeriesItemState> ms;
			internal Recycler<Path> recycler;
			internal IEnumerator<Path> paths;
			internal Path NextPath() {
				if (paths.MoveNext()) return paths.Current;
				else return null;
			}
		}
		/// <summary>
		/// Path factory for recycler.
		/// </summary>
		/// <returns></returns>
		Path CreatePath() {
			var path = new Path();
			return path;
		}
		object IDataSourceRenderer.Preamble(IChartRenderContext icrc) {
			if (ValueAxis == null || CategoryAxis == null) return null;
			foreach(var csi in ColumnStack) {
				// TODO report the binding error
				if (String.IsNullOrEmpty(csi.ValuePath)) return null;
			}
			var bys = new BindingEvaluator[ColumnStack.Count];
			for (int ix = 0; ix < ColumnStack.Count; ix++) {
				var by = new BindingEvaluator(ColumnStack[ix].ValuePath);
				bys[ix] = by;
			}
			ResetLimits();
			var paths = ItemState.Select(ms => ms.Elements).SelectMany(el=>el).Select(el=>el.Item2);
			var recycler = new Recycler<Path>(paths, CreatePath);
			return new State() {
				bx = !String.IsNullOrEmpty(CategoryPath) ? new BindingEvaluator(CategoryPath) : null,
				bl = !String.IsNullOrEmpty(CategoryLabelPath) ? new BindingEvaluator(CategoryLabelPath) : null,
				bys = bys,
				ms = new List<SeriesItemState>(),
				recycler = recycler,
				paths = recycler.Items().GetEnumerator()
			};
		}

		void IDataSourceRenderer.Render(object state, int index, object item) {
			var st = state as State;
			var valuex = st.bx != null ? (double)st.bx.For(item) : index;
			st.ix = index;
			var leftx = (st.bl == null ? CategoryAxis.For(valuex) : CategoryAxis.For(new Tuple<double, String>(valuex, st.bl.For(item).ToString()))) + BarOffset;
			var rightx = leftx + BarWidth;
			var sis = new SeriesItemState() { Index = index, XValue = leftx };
			for (int ix = 0; ix < st.bys.Length; ix++) {
				var valuey = (double)st.bys[ix].For(item);
				if (double.IsNaN(valuey)) {
					if (st.bl != null) {
						// still map the X
						CategoryAxis.For(new Tuple<double, String>(valuex, st.bl.For(item).ToString()));
						UpdateLimits(valuex, double.NaN);
					}
					continue;
				}
				var colbase = valuey >= 0 ? sis.Max : sis.Min;
				var colend = colbase + valuey;
				var y1 = ValueAxis.For(colend);
				var y2 = ValueAxis.For(colbase);
				var topy = Math.Max(y1, y2);
				var bottomy = Math.Min(y1, y2);
				sis.UpdateLimits(y1);
				_trace.Verbose($"{Name}[{index},{ix}] {valuey} ({leftx},{topy}) ({rightx},{bottomy}) sis ({sis.Min},{sis.Max})");
				var pf = PathHelper.Rectangle(leftx, topy, rightx, bottomy);
				var path = st.NextPath();
				if (path == null) return;
				var pg = new PathGeometry();
				pg.Figures.Add(pf);
				path.Data = pg;
				BindTo(ColumnStack[ix], "PathStyle", path, Path.StyleProperty);
				UpdateLimits(valuex, sis.Min, sis.Max);
				sis.Elements.Add(new Tuple<double, Path>(y1, path));
			}
			st.ms.Add(sis);
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
			ItemState = st.ms;
			Layer.Remove(st.recycler.Unused);
			Layer.Add(st.recycler.Created);
			Dirty = false;
		}
		#endregion
	}
}
