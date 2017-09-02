using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace eScapeLLC.UWP.Charts {
	#region IChartAxis
	public interface IChartAxis {
		void ResetLimits();
		double For(double value);
		double Minimum { get; }
		double Maximum { get; }
	}
	#endregion
	#region IChartRenderContext
	public interface IChartRenderContext {
		Size Dimensions { get; }
		object DataContext { get; }
		ChartComponent Find(String name);
	}
	#endregion
	#region ChartComponent
	public delegate void RefreshRequestEventHandler(ChartComponent cc);
	public abstract class ChartComponent : FrameworkElement {
		protected ChartComponent() { }
		/// <summary>
		/// Render the component.
		/// </summary>
		/// <param name="icrc"></param>
		public abstract void Render(IChartRenderContext icrc);
		/// <summary>
		/// Component is entering the chart.
		/// </summary>
		public virtual void Enter() { }
		/// <summary>
		/// Component is leaving the chart.
		/// </summary>
		public virtual void Leave() { }
		/// <summary>
		/// Listen for requests to update this component.
		/// </summary>
		public event RefreshRequestEventHandler RefreshRequest;
		/// <summary>
		/// Invoke the refresh request event.
		/// </summary>
		protected void Refresh() { RefreshRequest?.Invoke(this); }
	}
	#endregion
	#region TreeHelper
	public static class TreeHelper {
		/// <summary>
		/// Finds object in control's template by it's name.
		/// </summary>
		/// <param name="name">Objects name.</param>
		/// <param name="templatedParent">Templated parent.</param>
		/// <returns>Object reference if found, null otherwise.</returns>
		public static object TemplateFindName(string name, FrameworkElement templatedParent) {
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(templatedParent); i++) {
				DependencyObject child = VisualTreeHelper.GetChild(templatedParent, i);
				if (child is FrameworkElement) {
					if (((FrameworkElement)child).Name == name) {
						return child;
					} else {
						object subChild = TreeHelper.TemplateFindName(name, (FrameworkElement)child);
						if (subChild != null && subChild is FrameworkElement && ((FrameworkElement)subChild).Name == name) {
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
	/// Utility class to facilitate temporary binding evaluation
	/// </summary>
	public class BindingEvaluator : FrameworkElement {
		/// <summary>
		/// Created binding evaluator and set path to the property which's value should be evaluated.
		/// </summary>
		/// <param name="propertyPath">Path to the property</param>
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
			var binding = new Binding();
			binding.Path = new PropertyPath(_propertyPath);
			binding.Mode = BindingMode.OneTime;
			binding.Source = source;
			SetBinding(EvaluatorProperty, binding);
			return GetValue(EvaluatorProperty);
		}
	}
	#endregion
}