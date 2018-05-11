using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region LegendBase
	/// <summary>
	/// Abstract base class for legend implementations.
	/// </summary>
	public abstract class LegendBase : ViewModelBase {
		String _title;
		/// <summary>
		/// The title.
		/// </summary>
		public String Title { get { return _title; } set { _title = value; Changed(nameof(Title)); } }
	}
	#endregion
	#region Legend
	/// <summary>
	/// Legend VM that mimics the "color scheme" of the component.
	/// </summary>
	public class Legend : LegendBase {
		Brush _fill;
		Brush _stroke;
		/// <summary>
		/// The color swatch to display.
		/// </summary>
		public Brush Fill { get { return _fill; } set { _fill = value; Changed(nameof(Fill)); } }
		/// <summary>
		/// The border for the swatch.
		/// </summary>
		public Brush Stroke { get { return _stroke; } set { _stroke = value; Changed(nameof(Stroke)); } }
	}
	#endregion
	#region LegendWithPath
	/// <summary>
	/// Legend VM with a custom Path for its visualization.
	/// </summary>
	public class LegendWithPath : LegendBase {
		Path _path;
		/// <summary>
		/// The path to display in the legend.
		/// </summary>
		public Path Path { get { return _path; } set { _path = value; Changed(nameof(Path)); } }
	}
	#endregion
	#region LegendWithElement
	/// <summary>
	/// Legend VM with a custom Path for its visualization.
	/// </summary>
	public class LegendWithElement : LegendBase {
		FrameworkElement _element;
		/// <summary>
		/// The element to display in the legend.
		/// MAY come from a <see cref="DataTemplate"/>.
		/// </summary>
		public FrameworkElement Element { get { return _element; } set { _element = value; Changed(nameof(Element)); } }
	}
	#endregion
}
