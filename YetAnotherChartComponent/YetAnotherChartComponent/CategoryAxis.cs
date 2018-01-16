using eScape.Core;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region CategoryAxis
	/// <summary>
	/// Horizontal Category axis.
	/// Category axis cells start on the left and extend rightward (in device X-units).
	/// The discrete category axis is a simple "positional-index" axis [0..N-1].  Each index defines a "cell" that allows "normalized" positioning [0..1) within the cell.
	/// Certain series types MAY extend the discrete axis by ONE cell, to draw the "last" elements there.
	/// </summary>
	public class CategoryAxis : AxisCommon, IRequireLayout, IRequireRender, IRequireTransforms, IRequireEnterLeave {
		static LogTools.Flag _trace = LogTools.Add("CategoryAxis", LogTools.Level.Error);
		#region properties
		/// <summary>
		/// Path for axis "bar".
		/// </summary>
		protected Path Axis { get; set; }
		/// <summary>
		/// Axis bar geometry.
		/// </summary>
		protected PathGeometry AxisGeometry { get; set; }
		/// <summary>
		/// Manage labels.
		/// </summary>
		protected Dictionary<int, Tuple<double, string>> LabelMap { get; set; } = new Dictionary<int, Tuple<double, string>>();
		/// <summary>
		/// List of active TextBlocks for labels.
		/// </summary>
		protected List<TextBlock> TickLabels { get; set; }
		/// <summary>
		/// The layer to manage components.
		/// </summary>
		protected IChartLayer Layer { get; set; }
		#endregion
		#region ctor
		/// <summary>
		/// Default ctor.
		/// </summary>
		public CategoryAxis() : base(AxisType.Category, AxisOrientation.Horizontal, Side.Bottom) {
			CommonInit();
		}
		private void CommonInit() {
			TickLabels = new List<TextBlock>();
			Axis = new Path();
			AxisGeometry = new PathGeometry();
			Axis.Data = AxisGeometry;
			MinHeight = 24;
		}
		#endregion
		#region extensions
		/// <summary>
		/// Clear the label map in addition to default impl.
		/// </summary>
		public override void ResetLimits() {
			LabelMap.Clear();
			base.ResetLimits();
		}
		/// <summary>
		/// Labels are cached for presentation.
		/// </summary>
		/// <param name="valueWithLabel"></param>
		/// <returns>base.For(double)</returns>
		public override double For(Tuple<double, string> valueWithLabel) {
			var mv = base.For(valueWithLabel.Item1);
			int key = (int)mv;
			if (LabelMap.ContainsKey(key)) {
				// should be an error but just overwrite it
				LabelMap[key] = valueWithLabel;
			} else {
				LabelMap.Add(key, valueWithLabel);
			}
			return mv;
		}
		/// <summary>
		/// Add elements and attach bindings.
		/// </summary>
		/// <param name="icelc"></param>
		void IRequireEnterLeave.Enter(IChartEnterLeaveContext icelc) {
			Layer = icelc.CreateLayer(Axis);
			BindTo(this, "PathStyle", Axis, Path.StyleProperty);
			ApplyLabelStyle();
			if (PathStyle == null && Theme != null) {
				if (Theme.PathAxisCategory != null) PathStyle = Theme.PathAxisCategory;
			}
		}
		/// <summary>
		/// Reverse effect of Enter.
		/// </summary>
		/// <param name="icelc"></param>
		void IRequireEnterLeave.Leave(IChartEnterLeaveContext icelc) {
			icelc.DeleteLayer(Layer);
			Layer = null;
		}
		/// <summary>
		/// Claim the space indicated by properties.
		/// </summary>
		/// <param name="iclc"></param>
		void IRequireLayout.Layout(IChartLayoutContext iclc) {
			var space = AxisMargin + AxisLineThickness + MinHeight;
			iclc.ClaimSpace(this, Side, space);
		}
		/// <summary>
		/// Compute axis visual elements.
		/// </summary>
		/// <param name="icrc"></param>
		void IRequireRender.Render(IChartRenderContext icrc) {
			if (!Dirty) return;
			_trace.Verbose($"{Name} min:{Minimum} max:{Maximum} r:{Range}");
			AxisGeometry.Figures.Clear();
			//icrc.Remove(TickLabels);
			var pf = PathHelper.Rectangle(Minimum, 0, Maximum, AxisLineThickness);
			AxisGeometry.Figures.Add(pf);
			var i1 = (int)Minimum;
			var i2 = (int)Maximum;
			var scalex = icrc.Area.Width / Range;
			// recycle and lay out tick labels
			var tbr = new Recycler<TextBlock>(TickLabels, () => {
				if (LabelStyle != null) {
					// let style override everything but what MUST be calculated
					var tb = new TextBlock() {
						Width = scalex,
					};
					tb.Style = LabelStyle;
					return tb;
				} else {
					// SHOULD NOT execute this code, unless default style failed!
					var tb = new TextBlock() {
						FontSize = 10,
						Foreground = Axis.Fill,
						VerticalAlignment = VerticalAlignment.Center,
						HorizontalAlignment = HorizontalAlignment.Center,
						Width = scalex,
						TextAlignment = TextAlignment.Center
					};
					return tb;
				}
			});
			var tbget = tbr.Items().GetEnumerator();
			for (var ix = i1; ix <= i2; ix++) {
				if (LabelMap.ContainsKey(ix)) {
					// create a label
					var tpx = LabelMap[ix];
					_trace.Verbose($"key {ix} label {tpx.Item2}");
					if (tbget.MoveNext()) {
						var tb = tbget.Current;
						tb.SetValue(Canvas.LeftProperty, icrc.Area.Left + ix * scalex);
						tb.SetValue(Canvas.TopProperty, icrc.Area.Top + AxisLineThickness + 2 * AxisMargin);
						// cheat: save the grid value so we can rescale the Left in Transforms()
						tb.Tag = tpx;
						tb.Text = tpx.Item2;
					}
				}
			}
			// VT and internal bookkeeping
			Layer.Remove(tbr.Unused);
			Layer.Add(tbr.Created);
			foreach (var tb in tbr.Unused) {
				TickLabels.Remove(tb);
			}
			TickLabels.AddRange(tbr.Created);
			Dirty = false;
		}
		/// <summary>
		/// X-coordinates	axis
		/// Y-coordinates	"px"
		/// Grid-coordinates (x:axis, y:[0..1])
		/// </summary>
		/// <param name="icrc"></param>
		void IRequireTransforms.Transforms(IChartRenderContext icrc) {
			var scalex = icrc.Area.Width / Range;
			var matx = new Matrix(scalex, 0, 0, 1, icrc.Area.Left, icrc.Area.Top + AxisMargin);
			AxisGeometry.Transform = new MatrixTransform() { Matrix = matx };
			_trace.Verbose($"transforms sx:{scalex:F3} matx:{matx} a:{icrc.Area}");
			foreach (var tb in TickLabels) {
				var vx = (Tuple<double, String>)tb.Tag;
				tb.SetValue(Canvas.LeftProperty, icrc.Area.Left + vx.Item1 * scalex);
				tb.SetValue(Canvas.TopProperty, icrc.Area.Top + AxisLineThickness + 2 * AxisMargin);
				tb.Width = scalex;
			}
		}
		#endregion
	}
	#endregion
}
