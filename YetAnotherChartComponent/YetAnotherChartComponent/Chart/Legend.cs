using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
	/// Legend VM with a custom Geometry for its visualization.
	/// </summary>
	public class LegendWithGeometry : Legend {
		Geometry _data;
		/// <summary>
		/// The path to display in the legend.
		/// </summary>
		public Geometry Data { get { return _data; } set { _data = value; Changed(nameof(Data)); } }
	}
	#endregion
	#region LegendWithImageSource
	/// <summary>
	/// Legend VM with a custom ImageSource for its visualization.
	/// </summary>
	public class LegendWithImageSource : LegendBase {
		ImageSource _source;
		/// <summary>
		/// The path to display in the legend.
		/// </summary>
		public ImageSource Source { get { return _source; } set { _source = value; Changed(nameof(Source)); } }
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
	#region LegendTemplateSelector
	/// <summary>
	/// Default <see cref="DataTemplateSelector"/> for Legends.
	/// </summary>
	public class LegendTemplateSelector : DataTemplateSelector {
		/// <summary>
		/// Use as default.
		/// </summary>
		public DataTemplate ForLegend { get; set; }
		/// <summary>
		/// Use for <see cref="LegendWithGeometry"/>.
		/// </summary>
		public DataTemplate ForLegendWithGeometry { get; set; }
		/// <summary>
		/// Use for <see cref="LegendWithImageSource"/>.
		/// </summary>
		public DataTemplate ForLegendWithImageSource { get; set; }
		/// <summary>
		/// Determine a suitable <see cref="DataTemplate"/>.
		/// </summary>
		/// <param name="item">SHOULD be subclass of <see cref="LegendBase"/>.</param>
		/// <param name="container">Not consulted.</param>
		/// <returns></returns>
		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) {
			if (item is LegendWithImageSource && ForLegendWithImageSource != null)
				return ForLegendWithImageSource;
			else if (item is LegendWithGeometry && ForLegendWithGeometry != null)
				return ForLegendWithGeometry;
			else if (ForLegend != null)
				return ForLegend;
			return base.SelectTemplateCore(item, container);
		}
	}
	#endregion
}
