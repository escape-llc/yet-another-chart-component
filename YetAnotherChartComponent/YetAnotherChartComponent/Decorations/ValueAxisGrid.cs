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
			internal TickState Tick { get; set; }
			internal Path Element { get; set; }
			internal void SetLocation(double left, double top) {
				Element.SetValue(Canvas.LeftProperty, left);
				Element.SetValue(Canvas.TopProperty, top);
			}
		}
		#endregion
		#region properties
		/// <summary>
		/// The style to use for Path geometry.
		/// </summary>
		public Style PathStyle { get { return (Style)GetValue(PathStyleProperty); } set { SetValue(PathStyleProperty, value); } }
		/// <summary>
		/// The style for minor grid Path geometry.
		/// </summary>
		public Style MinorGridPathStyle { get { return (Style)GetValue(MinorGridPathStyleProperty); } set { SetValue(MinorGridPathStyleProperty, value); } }
		/// <summary>
		/// If GT Zero, the number of minor grid lines.
		/// NOTE: this is One Less than the actual subdivision "fraction" e.g. MinorGridLineCount == 1 divides by 2!
		/// </summary>
		public int MinorGridLineCount { get { return (int)GetValue(MinorGridLineCountProperty); } set { SetValue(MinorGridLineCountProperty, value); } }
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
		/// Paths for the minor grid lines.
		/// </summary>
		protected List<ItemState> MinorGridLines { get; set; }
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
		/// <summary>
		/// Identifies <see cref="MinorGridPathStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty MinorGridPathStyleProperty = DependencyProperty.Register(nameof(MinorGridPathStyle), typeof(Style), typeof(ValueAxisGrid), new PropertyMetadata(null));
		/// <summary>
		/// Identifies <see cref="MinorGridLineCount"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty MinorGridLineCountProperty = DependencyProperty.Register(nameof(MinorGridLineCount), typeof(uint), typeof(ValueAxisGrid), new PropertyMetadata(null));
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// Initialize geometry and path.
		/// </summary>
		public ValueAxisGrid() {
			GridLines = new List<ItemState>();
			MinorGridLines = new List<ItemState>();
			MinorGridLineCount = 0;
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
		Path CreateSubElement(ItemState state) {
			var path = new Path();
			var gline = new LineGeometry() { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
			path.Data = gline;
			if (MinorGridPathStyle != null) {
				BindTo(this, nameof(MinorGridPathStyle), path, FrameworkElement.StyleProperty);
			} else if (PathStyle != null) {
				BindTo(this, nameof(PathStyle), path, FrameworkElement.StyleProperty);
			}
			return path;
		}
		#endregion
		#region extensions
		void IRequireEnterLeave.Enter(IChartEnterLeaveContext icelc) {
			EnsureAxes(icelc as IChartComponentContext);
			Layer = icelc.CreateLayer();
			DoBindings(icelc);
		}
		void IRequireEnterLeave.Leave(IChartEnterLeaveContext icelc) {
			ValueAxis = null;
			icelc.DeleteLayer(Layer);
			Layer = null;
		}
		internal class SubtickState : TickState {
			internal int ParentIndex { get; set; }
			internal SubtickState(int pidx, int idx, double vx) :base(idx, vx) { ParentIndex = pidx; }
		}
		ItemState CreateSubtick(IChartRenderContext icrc, Recycler<Path, ItemState> recycler, SubtickState tick) {
			if (tick.Value <= ValueAxis.Minimum || tick.Value >= ValueAxis.Maximum) return null;
			var state = new ItemState() { Tick = tick };
			var current = recycler.Next(state);
			if (!current.Item1) {
				// restore binding
				if (MinorGridPathStyle != null) {
					BindTo(this, nameof(MinorGridPathStyle), current.Item2, FrameworkElement.StyleProperty);
				} else if (PathStyle != null) {
					BindTo(this, nameof(PathStyle), current.Item2, FrameworkElement.StyleProperty);
				}
			}
			state.Element = current.Item2;
			ApplyBinding(this, nameof(Visibility), state.Element, UIElement.VisibilityProperty);
			state.SetLocation(icrc.Area.Left, tick.Value);
			return state;
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
			var recycler = new Recycler<Path, ItemState>(GridLines.Where(tl => tl.Element != null).Select(tl => tl.Element), (ist) => {
				return CreateElement(ist);
			});
			var mrecycler = new Recycler<Path, ItemState>(MinorGridLines.Where(tl => tl.Element != null).Select(tl => tl.Element), (ist) => {
				return CreateSubElement(ist);
			});
			var itemstate = new List<ItemState>();
			var mitemstate = new List<ItemState>();
			//_trace.Verbose($"grid range:{tc.Range} tintv:{tc.TickInterval}");
			var tixarray = tc.GetTicks().ToArray();
			var sc = new ValueAxisSelectorContext(ValueAxis, icrc.SeriesArea, tixarray, tc.TickInterval);
			for (int ix = 0; ix < tixarray.Length; ix++) {
				var tick = tixarray[ix];
				//_trace.Verbose($"grid vx:{tick}");
				var state = new ItemState() { Tick = tick };
				var current = recycler.Next(state);
				if(!current.Item1) {
					// restore binding
					if (PathStyle != null) {
						BindTo(this, nameof(PathStyle), current.Item2, FrameworkElement.StyleProperty);
					}
				}
				state.Element = current.Item2;
				ApplyBinding(this, nameof(Visibility), state.Element, UIElement.VisibilityProperty);
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
				if (MinorGridLineCount != 0) {
					// lay out minor divisions
					var mintv = tc.TickInterval / (double)(MinorGridLineCount + 1);
					var diry = Math.Sign(tick.Index);
					if (diry == 0) {
						// special case: zero
						for (int mx = 1; mx <= MinorGridLineCount; mx++) {
							var mtick = new SubtickState(tick.Index, mx, tick.Value + (double)mx * mintv);
							var mstate = CreateSubtick(icrc, mrecycler, mtick);
							if (mstate != null) {
								mitemstate.Add(mstate);
							}
							var mtick2 = new SubtickState(tick.Index, -mx, tick.Value + (double)(-mx) * mintv);
							var mstate2 = CreateSubtick(icrc, mrecycler, mtick2);
							if (mstate2 != null) {
								mitemstate.Add(mstate2);
							}
						}
					} else {
						for (int mx = 1; mx <= MinorGridLineCount; mx++) {
							var mtick = new SubtickState(tick.Index, diry*mx, tick.Value + (double)(diry * mx) * mintv);
							var mstate = CreateSubtick(icrc, mrecycler, mtick);
							if (mstate != null) {
								mitemstate.Add(mstate);
							}
						}
					}
				}
			}
			// VT and internal bookkeeping
			MinorGridLines = mitemstate;
			GridLines = itemstate;
			Layer.Remove(mrecycler.Unused);
			Layer.Remove(recycler.Unused);
			Layer.Add(recycler.Created);
			Layer.Add(mrecycler.Created);
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
				var adj = state.Element.ActualHeight / 2;
				var top = icrc.Area.Bottom - (state.Tick.Value - ValueAxis.Minimum) * scaley - adj;
				_trace.Verbose($"transforms tick:{state.Tick.Index} adj:{adj} top:{top}");
				state.Element.Data.Transform = gmt;
				state.SetLocation(icrc.SeriesArea.Left, top);
			}
			foreach(var state in MinorGridLines) {
				var adj = state.Element.ActualHeight / 2;
				var top = icrc.Area.Bottom - (state.Tick.Value - ValueAxis.Minimum) * scaley - adj;
				_trace.Verbose($"transforms subtick:{state.Tick.Index} adj:{adj} top:{top}");
				state.Element.Data.Transform = gmt;
				state.SetLocation(icrc.SeriesArea.Left, top);
			}
		}
		#endregion
	}
	#endregion
}
