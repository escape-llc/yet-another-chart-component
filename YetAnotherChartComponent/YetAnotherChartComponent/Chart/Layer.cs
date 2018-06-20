﻿using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace eScapeLLC.UWP.Charts {
	#region layer implementations
	#region CanvasLayerCore
	/// <summary>
	/// Base implementation for <see cref="IChartLayer"/> that uses a <see cref="Canvas"/>.
	/// </summary>
	public abstract class CanvasLayerCore : IChartLayer {
		#region data
		/// <summary>
		/// Access to the <see cref="Canvas"/>.
		/// </summary>
		protected readonly Canvas canvas;
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="canvas">Target canvas.</param>
		public CanvasLayerCore(Canvas canvas) {
			this.canvas = canvas;
		}
		#endregion
		#region extension points
		/// <summary>
		/// Add element with assign z-index.
		/// </summary>
		/// <param name="fe"></param>
		protected virtual void InternalAdd(FrameworkElement fe) {
			if((this as IChartLayer).UseImplicitAnimations) {
				UniversalApiContract.v3.CompositionSupport.AttachAnimations(fe, 1000);
				UniversalApiContract.v4.CompositionSupport.AttachAnimations(fe);
			}
			canvas.Children.Add(fe);
		}
		/// <summary>
		/// Add elements with assign z-index.
		/// </summary>
		/// <param name="fes"></param>
		protected virtual void InternalAdd(IEnumerable<FrameworkElement> fes) { foreach (var fe in fes) { (this as IChartLayer).Add(fe); } }
		/// <summary>
		/// Set Canvas layout properties on the source canvas.
		/// </summary>
		/// <param name="target">Location in PX.</param>
		protected abstract void InternalLayout(Rect target);
		/// <summary>
		/// Remove element and detach animations.
		/// </summary>
		/// <param name="fe"></param>
		protected virtual void InternalRemove(FrameworkElement fe) {
			canvas.Children.Remove(fe);
			PostRemove(fe);
		}
		/// <summary>
		/// Any actions to perform as element leaves VT.
		/// Called from <see cref="InternalClear"/> and <see cref="InternalRemove(FrameworkElement)"/>.
		/// </summary>
		/// <param name="fe"></param>
		protected virtual void PostRemove(FrameworkElement fe) {
			UniversalApiContract.v4.CompositionSupport.DetachAnimations(fe);
			UniversalApiContract.v3.CompositionSupport.DetachAnimations(fe);
		}
		/// <summary>
		/// Remove elements.
		/// </summary>
		/// <param name="fes"></param>
		protected virtual void InternalRemove(IEnumerable<FrameworkElement> fes) { foreach (var fe in fes) (this as IChartLayer).Remove(fe); }
		/// <summary>
		/// Remove all children.
		/// </summary>
		protected virtual void InternalClear() {
			try {
				foreach (var fe in canvas.Children) {
					if (fe is FrameworkElement fe2) {
						// NOTE this MAY be a problem if PostRemove requires FE2 be really NOT IN Visual Tree!
						PostRemove(fe2);
					}
				}
			} finally {
				canvas.Children.Clear();
			}
		}
		#endregion
		#region IChartLayer
		bool IChartLayer.UseImplicitAnimations { get; set; }
		void IChartLayer.Add(FrameworkElement fe) { InternalAdd(fe); }
		void IChartLayer.Add(IEnumerable<FrameworkElement> fes) { InternalAdd(fes); }
		void IChartLayer.Layout(Rect target) { InternalLayout(target); }
		void IChartLayer.Remove(FrameworkElement fe) { InternalRemove(fe); }
		void IChartLayer.Remove(IEnumerable<FrameworkElement> fes) { InternalRemove(fes); }
		void IChartLayer.Clear() { InternalClear(); }
		#endregion
	}
	#endregion
	#region CommonCanvasLayer
	/// <summary>
	/// Layer where all layers share a common <see cref="Canvas"/>.
	/// Because of the sharing, this implementation tracks its "own" elements separately from <see cref="Panel.Children"/>.
	/// </summary>
	public class CommonCanvasLayer : CanvasLayerCore {
		#region data
		readonly int zindex;
		readonly List<FrameworkElement> elements;
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="canvas">Target canvas (shared).</param>
		/// <param name="zindex">Z-index to assign to elements.</param>
		public CommonCanvasLayer(Canvas canvas, int zindex) :base(canvas) {
			this.zindex = zindex;
			this.elements = new List<FrameworkElement>();
		}
		#endregion
		#region IChartLayer
		/// <summary>
		/// Add element with assigned z-index.
		/// </summary>
		/// <param name="fe"></param>
		protected override void InternalAdd(FrameworkElement fe) {
			fe.SetValue(Canvas.ZIndexProperty, zindex);
			base.InternalAdd(fe);
			elements.Add(fe);
		}
		/// <summary>
		/// Do Not Respond, because one canvas owns everything.
		/// </summary>
		/// <param name="target"></param>
		protected override void InternalLayout(Rect target) { }
		/// <summary>
		/// Do reverse bookkeeping.
		/// </summary>
		/// <param name="fe"></param>
		protected override void PostRemove(FrameworkElement fe) {
			elements.Remove(fe);
			base.PostRemove(fe);
		}
		#endregion
	}
	#endregion
	#region CanvasLayer
	/// <summary>
	/// Layer where each layer is bound to a different <see cref="Canvas"/> (COULD be <see cref="IPanel"/>).
	/// This implementation relies on <see cref="Panel.Children"/> to track the elements.
	/// </summary>
	public class CanvasLayer : CanvasLayerCore {
		#region data
		readonly int zindex;
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="canvas">Target canvas.</param>
		/// <param name="zindex">Z-index to assign to this canvas.</param>
		public CanvasLayer(Canvas canvas, int zindex) : base(canvas) {
			this.zindex = zindex;
			canvas.SetValue(Canvas.ZIndexProperty, zindex);
		}
		#endregion
		#region extensions
		/// <summary>
		/// Set Canvas layout properties on the source canvas.
		/// </summary>
		/// <param name="target">Location in PX.</param>
		protected override void InternalLayout(Rect target) {
			canvas.SetValue(Canvas.TopProperty, target.Top);
			canvas.SetValue(Canvas.LeftProperty, target.Left);
			canvas.SetValue(FrameworkElement.WidthProperty, target.Width);
			canvas.SetValue(FrameworkElement.HeightProperty, target.Height);
		}
		#endregion
	}
	#endregion
	#endregion
}
