using eScape.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region ValueAxisGrid
	/// <summary>
	/// Grid lines for the value axis.
	/// </summary>
	public class ValueAxisGrid : ChartComponent, IRequireChartTheme, IRequireEnterLeave, IRequireRender, IRequireTransforms {
		static readonly LogTools.Flag _trace = LogTools.Add("ValueAxisGrid", LogTools.Level.Error);
		#region ItemState
		/// <summary>
		/// Internal state for generated grid lines.
		/// </summary>
		protected class ItemState {
			internal TickState tick { get; set; }
			internal Path element { get; set; }
			internal void SetLocation(double left, double top) {
				element.SetValue(Canvas.LeftProperty, left);
				element.SetValue(Canvas.TopProperty, top);
			}
		}
		#endregion
		#region properties
		/// <summary>
		/// The style to use for Path geometry.
		/// </summary>
		public Style PathStyle { get { return (Style)GetValue(PathStyleProperty); } set { SetValue(PathStyleProperty, value); } }
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
		/// Converter to use as the element <see cref="FrameworkElement.Style"/> selector.
		/// These are already set to their "standard" values before this is called, so it MAY selectively opt out of setting them.
		/// <para/>
		/// The <see cref="IValueConverter.Convert"/> targetType parameter is used to determine which value is requested.
		/// <para/>
		/// Uses <see cref="Tuple{Style,String}"/> for style/label override.  Return a new instance/NULL to opt in/out.  the String portion is NOT USED.
		/// </summary>
		public IValueConverter PathFormatter { get; set; }
		/// <summary>
		/// The dereferenced value axis.
		/// </summary>
		protected IChartAxis ValueAxis { get; set; }
		/// <summary>
		/// Paths for the grid lines.
		/// </summary>
		protected List<ItemState> GridLines { get; set; }
		/// <summary>
		/// The layer for components.
		/// </summary>
		protected IChartLayer Layer { get; set; }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="PathStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty PathStyleProperty = DependencyProperty.Register(nameof(PathStyle), typeof(Style), typeof(ValueAxisGrid), new PropertyMetadata(null));
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// Initialize geometry and path.
		/// </summary>
		public ValueAxisGrid() {
			//Grid = new Path();
			//GridGeometry = new GeometryGroup();
			//Grid.Data = GridGeometry;
			GridLines = new List<ItemState>();
		}
		#endregion
		#region helpers
		/// <summary>
		/// Dereference the ValueAxisName.
		/// </summary>
		/// <param name="iccc"></param>
		void EnsureAxes(IChartComponentContext iccc) {
			if (ValueAxis == null && !String.IsNullOrEmpty(ValueAxisName)) {
				ValueAxis = iccc.Find(ValueAxisName) as IChartAxis;
			} else {
				if (iccc is IChartErrorInfo icei) {
					icei.Report(new ChartValidationResult(NameOrType(), $"Value axis '{ValueAxisName}' was not found", new[] { nameof(ValueAxis), nameof(ValueAxisName) }));
				}
			}
		}
		/// <summary>
		/// Apply bindings to internal elements.
		/// </summary>
		/// <param name="icelc"></param>
		void DoBindings(IChartEnterLeaveContext icelc) {
			AssignFromRef(icelc as IChartErrorInfo, NameOrType(), nameof(PathStyle), nameof(Theme.PathGridValue),
				PathStyle == null, Theme != null, Theme.PathGridValue != null,
				() => PathStyle = Theme.PathGridValue
			);
		}
		/// <summary>
		/// Coordinates are strict NDC.  XY-offsets are delegated to <see cref="Canvas"/>.
		/// </summary>
		/// <param name="state"></param>
		/// <returns></returns>
		Path CreateElement(ItemState state) {
			var path = new Path();
			var gline = new LineGeometry() { StartPoint=new Point(0, 0), EndPoint=new Point(1, 0) };
			path.Data = gline;
			if (PathStyle != null) {
				BindTo(this, nameof(PathStyle), path, FrameworkElement.StyleProperty);
			}
			return path;
		}
		#endregion
		#region extensions
		void IRequireEnterLeave.Enter(IChartEnterLeaveContext icelc) {
			EnsureAxes(icelc as IChartComponentContext);
			Layer = icelc.CreateLayer(/*Grid*/);
			DoBindings(icelc);
		}
		void IRequireEnterLeave.Leave(IChartEnterLeaveContext icelc) {
			ValueAxis = null;
			icelc.DeleteLayer(Layer);
			Layer = null;
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
			var recycler = new Recycler<Path, ItemState>(GridLines.Where(tl => tl.element != null).Select(tl => tl.element), (ist) => {
				return CreateElement(ist);
			});
			var itemstate = new List<ItemState>();
			//_trace.Verbose($"grid range:{tc.Range} tintv:{tc.TickInterval}");
			var tixarray = tc.GetTicks().ToArray();
			var sc = new ValueAxisSelectorContext(ValueAxis, icrc.SeriesArea, tixarray, tc.TickInterval);
			for (int ix = 0; ix < tixarray.Length; ix++) {
				var tick = tixarray[ix];
				//_trace.Verbose($"grid vx:{tick}");
				var state = new ItemState() { tick = tick };
				var current = recycler.Next(state);
				if(!current.Item1) {
					// restore binding
					if (PathStyle != null) {
						BindTo(this, nameof(PathStyle), current.Item2, FrameworkElement.StyleProperty);
					}
				}
				state.element = current.Item2;
				ApplyBinding(this, nameof(Visibility), state.element, UIElement.VisibilityProperty);
				if (PathFormatter != null) {
					sc.SetTick(ix);
					// call for Style, String override
					var format = PathFormatter.Convert(sc, typeof(Tuple<Style, String>), null, System.Globalization.CultureInfo.CurrentUICulture.Name);
					if (format is Tuple<Style, String> ovx) {
						if (ovx.Item1 != null) {
							current.Item2.Style = ovx.Item1;
						}
					}
				}
				state.SetLocation(icrc.Area.Left, tick.Value);
				sc.Generated(tick);
				itemstate.Add(state);
			}
			// VT and internal bookkeeping
			GridLines = itemstate;
			Layer.Remove(recycler.Unused);
			Layer.Add(recycler.Created);
			Dirty = false;
		}
		/// <summary>
		/// Grid-coordinates (x:[0..1], y:axis)
		/// </summary>
		/// <param name="icrc"></param>
		void IRequireTransforms.Transforms(IChartRenderContext icrc) {
			if (ValueAxis == null) return;
			var scalex = icrc.SeriesArea.Width;
			var scaley = icrc.SeriesArea.Height / ValueAxis.Range;
			var gmatx = new Matrix(scalex, 0, 0, -scaley, 0, 0);
			var gmt = new MatrixTransform() { Matrix = gmatx };
			_trace.Verbose($"transforms sx:{scalex:F3} sy:{scaley:F3} matx:{gmatx} a:{icrc.Area} sa:{icrc.SeriesArea}");
			foreach (var state in GridLines) {
				var adj = state.element.ActualHeight / 2;
				var top = icrc.Area.Bottom - (state.tick.Value - ValueAxis.Minimum) * scaley - adj;
				_trace.Verbose($"transforms tick:{state.tick.Index} adj:{adj} top:{top}");
				state.element.Data.Transform = gmt;
				state.SetLocation(icrc.SeriesArea.Left, top);
			}
		}
		#endregion
	}
	#endregion
}
