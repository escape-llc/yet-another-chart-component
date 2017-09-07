using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region IChartAxis
	public enum AxisType { Category, Value };
	/// <summary>
	/// Features for axes.
	/// </summary>
	public interface IChartAxis {
		/// <summary>
		/// The axis type.
		/// </summary>
		AxisType Type { get; }
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
		/// Map the value.
		/// </summary>
		/// <param name="value">Input (actual) value.</param>
		/// <returns>Axis-mapped value.</returns>
		double For(double value);
	}
	#endregion
	#region IChartRenderContext
	/// <summary>
	/// Feaatures for rendering.
	/// </summary>
	public interface IChartRenderContext {
		/// <summary>
		/// Current dimensions.
		/// </summary>
		Size Dimensions { get; }
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
	/// <summary>
	/// Additional features for enter/leave.
	/// </summary>
	public interface IChartEnterLeaveContext : IChartRenderContext {
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
	}
	#endregion
	#region ChartComponent
	/// <summary>
	/// Refresh delegate.
	/// </summary>
	/// <param name="cc">Originating component.</param>
	public delegate void RefreshRequestEventHandler(ChartComponent cc);
	/// <summary>
	/// Base class of chart components.
	/// </summary>
	public abstract class ChartComponent : FrameworkElement {
		#region ctor
		protected ChartComponent() { }
		#endregion
		#region extension points
		/// <summary>
		/// Render the component.
		/// </summary>
		/// <param name="icrc"></param>
		public abstract void Render(IChartRenderContext icrc);
		/// <summary>
		/// Adjust transforms after rendering and layout are completed.
		/// Default impl.
		/// </summary>
		/// <param name="icrc"></param>
		public virtual void Transforms(IChartRenderContext icrc) { }
		/// <summary>
		/// Component is entering the chart.
		/// Default impl.
		/// </summary>
		/// <param name="icelc"></param>
		public virtual void Enter(IChartEnterLeaveContext icelc) { }
		/// <summary>
		/// Component is leaving the chart.
		/// Default impl.
		/// </summary>
		/// <param name="icelc"></param>
		public virtual void Leave(IChartEnterLeaveContext icelc) { }
		#endregion
		#region events
		/// <summary>
		/// Listen for requests to update this component.
		/// </summary>
		public event RefreshRequestEventHandler RefreshRequest;
		#endregion
		#region properties
		/// <summary>
		/// True: visuals require re-computing.
		/// </summary>
		public bool Dirty { get; set; }
		#endregion
		#region helpers
		/// <summary>
		/// Invoke the refresh request event.
		/// </summary>
		protected void Refresh() { RefreshRequest?.Invoke(this); }
		/// <summary>
		/// Bind the Brush DP to the given shape.
		/// </summary>
		/// <param name="sh"></param>
		/// <param name="dp"></param>
		protected static void BindBrush(ChartComponent cc, String path, Shape sh, DependencyProperty dp) {
			Binding bx = new Binding(); // new Binding("Brush");
			bx.Path = new PropertyPath(path);
			bx.Source = cc;
			sh.SetBinding(dp, bx);
		}
		#endregion
	}
	#endregion
	#region TreeHelper
	public static class TreeHelper {
		/// <summary>
		/// Finds object in control's template by its name.
		/// </summary>
		/// <param name="name">Objects name.</param>
		/// <param name="templatedParent">Templated parent.</param>
		/// <returns>Object reference if found, null otherwise.</returns>
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
	#endregion
	#region BindingEvaluator
	/// <summary>
	/// Utility class to facilitate temporary binding evaluation.
	/// </summary>
	public class BindingEvaluator : FrameworkElement {
		/// <summary>
		/// Created binding evaluator and set path to the property which's value should be evaluated.
		/// </summary>
		/// <param name="propertyPath">Path to the property.</param>
		public BindingEvaluator(string propertyPath) {
			_propertyPath = propertyPath;
		}
		private string _propertyPath;
		/// <summary>
		/// Dependency property used to evaluate values.
		/// </summary>
		public static readonly DependencyProperty EvaluatorProperty = DependencyProperty.Register("Evaluator", typeof(object), typeof(BindingEvaluator), null);
		/// <summary>
		/// Returns evaluated value of property on provided object source.
		/// </summary>
		/// <param name="source">Object for which property value should be evaluated.</param>
		/// <returns>Value of the property.</returns>
		public object Eval(object source) {
			ClearValue(EvaluatorProperty);
			var binding = new Binding {
				Path = new PropertyPath(_propertyPath),
				Mode = BindingMode.OneTime,
				Source = source
			};
			SetBinding(EvaluatorProperty, binding);
			return GetValue(EvaluatorProperty);
		}
	}
	#endregion
	#region PathHelper
	public static class PathHelper {
		public static PathFigure Rectangle(double left, double top, double right, double bottom) {
			var pf = new PathFigure {
				StartPoint = new Windows.Foundation.Point(left, top)
			};
			var ls = new LineSegment() {
				Point = new Windows.Foundation.Point(left, bottom)
			};
			pf.Segments.Add(ls);
			ls = new LineSegment() {
				Point = new Windows.Foundation.Point(right, bottom)
			};
			pf.Segments.Add(ls);
			ls = new LineSegment() {
				Point = new Windows.Foundation.Point(right, top)
			};
			pf.Segments.Add(ls);
			ls = new LineSegment() {
				Point = new Windows.Foundation.Point(left, top)
			};
			pf.Segments.Add(ls);
			return pf;
		}
	}
	#endregion
}