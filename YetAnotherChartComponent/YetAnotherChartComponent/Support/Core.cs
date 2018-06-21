using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

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
		/// MUST be called from UI thread.
		/// </summary>
		/// <param name="fe">Element to add.</param>
		void Add(FrameworkElement fe);
		/// <summary>
		/// Remove content.
		/// MUST be called from UI thread.
		/// </summary>
		/// <param name="fe">Element to remove.</param>
		void Remove(FrameworkElement fe);
		/// <summary>
		/// Add group of elements.
		/// MUST be called from UI thread.
		/// </summary>
		/// <param name="fes"></param>
		void Add(IEnumerable<FrameworkElement> fes);
		/// <summary>
		/// Remove group of elements.
		/// MUST be called from UI thread.
		/// </summary>
		/// <param name="fes"></param>
		void Remove(IEnumerable<FrameworkElement> fes);
		/// <summary>
		/// Position the layer.
		/// MUST be called from UI thread.
		/// </summary>
		/// <param name="target"></param>
		void Layout(Rect target);
		/// <summary>
		/// Remove all the components this layer knows about.
		/// MUST be called from UI thread.
		/// </summary>
		void Clear();
	}
	/// <summary>
	/// Accept animation configuration.
	/// </summary>
	public interface IChartLayerAnimation {
		/// <summary>
		/// Control the state of implicit composition animations (for Offset).
		/// </summary>
		bool UseImplicitAnimations { get; set; }
		/// <summary>
		/// Play on entry to VT.
		/// </summary>
		Storyboard Enter { get; set; }
		/// <summary>
		/// Play on exit, before removing from VT.
		/// </summary>
		Storyboard Leave { get; set; }
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
	/// Which type of render pipeline is running.
	/// </summary>
	public enum RenderType {
		/// <summary>
		/// Full render.
		/// </summary>
		Full,
		/// <summary>
		/// Chart transforms-only render, or component transforms-only render.
		/// </summary>
		TransformsOnly,
		/// <summary>
		/// Component full render.
		/// </summary>
		Component,
		/// <summary>
		/// Incremental render.
		/// </summary>
		Incremental
	}
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
		/// Type of render pipeline.
		/// </summary>
		RenderType Type { get; }
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
	#region IRequireCategoryAxis
	/// <summary>
	/// Requirement for Category-Axis mapping.
	/// </summary>
	public interface IRequireCategoryAxis {
		/// <summary>
		/// Name of the axis.
		/// SHOULD be not-empty.
		/// </summary>
		String CategoryAxisName { get; }
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
		/// A value that generates <see cref="Geometry"/> has changed.
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
	public class BindingEvaluator : DependencyObject {
		#region data
		private readonly PropertyPath _pp;
		private readonly RelativeSource _rs;
		#endregion
		#region DPs
		/// <summary>
		/// Dependency property used to evaluate values.
		/// Note there is NO backing "Evaluator" property!
		/// </summary>
		public static readonly DependencyProperty EvaluatorProperty = DependencyProperty.Register("Evaluator", typeof(object), typeof(BindingEvaluator), null);
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// Initializes <see cref="_pp"/>  and <see cref="_rs"/>.
		/// If the <see cref="PropertyPath.Path"/> is "." or <see cref="String.Empty"/> then a <see cref="RelativeSource"/> binding is configured.
		/// </summary>
		/// <param name="path">Path to the property.</param>
		public BindingEvaluator(string path) {
			var isrel = path == "." || String.IsNullOrEmpty(path);
			_pp = isrel ? null : new PropertyPath(path);
			_rs = isrel ? new RelativeSource() { Mode = RelativeSourceMode.Self } : null;
		}
		#endregion
		#region public
		/// <summary>
		/// Returns value of binding on provided object.
		/// If the <see cref="PropertyPath.Path"/> is "." then a <see cref="RelativeSource"/> binding is configured.
		/// </summary>
		/// <param name="source">Object to evaluate binding against.</param>
		/// <returns>Value of the binding.</returns>
		public object For(object source) {
			var binding = new Binding {
				Path = _pp,
				Mode = BindingMode.OneTime,
				Source = source,
				RelativeSource = _rs
			};
			BindingOperations.SetBinding(this, EvaluatorProperty, binding);
			return GetValue(EvaluatorProperty);
		}
		#endregion
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
		/// Goes "around" in CCW direction.
		/// Start(LT), Segments(LB, RB, RT), Closed(True)
		/// </summary>
		/// <param name="left">X1.</param>
		/// <param name="top">Y1.</param>
		/// <param name="right">X2.</param>
		/// <param name="bottom">Y2.</param>
		/// <returns>New instance.</returns>
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
		/// <param name="startx">X1.</param>
		/// <param name="starty">Y1.</param>
		/// <param name="endx">X2.</param>
		/// <param name="endy">Y2.</param>
		/// <returns>New instance.</returns>
		public static PathFigure Line(double startx, double starty, double endx, double endy) {
			var pf = new PathFigure { StartPoint = new Windows.Foundation.Point(startx, starty) };
			var ls = new LineSegment() { Point = new Windows.Foundation.Point(startx, endy) };
			pf.Segments.Add(ls);
			return pf;
		}
	}
	#endregion
	#region ViewModelBase
	/// <summary>
	/// Very lightweight VM base class.
	/// </summary>
	public abstract class ViewModelBase : INotifyPropertyChanged {
		/// <summary>
		/// Implemented for <see cref="Windows.UI.Xaml.Data.INotifyPropertyChanged"/>.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;
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
	#region Shims
	/// <summary>
	/// "Internal" VM used as the <see cref="FrameworkElement.DataContext"/> for a <see cref="DataTemplate"/> used by a <see cref="ChartComponent"/>.
	/// </summary>
	public class DataTemplateShim : ViewModelBase {
		#region data
		Visibility _vis;
		#endregion
		#region properties
		/// <summary>
		/// Current visibility.
		/// </summary>
		public Visibility Visibility { get { return _vis; } set { _vis = value; Changed(nameof(Visibility)); } }
		#endregion
	}
	/// <summary>
	/// VM for a text label context.
	/// </summary>
	public class TextShim : DataTemplateShim {
		#region data
		String _text;
		#endregion
		#region properties
		/// <summary>
		/// Current text.
		/// </summary>
		public String Text { get { return _text; } set { _text = value; Changed(nameof(Text)); } }
		#endregion
	}
	/// <summary>
	/// VM shim for a custom label context.
	/// </summary>
	public class ObjectShim : TextShim {
		#region data
		object _value;
		#endregion
		#region properties
		/// <summary>
		/// Additional custom state.
		/// </summary>
		public object CustomValue { get { return _value; } set { _value = value; Changed(nameof(CustomValue)); } }
		#endregion
	}
	#endregion
}