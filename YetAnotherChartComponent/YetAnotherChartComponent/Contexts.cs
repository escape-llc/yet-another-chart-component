using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace eScapeLLC.UWP.Charts {
	#region DefaultComponentContext
	/// <summary>
	/// Default impl for component context.
	/// </summary>
	public class DefaultComponentContext : IChartComponentContext {
		#region properties
		/// <summary>
		/// The list of components to search for Find().
		/// </summary>
		protected ObservableCollection<ChartComponent> Components { get; set; }
		/// <summary>
		/// The data context in effect.
		/// </summary>
		public object DataContext { get; protected set; }
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="components">The list of components.</param>
		/// <param name="dc">The data context.</param>
		public DefaultComponentContext(ObservableCollection<ChartComponent> components, object dc) {
			Components = components;
			DataContext = dc;
		}
		#endregion
		#region public
		/// <summary>
		/// Search the components list by name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns>!NULL: found; NULL: not found.</returns>
		public ChartComponent Find(string name) {
			return Components.SingleOrDefault((cx) => cx.Name == name);
		}
		#endregion
	}
	#endregion
	#region DefaultRenderContext
	/// <summary>
	/// Default impl for render context.
	/// </summary>
	public class DefaultRenderContext : DefaultComponentContext, IChartRenderContext {
		#region properties
		/// <summary>
		/// The surface.  SHOULD NOT be null.
		/// </summary>
		protected Canvas Surface { get; set; }
		/// <summary>
		/// The overall size of the chart rectangle.
		/// </summary>
		public Size Dimensions { get; protected set; }
		/// <summary>
		/// The area for this component.
		/// </summary>
		public Rect Area { get; protected set; }
		/// <summary>
		/// The remaining area for series.
		/// </summary>
		public Rect SeriesArea { get; protected set; }
		/// <summary>
		/// True: is transforms only; False: full.
		/// </summary>
		public bool IsTransformsOnly { get; set; }
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// Initialize.
		/// </summary>
		/// <param name="surface">The hosting UI.</param>
		/// <param name="components">The list of components.</param>
		/// <param name="sz">Size of chart rectangle.</param>
		/// <param name="rc">The target rectangle.</param>
		/// <param name="sa">The series area rectangle.</param>
		/// <param name="dc">The data context.</param>
		public DefaultRenderContext(Canvas surface, ObservableCollection<ChartComponent> components, Size sz, Rect rc, Rect sa, object dc) : base(components, dc) {
			Surface = surface;
			Dimensions = sz;
			Area = rc;
			SeriesArea = sa;
		}
		#endregion
	}
	#endregion
	#region RenderContextWithErrorInfo
	/// <summary>
	/// Render context version with IChartErrorInfo.
	/// </summary>
	public class RenderContextWithErrorInfo : DefaultRenderContext, IChartErrorInfo {
		#region properties
		/// <summary>
		/// List of collected errors from IChartErrorInfo.
		/// </summary>
		public List<ChartValidationResult> Errors { get; protected set; }
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// Initialize.
		/// </summary>
		/// <param name="surface">The hosting UI.</param>
		/// <param name="components">The list of components.</param>
		/// <param name="sz">Size of chart rectangle.</param>
		/// <param name="rc">The target rectangle.</param>
		/// <param name="sa">The series area rectangle.</param>
		/// <param name="dc">The data context.</param>
		public RenderContextWithErrorInfo(Canvas surface, ObservableCollection<ChartComponent> components, Size sz, Rect rc, Rect sa, object dc) : base(surface, components, sz, rc, sa, dc) {
			Errors = new List<ChartValidationResult>();
		}
		#endregion
		#region IChartErrorInfo
		void IChartErrorInfo.Report(ChartValidationResult cvr) {
			Errors.Add(cvr);
		}
		#endregion
	}
	#endregion
	#region DefaultLayoutCompleteContext
	/// <summary>
	/// Default impl for layout complete context.
	/// </summary>
	public class DefaultLayoutCompleteContext : IChartLayoutCompleteContext {
		#region properties
		/// <summary>
		/// The overall size of the chart rectangle.
		/// </summary>
		public Size Dimensions { get; protected set; }
		/// <summary>
		/// The area for this component.
		/// </summary>
		public Rect Area { get; protected set; }
		/// <summary>
		/// The remaining area for series.
		/// </summary>
		public Rect SeriesArea { get; protected set; }
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="sz">Size of chart rectangle.</param>
		/// <param name="rc">The target rectangle.</param>
		/// <param name="sa">The series area rectangle.</param>
		public DefaultLayoutCompleteContext(Size sz, Rect rc, Rect sa) {
			Dimensions = sz;
			Area = rc;
			SeriesArea = sa;
		}
		#endregion
	}
	#endregion
	#region DefaultDataSourceRenderContext
	/// <summary>
	/// Default implementation for IDataSourceRenderContext.
	/// </summary>
	public class DefaultDataSourceRenderContext : DefaultRenderContext, IDataSourceRenderContext {
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="surface"></param>
		/// <param name="components"></param>
		/// <param name="sz"></param>
		/// <param name="rc"></param>
		/// <param name="sa"></param>
		/// <param name="dc"></param>
		public DefaultDataSourceRenderContext(Canvas surface, ObservableCollection<ChartComponent> components, Size sz, Rect rc, Rect sa, object dc)
		: base(surface, components, sz, rc, sa, dc) {
		}
	}
	#endregion
	#region DefaultEnterLeaveContext
	/// <summary>
	/// Default impl of the enter/leave context.
	/// Also implements IChartErrorInfo.
	/// </summary>
	public class DefaultEnterLeaveContext : DefaultComponentContext, IChartEnterLeaveContext, IChartErrorInfo {
		#region properties
		/// <summary>
		/// The next Z-index to allocate.
		/// </summary>
		public int NextZIndex { get; set; }
		/// <summary>
		/// The list of layers.
		/// </summary>
		protected List<IChartLayer> Layers { get; set; }
		/// <summary>
		/// The surface.  SHOULD NOT be null.
		/// </summary>
		protected Canvas Surface { get; set; }
		/// <summary>
		/// List of collected errors from IChartErrorInfo.
		/// </summary>
		public List<ChartValidationResult> Errors { get; protected set; }
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// Initialize.
		/// </summary>
		/// <param name="surface">The hosting UI.</param>
		/// <param name="components">The list of components.</param>
		/// <param name="layers">The list of layers.</param>
		/// <param name="dc">The data context.</param>
		public DefaultEnterLeaveContext(Canvas surface, ObservableCollection<ChartComponent> components, List<IChartLayer> layers, object dc) : base(components, dc) {
			Surface = surface;
			Layers = layers;
			Errors = new List<ChartValidationResult>();
		}
		#endregion
		#region IChartEnterLeaveContext
		/// <summary>
		/// Add given element to surface.
		/// </summary>
		IChartLayer IChartEnterLeaveContext.CreateLayer() {
			var ccl = new CommonCanvasLayer(Surface, NextZIndex++);
			Layers.Add(ccl);
			return ccl;
		}
		IChartLayer IChartEnterLeaveContext.CreateLayer(params FrameworkElement[] fes) {
			var icl = (this as IChartEnterLeaveContext).CreateLayer();
			icl.Add(fes);
			return icl;
		}
		void IChartEnterLeaveContext.DeleteLayer(IChartLayer icl) {
			icl.Clear();
			Layers.Remove(icl);
		}
		#endregion
		#region IChartErrorInfo
		void IChartErrorInfo.Report(ChartValidationResult cvr) {
			Errors.Add(cvr);
		}
		#endregion
	}
	#endregion
	#region DefaultLayoutContext
	/// <summary>
	/// Default impl of layout context.
	/// </summary>
	public class DefaultLayoutContext : IChartLayoutContext {
		/// <summary>
		/// Overall size of chart rectangle.
		/// </summary>
		public Size Dimensions { get; protected set; }
		/// <summary>
		/// Amount of space remaining after claims.
		/// Gets adjusted after each call to Claim().
		/// </summary>
		public Rect RemainingRect { get; protected set; }
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="sz"></param>
		/// <param name="rc"></param>
		public DefaultLayoutContext(Size sz, Rect rc) { Dimensions = sz; RemainingRect = rc; }
		IDictionary<ChartComponent, Rect> ClaimedRects { get; set; } = new Dictionary<ChartComponent, Rect>();
		/// <summary>
		/// Return the rect mapped to this component, else RemainingRect.
		/// </summary>
		/// <param name="cc"></param>
		/// <returns></returns>
		public Rect For(ChartComponent cc) { return ClaimedRects.ContainsKey(cc) ? ClaimedRects[cc] : RemainingRect; }
		/// <summary>
		/// Trim axis rectangles to be "flush" with the RemainingRect.
		/// </summary>
		public void FinalizeRects() {
			var tx = new Dictionary<ChartComponent, Rect>();
			foreach (var kv in ClaimedRects) {
				if (kv.Key is IChartAxis) {
					var ica = kv.Key as IChartAxis;
					switch (ica.Orientation) {
					case AxisOrientation.Horizontal:
						switch (ica.Side) {
						case Side.Bottom:
							tx.Add(kv.Key, new Rect(RemainingRect.Left, kv.Value.Top, RemainingRect.Width, kv.Value.Height));
							break;
						case Side.Top:
							// TODO fix
							tx.Add(kv.Key, new Rect(RemainingRect.Left, kv.Value.Top, RemainingRect.Width, kv.Value.Height));
							break;
						}
						break;
					case AxisOrientation.Vertical:
						switch (ica.Side) {
						case Side.Right:
							tx.Add(kv.Key, new Rect(kv.Value.Left, kv.Value.Top, kv.Value.Width, RemainingRect.Height));
							break;
						case Side.Left:
							// TODO fix
							tx.Add(kv.Key, new Rect(kv.Value.Left, kv.Value.Top, kv.Value.Width, RemainingRect.Height));
							break;
						}
						break;
					}
				}
			}
			// apply dictionary updates
			foreach (var kv in tx) {
				ClaimedRects[kv.Key] = kv.Value;
			}
		}
		/// <summary>
		/// Claim the indicated space for given component.
		/// </summary>
		/// <param name="cc"></param>
		/// <param name="sd"></param>
		/// <param name="amt"></param>
		/// <returns></returns>
		public Rect ClaimSpace(ChartComponent cc, Side sd, double amt) {
			var ul = new Point();
			var sz = new Size();
			switch (sd) {
			case Side.Top:
				ul.X = RemainingRect.Left;
				ul.Y = RemainingRect.Top;
				sz.Width = Dimensions.Width;
				sz.Height = amt;
				RemainingRect = new Rect(RemainingRect.Left, RemainingRect.Top + amt, RemainingRect.Width, RemainingRect.Height - amt);
				break;
			case Side.Right:
				ul.X = RemainingRect.Right - amt;
				ul.Y = RemainingRect.Top;
				sz.Width = amt;
				sz.Height = Dimensions.Height;
				RemainingRect = new Rect(RemainingRect.Left, RemainingRect.Top, RemainingRect.Width - amt, RemainingRect.Height);
				break;
			case Side.Bottom:
				ul.X = RemainingRect.Left;
				ul.Y = RemainingRect.Bottom - amt;
				sz.Width = Dimensions.Width;
				sz.Height = amt;
				RemainingRect = new Rect(RemainingRect.Left, RemainingRect.Top, RemainingRect.Width, RemainingRect.Height - amt);
				break;
			case Side.Left:
				ul.X = RemainingRect.Left;
				ul.Y = RemainingRect.Top;
				sz.Width = amt;
				sz.Height = Dimensions.Height;
				RemainingRect = new Rect(RemainingRect.Left + amt, RemainingRect.Top, RemainingRect.Width - amt, RemainingRect.Height);
				break;
			}
			var rect = new Rect(ul, sz);
			ClaimedRects.Add(cc, rect);
			return rect;
		}
	}
	#endregion
}
