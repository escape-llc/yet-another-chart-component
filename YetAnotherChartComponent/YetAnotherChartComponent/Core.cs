﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region AxisType
	/// <summary>
	/// Allowed axis types.
	/// </summary>
	public enum AxisType {
		/// <summary>
		/// X-axis value.
		/// </summary>
		Category,
		/// <summary>
		/// Y-axis value.
		/// </summary>
		Value
	};
	#endregion
	#region AxisOrientation
	/// <summary>
	/// Allowed axis orientations.
	/// </summary>
	public enum AxisOrientation {
		/// <summary>
		/// Horizontal orientation.
		/// </summary>
		Horizontal,
		/// <summary>
		/// Vertical orientation.
		/// </summary>
		Vertical
	};
	#endregion
	#region AxisVisibility
	/// <summary>
	/// Axis visibility.
	/// </summary>
	public enum AxisVisibility {
		/// <summary>
		/// Visible and taking up layout space.
		/// </summary>
		Visible,
		/// <summary>
		/// Not visible and taking up layout space.
		/// </summary>
		Hidden,
		/// <summary>
		/// Not visible not taking up layout space.
		/// </summary>
		Collapsed
	};
	#endregion
	#region Side
	/// <summary>
	/// Side to claim space from.
	/// </summary>
	public enum Side {
		/// <summary>
		/// Top.
		/// </summary>
		Top,
		/// <summary>
		/// Right.
		/// </summary>
		Right,
		/// <summary>
		/// Bottom.
		/// </summary>
		Bottom,
		/// <summary>
		/// Left.
		/// </summary>
		Left,
		/// <summary>
		/// No fixed side, no space claimed.
		/// </summary>
		Float
	};
	#endregion
	#region IChartAxis
	/// <summary>
	/// Features for axes.
	/// Axes must be present in the component list, to provide the infrastructure for scaling data series,
	/// even if they will not display.
	/// </summary>
	public interface IChartAxis {
		/// <summary>
		/// The axis type.
		/// </summary>
		AxisType Type { get; }
		/// <summary>
		/// The axis orientation.
		/// Typically Horizontal for Category and Vertical for Value.
		/// </summary>
		AxisOrientation Orientation { get; }
		/// <summary>
		/// The side of the data area this axis attaches to.
		/// Typically Bottom for Category and Right for Value.
		/// </summary>
		Side Side { get; }
		/// <summary>
		/// Minimum value or NaN.
		/// </summary>
		double Minimum { get; }
		/// <summary>
		/// Maximum value or NaN.
		/// </summary>
		double Maximum { get; }
		/// <summary>
		/// Range or NaN.
		/// </summary>
		double Range { get; }
		/// <summary>
		/// Reset the limits so axis can re-calculate.
		/// </summary>
		void ResetLimits();
		/// <summary>
		/// Do bookkeeping for updating limits/range.
		/// </summary>
		/// <param name="value">The value.</param>
		void UpdateLimits(double value);
		/// <summary>
		/// Map the value.
		/// </summary>
		/// <param name="value">Input (actual) value.</param>
		/// <returns>Axis-mapped value.</returns>
		double For(double value);
		/// <summary>
		/// Map the value with label.
		/// </summary>
		/// <param name="valueWithLabel">Value with its label.</param>
		/// <returns>Axis-mapped value.</returns>
		double For(Tuple<double, String> valueWithLabel);
		/// <summary>
		/// Return the "scale" for this axis.
		/// </summary>
		/// <param name="dimension">Overall Dimension (in DC).</param>
		/// <returns>Dimension / Range.</returns>
		double ScaleFor(double dimension);
	}
	#endregion
	#region IChartLayer
	/// <summary>
	/// Represents a container for chart component visual elements.
	/// </summary>
	public interface IChartLayer {
		/// <summary>
		/// Add content.
		/// </summary>
		/// <param name="fe">Element to add.</param>
		void Add(FrameworkElement fe);
		/// <summary>
		/// Remove content.
		/// </summary>
		/// <param name="fe">Element to remove.</param>
		void Remove(FrameworkElement fe);
		/// <summary>
		/// Add group of elements.
		/// </summary>
		/// <param name="fes"></param>
		void Add(IEnumerable<FrameworkElement> fes);
		/// <summary>
		/// Remove group of elements.
		/// </summary>
		/// <param name="fes"></param>
		void Remove(IEnumerable<FrameworkElement> fes);
		/// <summary>
		/// Position the layer.
		/// </summary>
		/// <param name="target"></param>
		void Layout(Rect target);
		/// <summary>
		/// Remove all the components this layer knows about.
		/// </summary>
		void Clear();
	}
	#endregion
	#region IChartLayoutContext
	/// <summary>
	/// The context for <see cref="IRequireLayout"/> interface.
	/// </summary>
	public interface IChartLayoutContext {
		/// <summary>
		/// Overall dimensions.
		/// </summary>
		Size Dimensions { get; }
		/// <summary>
		/// Space remaining after claims.
		/// This rectangle is passed to all components via <see cref="IChartRenderContext.SeriesArea"/>.
		/// </summary>
		Rect RemainingRect { get; }
		/// <summary>
		/// Subtract space from RemainingRect and register that rectangle for given component.
		/// Returns the allocated rectangle.
		/// The claimed rectangle is passed back to this component via IChartRenderContext.Area.
		/// </summary>
		/// <param name="cc">Component key.</param>
		/// <param name="sd">Side to allocate from.</param>
		/// <param name="amt">Amount.  Refers to Height:Top/Bottom and Width:Left/Right.  Alternate dimension comes from the Dimensions property.</param>
		/// <returns>Allocated and registered rectangle.</returns>
		Rect ClaimSpace(ChartComponent cc, Side sd, double amt);
	}
	#endregion
	#region IChartLayoutCompleteContext
	/// <summary>
	/// Context interface for <see cref="IRequireLayoutComplete"/>.
	/// </summary>
	public interface IChartLayoutCompleteContext {
		/// <summary>
		/// Overall dimensions.
		/// </summary>
		Size Dimensions { get; }
		/// <summary>
		/// Space remaining after claims.
		/// This rectangle is passed to all components via <see cref="IChartRenderContext.SeriesArea"/>.
		/// </summary>
		Rect SeriesArea { get; }
		/// <summary>
		/// Space for this component.
		/// If no space was claimed, equal to <see cref="SeriesArea"/>.
		/// </summary>
		Rect Area { get; }
	}
	#endregion
	#region ChartValidationResult
	/// <summary>
	/// Use internally to report errors to the chart "owner".
	/// </summary>
	public class ChartValidationResult : ValidationResult {
		/// <summary>
		/// Source of the error: chart, series, axis, etc.
		/// MAY be the name of a component.
		/// MAY be the Type of an unnamed component.
		/// </summary>
		public String Source { get; private set; }
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="errorMessage"></param>
		public ChartValidationResult(string source, string errorMessage) : base(errorMessage) { Source = source; }
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="errorMessage"></param>
		/// <param name="memberNames"></param>
		public ChartValidationResult(string source, string errorMessage, IEnumerable<string> memberNames) : base(errorMessage, memberNames) { Source = source; }
	}
	#endregion
	#region IChartErrorInfo
	/// <summary>
	/// Ability to accept (and forward) error reports.
	/// Reports MAY be buffered by the context for later delivery.
	/// </summary>
	public interface IChartErrorInfo {
		/// <summary>
		/// Report an error, to aid configuration troubleshooting.
		/// </summary>
		/// <param name="cvr">The error.</param>
		void Report(ChartValidationResult cvr);
	}
	#endregion
	#region IChartComponentContext
	/// <summary>
	/// General component context.
	/// </summary>
	public interface IChartComponentContext {
		/// <summary>
		/// The data context object.
		/// </summary>
		object DataContext { get; }
		/// <summary>
		/// Look up a component by name.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <returns>Matching component or NULL.</returns>
		ChartComponent Find(String name);
	}
	#endregion
	#region IChartRenderContext
	/// <summary>
	/// The context for <see cref="IRequireRender"/> and <see cref="IRequireTransforms"/> interfaces.
	/// MAY also implement <see cref="IChartErrorInfo"/>.
	/// MAY also implement <see cref="IChartComponentContext"/>.
	/// </summary>
	public interface IChartRenderContext {
		/// <summary>
		/// Current overall dimensions.
		/// </summary>
		Size Dimensions { get; }
		/// <summary>
		/// The area to render this component in.
		/// </summary>
		Rect Area { get; }
		/// <summary>
		/// The area where series are displayed.
		/// </summary>
		Rect SeriesArea { get; }
		/// <summary>
		/// Whether this pass is transforms-only.
		/// </summary>
		bool IsTransformsOnly { get; }
	}
	#endregion
	#region IChartEnterLeaveContext
	/// <summary>
	/// The context for <see cref="IRequireEnterLeave"/> interface.
	/// SHOULD also implement <see cref="IChartErrorInfo"/>.
	/// SHOULD also implement <see cref="IChartComponentContext"/>.
	/// </summary>
	public interface IChartEnterLeaveContext {
		/// <summary>
		/// Create a layer.
		/// </summary>
		/// <returns></returns>
		IChartLayer CreateLayer();
		/// <summary>
		/// Create a layer with given initial components.
		/// </summary>
		/// <param name="fes">Initial components.</param>
		/// <returns></returns>
		IChartLayer CreateLayer(params FrameworkElement[] fes);
		/// <summary>
		/// Delete given layer.
		/// This in turn deletes all the components within the layer being tracked.
		/// </summary>
		/// <param name="icl"></param>
		void DeleteLayer(IChartLayer icl);
	}
	#endregion
	#region IRequireLayout
	/// <summary>
	/// Require participation in layout pass.
	/// </summary>
	public interface IRequireLayout {
		/// <summary>
		/// Claim layout space before rendering begins.
		/// </summary>
		/// <param name="iclc">The context.</param>
		void Layout(IChartLayoutContext iclc);
	}
	/// <summary>
	/// Require callback when layout has completed.
	/// </summary>
	public interface IRequireLayoutComplete {
		/// <summary>
		/// The layout is complete; here is your info.
		/// </summary>
		/// <param name="iclcc">Layout results context.</param>
		void LayoutComplete(IChartLayoutCompleteContext iclcc);
	}
	#endregion
	#region IRequireEnterLeave
	/// <summary>
	/// Require component lifecycle.
	/// </summary>
	public interface IRequireEnterLeave {
		/// <summary>
		/// Component is entering the chart.
		/// Opportunity to add objects to the Visual Tree, then obtain/transfer bindings to those objects from the component's DPs.
		/// Framework makes an effort to defer this call until the VT is available.
		/// Example: components included directly in XAML via Chart.Components.
		/// </summary>
		/// <param name="icelc">The context.</param>
		void Enter(IChartEnterLeaveContext icelc);
		/// <summary>
		/// Component is leaving the chart.
		/// Opportunity to remove objects from Visual Tree etc. the dual of Enter().
		/// </summary>
		/// <param name="icelc">The context.</param>
		void Leave(IChartEnterLeaveContext icelc);
	}
	#endregion
	#region IRequireRender
	/// <summary>
	/// Require rendering pass.
	/// Generate coordinates that DO NOT depend on axis limits being finalized.
	/// MUST use this interface if NOT using <see cref="IDataSourceRenderer"/> or <see cref="IProvideDataSourceRenderer"/>.
	/// MAY also implement <see cref="IRequireAfterAxesFinalized"/>
	/// SHOULD also implement <see cref="IRequireTransforms"/>.
	/// </summary>
	public interface IRequireRender {
		/// <summary>
		/// Render the component.
		/// This is where data MUST be processed and Geometry etc. built.
		/// Non-geomerty drawing attributes MAY be configured here, but SHOULD have been arranged in Enter.
		/// Geometry coordinates MUST be represented in layout-invariant coordinates!
		/// This means when the layout rectangle size changes, only the GeometryTransform is adjusted (in ChartComponent.Transforms); no data is re-calculated.
		/// </summary>
		/// <param name="icrc">The context.</param>
		void Render(IChartRenderContext icrc);
	}
	#endregion
	#region IRequireAfterAxesFinalized
	/// <summary>
	/// Requirement for callback after axis limits are finalized.
	/// This is the earliest opportunity to access the <see cref="IChartAxis"/> Minimum/Maximum/Range properties with valid values.
	/// </summary>
	public interface IRequireAfterAxesFinalized {
		/// <summary>
		/// Generate coordinates after the axis limits are finalized.
		/// </summary>
		void AxesFinalized(IChartRenderContext icrc);
	}
	#endregion
	#region IRequireTransforms
	/// <summary>
	/// Require Transforms pass.
	/// MAY also implement <see cref="IRequireAfterAxesFinalized"/>
	/// SHOULD also implement one of <see cref="IRequireRender"/>, <see cref="IDataSourceRenderer"/> or <see cref="IProvideDataSourceRenderer"/>.
	/// </summary>
	public interface IRequireTransforms {
		/// <summary>
		/// Adjust transforms after layout and rendering are completed OR size changed.
		/// </summary>
		/// <param name="icrc">The context.</param>
		void Transforms(IChartRenderContext icrc);
	}
	#endregion
	#region IProvideLegend
	/// <summary>
	/// Ability to participate in the legend items collection.
	/// </summary>
	public interface IProvideLegend {
		/// <summary>
		/// The legend item(s) for this component.
		/// MUST return a stable enumeration (same values).
		/// MUST NOT be called before <see cref="IRequireEnterLeave.Enter"/>.
		/// </summary>
		IEnumerable<Legend> LegendItems { get; }
	}
	#endregion
	#region IProvideLegendDynamic
	/// <summary>
	/// Event args for the <see cref="IProvideLegendDynamic.LegendChanged"/> event.
	/// </summary>
	public sealed class LegendDynamicEventArgs : EventArgs {
		/// <summary>
		/// The previous items.
		/// </summary>
		public IEnumerable<Legend> PreviousItems { get; private set; }
		/// <summary>
		/// The current items.
		/// </summary>
		public IEnumerable<Legend> CurrentItems { get; private set; }
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="pitems"></param>
		/// <param name="nitems"></param>
		public LegendDynamicEventArgs(IEnumerable<Legend> pitems, IEnumerable<Legend> nitems) { PreviousItems = pitems; CurrentItems = nitems; }
	}
	/// <summary>
	/// Ability to provide a dynamically-varying set of legend items.
	/// </summary>
	public interface IProvideLegendDynamic : IProvideLegend {
		/// <summary>
		/// Event to signal the legend items have changed.
		/// </summary>
		event TypedEventHandler<ChartComponent, LegendDynamicEventArgs> LegendChanged;
	}
	#endregion
	#region IProvideValueExtents
	/// <summary>
	/// Ability to provide Value-Axis extents.
	/// </summary>
	public interface IProvideValueExtents {
		/// <summary>
		/// The lowest value.
		/// If unset, MUST be double.NaN.
		/// </summary>
		double Minimum { get; }
		/// <summary>
		/// The highest value.
		/// If unset, MUST be double.NaN.
		/// </summary>
		double Maximum { get; }
		/// <summary>
		/// Name of the axis.
		/// SHOULD be not-empty.
		/// </summary>
		String ValueAxisName { get; }
	}
	#endregion
	#region IProvideCategoryExtents
	/// <summary>
	/// Ability to provide Category-Axis extents.
	/// </summary>
	public interface IProvideCategoryExtents {
		/// <summary>
		/// The lowest value.
		/// If unset, MUST be double.NaN.
		/// </summary>
		double CategoryMinimum { get; }
		/// <summary>
		/// The highest value.
		/// If unset, MUST be double.NaN.
		/// </summary>
		double CategoryMaximum { get; }
		/// <summary>
		/// Name of the axis.
		/// SHOULD be not-empty.
		/// </summary>
		String CategoryAxisName { get; }
	}
	#endregion
	#region IProvideCustomTransform
	/// <summary>
	/// Ability to provide a transform not based on any axis information.
	/// Primary example is a pie wedge, which does not use any axes, but creates a custom transform based on its internal geometry.
	/// This allows other components to discover the correct transform to juxtapose elements with.
	/// </summary>
	public interface IProvideCustomTransform {
		/// <summary>
		/// Provide the custom transform for given area.
		/// </summary>
		/// <param name="area">Target area.</param>
		/// <returns>Combined MP matrix for the area.</returns>
		Matrix TransformFor(Rect area);
	}
	#endregion
	#region RefreshRequestEventHandler
	/// <summary>
	/// Refresh request type.
	/// Indicates the relative "severity" of requested update.
	/// MUST be honest!
	/// </summary>
	public enum RefreshRequestType {
		/// <summary>
		/// So very dirty...
		/// Implies ValueDirty and TransformsDirty.
		/// </summary>
		LayoutDirty,
		/// <summary>
		/// A value that generates Geometry has changed.
		/// Implies TransformsDirty.
		/// </summary>
		ValueDirty,
		/// <summary>
		/// Something that affects the transforms has changed.
		/// </summary>
		TransformsDirty
	};
	/// <summary>
	/// Axis update information.
	/// If the refresh request indicates axis extents are "intact" the refresh SHOULD be optimized.
	/// MUST be honest!
	/// </summary>
	public enum AxisUpdateState {
		/// <summary>
		/// No axis updates required.
		/// </summary>
		None,
		/// <summary>
		/// Value axis update required.
		/// </summary>
		Value,
		/// <summary>
		/// Category axis update required.
		/// </summary>
		Category,
		/// <summary>
		/// Both axes update required.
		/// </summary>
		Both,
		/// <summary>
		/// Unknown or expensive to check; treat as "Both" or "risk it".
		/// </summary>
		Unknown
	};
	/// <summary>
	/// Refresh request event args.
	/// </summary>
	public sealed class RefreshRequestEventArgs : EventArgs {
		/// <summary>
		/// Initialize.
		/// </summary>
		/// <param name="rrt">The request type.</param>
		/// <param name="aus">The axis update info.</param>
		/// <param name="cc">The component.</param>
		public RefreshRequestEventArgs(RefreshRequestType rrt, AxisUpdateState aus, ChartComponent cc) {
			this.Request = rrt;
			this.Component = cc;
			this.Axis = aus;
		}
		/// <summary>
		/// The request type.
		/// </summary>
		public RefreshRequestType Request { get; private set; }
		/// <summary>
		/// The component requesting refresh.
		/// </summary>
		public ChartComponent Component { get; private set; }
		/// <summary>
		/// Information about axis state.
		/// This is used to hopefully optimize the refresh process.
		/// </summary>
		public AxisUpdateState Axis { get; private set; }
	}
	/// <summary>
	/// Refresh delegate.
	/// </summary>
	/// <param name="cc">Originating component.</param>
	/// <param name="rrea">Refresh request info.</param>
	public delegate void RefreshRequestEventHandler(ChartComponent cc, RefreshRequestEventArgs rrea);
	#endregion
	#region ChartComponent
	/// <summary>
	/// Base class of chart components.
	/// It is FrameworkElement primarily to participate in DataContext and Binding.
	/// </summary>
	public abstract class ChartComponent : FrameworkElement {
		#region ctor
		/// <summary>
		/// Default ctor.
		/// </summary>
		protected ChartComponent() { }
		#endregion
		#region events
		/// <summary>
		/// "External" interest in this component's updates.
		/// </summary>
		public event RefreshRequestEventHandler RefreshRequest;
		#endregion
		#region properties
		/// <summary>
		/// True: visuals require re-computing.
		/// </summary>
		public bool Dirty { get; protected set; }
		#endregion
		#region helpers
		/// <summary>
		/// Generic DP property change handler.
		/// Calls ChartComponent.Refresh(ValueDirty, Unknown).
		/// </summary>
		/// <param name="ddo"></param>
		/// <param name="dpcea"></param>
		protected static void PropertyChanged_ValueDirty(DependencyObject ddo, DependencyPropertyChangedEventArgs dpcea) {
			var cc = ddo as ChartComponent;
			cc.Refresh(RefreshRequestType.ValueDirty, AxisUpdateState.Unknown);
		}
		/// <summary>
		/// Return the name if set, otherwise the type.
		/// </summary>
		/// <returns>Name or type.</returns>
		public String NameOrType() {
			return String.IsNullOrEmpty(Name) ? GetType().Name : Name;
		}
		/// <summary>
		/// Mark self as dirty and invoke the RefreshRequest event.
		/// </summary>
		/// <param name="rrt">Request type.</param>
		/// <param name="aus">Axis update status.</param>
		protected void Refresh(RefreshRequestType rrt, AxisUpdateState aus) { Dirty = true; RefreshRequest?.Invoke(this, new RefreshRequestEventArgs(rrt, aus, this)); }
		/// <summary>
		/// Bind source.Path to the target.DP.
		/// </summary>
		/// <param name="source">Source instance.</param>
		/// <param name="path">Component's (source) property path.</param>
		/// <param name="target">Target DO.</param>
		/// <param name="dp">FE's (target) DP.</param>
		protected static void BindTo(object source, String path, DependencyObject target, DependencyProperty dp) {
			Binding bx = new Binding() {
				Path = new PropertyPath(path),
				Source = source,
				Mode = BindingMode.OneWay
			};
			target.ClearValue(dp);
			BindingOperations.SetBinding(target, dp, bx);
		}
		/// <summary>
		/// Transfer any binding from the source DP to the given target's DP.
		/// </summary>
		/// <param name="source">Source element.</param>
		/// <param name="path"></param>
		/// <param name="target">Target framework element.</param>
		/// <param name="dp"></param>
		protected static void ApplyBinding(FrameworkElement source, String path, DependencyObject target, DependencyProperty dp) {
			var bx = source.GetBindingExpression(dp);
			if (bx != null) {
				target.ClearValue(dp);
				BindingOperations.SetBinding(target, dp, bx.ParentBinding);
			} else {
				BindTo(source, path, target, dp);
			}
		}
		/// <summary>
		/// Boilerplate for assigning a "local property" from a "reference value" while applying <see cref="IChartErrorInfo"/>.
		/// </summary>
		/// <param name="icei">For error reporting; MAY be NULL.</param>
		/// <param name="vsource">Validation source in error reports.</param>
		/// <param name="localprop">Local property in error reports.</param>
		/// <param name="refprop">Reference property in error reports.</param>
		/// <param name="localcheck">The local check; True to proceed to sourcecheck.</param>
		/// <param name="sourcecheck">The source check; True to proceed to refcheck.</param>
		/// <param name="refcheck">The reference check; True to proceed to action.</param>
		/// <param name="applyvalue">Execute if everything returned True.</param>
		protected static void AssignFromRef(IChartErrorInfo icei, String vsource, String localprop, String refprop, bool localcheck, bool sourcecheck, bool refcheck, Action applyvalue) {
			if (!localcheck) return;
			if (sourcecheck) {
				if (refcheck)
					applyvalue();
				else {
					if (icei != null) {
						icei.Report(new ChartValidationResult(vsource, $"{localprop} not found and {refprop} not found", new[] { localprop, refprop }));
					}
				}
			} else {
				if (icei != null) {
					icei.Report(new ChartValidationResult(vsource, $"{localprop} not found and no Theme was found", new[] { localprop, refprop }));
				}
			}
		}
		#endregion
	}
	#endregion
	#region TreeHelper (disabled not used)
#if false
	/// <summary>
	/// Static Helpers for visual tree navigation.
	/// </summary>
	public static class TreeHelper {
		/// <summary>
		/// Finds object in control's template by its name.
		/// </summary>
		/// <param name="name">Object's name.</param>
		/// <param name="templatedParent">Templated parent.</param>
		/// <returns>!NULL: found object; NULL: otherwise.</returns>
		public static object TemplateFindName(string name, FrameworkElement templatedParent) {
			for (int ix = 0; ix < VisualTreeHelper.GetChildrenCount(templatedParent); ix++) {
				var child = VisualTreeHelper.GetChild(templatedParent, ix);
				if (child is FrameworkElement) {
					if ((child as FrameworkElement).Name == name) {
						return child;
					} else {
						var subChild = TemplateFindName(name, child as FrameworkElement);
						if (subChild is FrameworkElement && (subChild as FrameworkElement).Name == name) {
							return subChild;
						}
					}
				}
			}
			return null;
		}
	}
#endif
	#endregion
	#region BindingEvaluator
	/// <summary>
	/// Utility class to facilitate runtime binding evaluation.
	/// </summary>
	public class BindingEvaluator : FrameworkElement {
		private readonly PropertyPath _pp;
		/// <summary>
		/// Ctor.
		/// Initializes <see cref="_pp"/>.
		/// </summary>
		/// <param name="path">Path to the property.</param>
		public BindingEvaluator(string path) {
			_pp = new PropertyPath(path);
		}
		/// <summary>
		/// Dependency property used to evaluate values.
		/// </summary>
		public static readonly DependencyProperty EvaluatorProperty = DependencyProperty.Register("Evaluator", typeof(object), typeof(BindingEvaluator), null);
		/// <summary>
		/// Returns value of binding on provided object.
		/// </summary>
		/// <param name="source">Object to evaluate binding against.</param>
		/// <returns>Value of the binding.</returns>
		public object For(object source) {
			var binding = new Binding {
				Path = _pp,
				Mode = BindingMode.OneTime,
				Source = source
			};
			SetBinding(EvaluatorProperty, binding);
			return GetValue(EvaluatorProperty);
		}
	}
	#endregion
	#region PathHelper
	/// <summary>
	/// Static methods for creating <see cref="PathFigure"/> instances.
	/// </summary>
	public static class PathHelper {
		/// <summary>
		/// Build Closed PathFigure for given rectangle.
		/// Does not check for coordinates' min/max because the Geometry Transform is not known here.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="top"></param>
		/// <param name="right"></param>
		/// <param name="bottom"></param>
		/// <returns></returns>
		public static PathFigure Rectangle(double left, double top, double right, double bottom) {
			var pf = new PathFigure { StartPoint = new Windows.Foundation.Point(left, top) };
			var ls = new LineSegment() { Point = new Windows.Foundation.Point(left, bottom) };
			pf.Segments.Add(ls);
			ls = new LineSegment() { Point = new Windows.Foundation.Point(right, bottom) };
			pf.Segments.Add(ls);
			ls = new LineSegment() { Point = new Windows.Foundation.Point(right, top) };
			pf.Segments.Add(ls);
			pf.IsClosed = true;
			return pf;
		}
		/// <summary>
		/// Build Open PathFigure for given line segment.
		/// </summary>
		/// <param name="startx"></param>
		/// <param name="starty"></param>
		/// <param name="endx"></param>
		/// <param name="endy"></param>
		/// <returns></returns>
		public static PathFigure Line(double startx, double starty, double endx, double endy) {
			var pf = new PathFigure { StartPoint = new Windows.Foundation.Point(startx, starty) };
			var ls = new LineSegment() { Point = new Windows.Foundation.Point(startx, endy) };
			pf.Segments.Add(ls);
			return pf;
		}
	}
	#endregion
	#region StyleExtensions
	/// <summary>
	/// Extension methods for <see cref="Style"/>.
	/// </summary>
	public static class StyleExtensions {
		/// <summary>
		/// Find the setter and get its value, ELSE return a default value.
		/// </summary>
		/// <typeparam name="T">Return type.</typeparam>
		/// <param name="style">Style to search.</param>
		/// <param name="property">DP to locate.</param>
		/// <param name="defv">Default value to return if nothing found OR the style is NULL.</param>
		/// <returns>The value or DEFV.</returns>
		public static T Find<T>(this Style style, DependencyProperty property, T defv = default(T)) {
			if (style == null) return defv;
			var xx = style.Find(property);
			return xx == null ? defv : (T)xx.Value;
		}
		/// <summary>
		/// Search style and all sub-styles for the given DP, ELSE return a default value.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="style"></param>
		/// <param name="property"></param>
		/// <param name="defv"></param>
		/// <returns></returns>
		public static T FindRecursive<T>(this Style style, DependencyProperty property, T defv = default(T)) {
			if (style == null) return defv;
			if(style.BasedOn != null) {
				var sx = style.BasedOn.Find(property);
				if(sx != null) {
					return (T)sx.Value;
				}
			}
			var xx = style.Find(property);
			return xx == null ? defv : (T)xx.Value;
		}
		/// <summary>
		/// Return the <see cref="Setter"/> for the given DP on the immediate style.
		/// </summary>
		/// <param name="style"></param>
		/// <param name="property"></param>
		/// <returns>The <see cref="Setter"/> or NULL.</returns>
		public static Setter Find(this Style style, DependencyProperty property) {
			if (style == null) return null;
			foreach (var xx in style.Setters) {
				if (xx is Setter sx) {
					if (sx.Property == property) {
						return sx;
					}
				}
			}
			return null;
		}
	}
	#endregion
	#region StyleGenerator
	/// <summary>
	/// Abstract base for style generator.
	/// </summary>
	public abstract class StyleGenerator : DependencyObject {
		#region properties
		/// <summary>
		/// The underlying style to use when creating new styles.
		/// </summary>
		public Style BaseStyle { get { return (Style)GetValue(BaseStyleProperty); } set { SetValue(BaseStyleProperty, value); } }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies the <see cref="BaseStyle"/> DP.
		/// </summary>
		public static readonly DependencyProperty BaseStyleProperty = DependencyProperty.Register(
			nameof(BaseStyle), typeof(string), typeof(StyleGenerator), new PropertyMetadata(null)
		);
		#endregion
		#region extension points
		/// <summary>
		/// Return the "next" style.
		/// </summary>
		/// <returns></returns>
		public abstract Style NextStyle();
		/// <summary>
		/// Reset the style sequence, if applicable.
		/// </summary>
		public abstract void Reset();
		#endregion
		#region helpers
		/// <summary>
		/// Convenience method to copy a style and replace the Fill property with given brush.
		/// </summary>
		/// <param name="source">Source style.</param>
		/// <param name="tp">Target DP.</param>
		/// <param name="pvalue">New value.</param>
		/// <returns></returns>
		/// <typeparam name="PT">Property value type.</typeparam>
		protected Style Override<PT>(Style source, DependencyProperty tp, PT pvalue) {
			var style = new Style(source.TargetType);
			var did = false;
			foreach (var setter in source.Setters) {
				if (setter is Setter sx && sx.Property == tp) {
					style.Setters.Add(new Setter(tp, pvalue));
					did = true;
				} else {
					style.Setters.Add(setter);
				}
			}
			if (!did) {
				style.Setters.Add(new Setter(Path.FillProperty, pvalue));
			}
			return style;
		}
		#endregion
	}
	/// <summary>
	/// Identity.  Returns the BaseStyle over-and-over.
	/// </summary>
	public sealed class IdentityStyleGenerator : StyleGenerator {
		/// <summary>
		/// Return the base style always.
		/// </summary>
		/// <returns></returns>
		public override Style NextStyle() { return BaseStyle; }
		/// <summary>
		/// No Action.
		/// </summary>
		public override void Reset() { }
	}
	/// <summary>
	/// Rotates through a hard-coded set of pre-defined colors.
	/// </summary>
	public sealed class DefaultColorsGenerator : StyleGenerator {
		#region data
		int current;
		Dictionary<Brush, Style> stylemap = new Dictionary<Brush, Style>();
		static Brush[] _presetBrushes = new Brush[] {
			new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x66, 0x00)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0xFC, 0xD2, 0x02)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0xB0, 0xDE, 0x09)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0x0D, 0x8E, 0xCF)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0x2A, 0x0C, 0xD0)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0xCD, 0x0D, 0x74)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0xCC, 0x00, 0x00)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0xCC, 0x00)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x00, 0xCC)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0xDD, 0xDD, 0xDD)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0x99, 0x99, 0x99)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0x33, 0x33, 0x33)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0x99, 0x00, 0x00))
		};
		#endregion
		#region extensions
		/// <summary>
		/// Reset the counter but don't clear the style cache.
		/// </summary>
		public override void Reset() {
			current = 0;
		}
		/// <summary>
		/// Make a new style for each color, caching them as they are created.
		/// </summary>
		/// <returns></returns>
		public override Style NextStyle() {
			var cbrush = _presetBrushes[current];
			if (stylemap.ContainsKey(cbrush)) return stylemap[cbrush];
			var style = Override(BaseStyle, Path.FillProperty, cbrush);
			// advance
			stylemap.Add(cbrush, style);
			current = (current + 1) % _presetBrushes.Length;
			return style;
		}
		#endregion
	}
	#endregion
	#region Recycler<T>
	/// <summary>
	/// Recycles an input list of instances, then provides new instances after those run out.
	/// Does the bookkeeping to track unused and newly-provided instances.
	/// </summary>
	/// <typeparam name="T">Type of items to recycle.</typeparam>
	public class Recycler<T> {
		#region data
		readonly List<T> _unused = new List<T>();
		readonly List<T> _created = new List<T>();
		readonly IEnumerable<T> _source;
		readonly Func<T> _factory;
		#endregion
		#region properties
		/// <summary>
		/// Original items that were not used up by iterating.
		/// </summary>
		public IEnumerable<T> Unused { get { return _unused; } }
		/// <summary>
		/// Excess items that were created after original items were used up.
		/// </summary>
		public IEnumerable<T> Created { get { return _created; } }
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="source">Initial list to reuse; MAY be empty.</param>
		/// <param name="factory">Used to create new instances when SOURCE runs out.</param>
		public Recycler(IEnumerable<T> source, Func<T> factory) {
#pragma warning disable IDE0016 // Use 'throw' expression
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (factory == null) throw new ArgumentNullException(nameof(factory));
#pragma warning restore IDE0016 // Use 'throw' expression
			_unused.AddRange(source);
			_source = source;
			_factory = factory;
		}
		#endregion
		#region public
		/// <summary>
		/// First exhaust the original source, then start creating new instances until caller stops.
		/// Do the bookkeeping for used and created lists.
		/// DO NOT use this to control looping!
		/// </summary>
		/// <returns>Item1: true=created, false=reused; Item2: Another instance.</returns>
		public IEnumerable<Tuple<bool,T>> Items() {
			foreach (var tx in _source) {
				_unused.Remove(tx);
				yield return new Tuple<bool,T>(false, tx);
			}
			while (true) {
				var tx = _factory();
				_created.Add(tx);
				yield return new Tuple<bool, T>(true, tx);
			}
		}
		#endregion
	}
	#endregion
	#region DataTemplateShim
	/// <summary>
	/// "Internal" shim view model used as the <see cref="FrameworkElement.DataContext"/> for a <see cref="DataTemplate"/>.
	/// </summary>
	public class DataTemplateShim : INotifyPropertyChanged {
		/// <summary>
		/// Implemented for <see cref="Windows.UI.Xaml.Data.INotifyPropertyChanged"/>.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;
		#region data
		Visibility _vis;
		String _text;
		#endregion
		#region properties
		/// <summary>
		/// Current visibility.
		/// </summary>
		public Visibility Visibility { get { return _vis; } set { _vis = value; Changed(nameof(Visibility)); } }
		/// <summary>
		/// Current text.
		/// </summary>
		public String Text { get { return _text; } set { _text = value; Changed(nameof(Text)); } }
		#endregion
		#region helpers
		/// <summary>
		/// Hit the <see cref="PropertyChanged"/> event.
		/// </summary>
		/// <param name="prop">Property that changed.</param>
		protected void Changed(String prop) {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
		}
		#endregion
	}
	#endregion
	#region Legend
	/// <summary>
	/// Base VM for the chart legend.
	/// </summary>
	public class Legend {
		/// <summary>
		/// The color swatch to display.
		/// </summary>
		public Brush Fill { get; set; }
		/// <summary>
		/// The border for the swatch.
		/// </summary>
		public Brush Stroke { get; set; }
		/// <summary>
		/// The title.
		/// </summary>
		public String Title { get; set; }
	}
	#endregion
}