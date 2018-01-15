using System;
using Windows.UI.Xaml;

namespace eScapeLLC.UWP.Charts {
	/// <summary>
	/// Ability to provide default styles.
	/// </summary>
	public interface IChartTheme {
		/// <summary>
		/// A name for this theme; informational only.
		/// </summary>
		String Name { get; }
		#region axis labels
		/// <summary>
		/// TextBlock style for Side.Left.
		/// </summary>
		Style LabelAxisLeft { get; }
		/// <summary>
		/// TextBlock style for Side.Right.
		/// </summary>
		Style LabelAxisRight { get; }
		/// <summary>
		/// TextBlock style for Side.Top.
		/// </summary>
		Style LabelAxisTop { get; }
		/// <summary>
		/// TextBlock style for Side.Bottom.
		/// </summary>
		Style LabelAxisBottom { get; }
		#endregion
		#region axis paths
		Style PathAxisCategory { get; }
		Style PathAxisValue { get; }
		#endregion
		#region decorations
		//Style PathGridCategory { get; }
		Style PathGridValue { get; }
		Style PathHorizontalRule { get; }
		Style PathHorizontalBand { get; }
		#endregion
		#region series paths
		Style PathLineSeries { get; }
		Style PathColumnSeries { get; }
		Style PathMarkerSeries { get; }
		#endregion
	}
	/// <summary>
	/// Requirement for the theme.
	/// Theme is provided before IRequireEnterLeave.Enter, and revoked after IRequireEnterLeave.Leave.
	/// </summary>
	public interface IRequireChartTheme {
		/// <summary>
		/// The theme.
		/// </summary>
		IChartTheme Theme { get; set; }
	}
	/// <summary>
	/// This is the "master" set of styles in a chart.
	/// ALL members MUST be non-NULL!
	/// </summary>
	public class ChartTheme : DependencyObject, IChartTheme {
		#region general
		/// <summary>
		/// A name for this theme; informational only.
		/// </summary>
		public String Name { get; set; }
		#endregion
		#region axis labels
		/// <summary>
		/// TextBlock style for Side.Left.
		/// </summary>
		public Style LabelAxisLeft { get { return (Style)GetValue(LabelAxisLeftProperty); } set { SetValue(LabelAxisLeftProperty, value); } }
		/// <summary>
		/// TextBlock style for Side.Right.
		/// </summary>
		public Style LabelAxisRight { get { return (Style)GetValue(LabelAxisRightProperty); } set { SetValue(LabelAxisRightProperty, value); } }
		/// <summary>
		/// TextBlock style for Side.Top.
		/// </summary>
		public Style LabelAxisTop { get { return (Style)GetValue(LabelAxisTopProperty); } set { SetValue(LabelAxisTopProperty, value); } }
		/// <summary>
		/// TextBlock style for Side.Bottom.
		/// </summary>
		public Style LabelAxisBottom { get { return (Style)GetValue(LabelAxisBottomProperty); } set { SetValue(LabelAxisBottomProperty, value); } }
		#endregion
		#region axis paths
		public Style PathAxisCategory { get { return (Style)GetValue(PathAxisCategoryProperty); } set { SetValue(PathAxisCategoryProperty, value); } }
		public Style PathAxisValue { get { return (Style)GetValue(PathAxisValueProperty); } set { SetValue(PathAxisValueProperty, value); } }
		#endregion
		#region decorations
		//Style PathGridCategory { get; }
		public Style PathGridValue { get { return (Style)GetValue(PathGridValueProperty); } set { SetValue(PathGridValueProperty, value); } }
		public Style PathHorizontalRule { get { return (Style)GetValue(PathHorizontalRuleProperty); } set { SetValue(PathHorizontalRuleProperty, value); } }
		public Style PathHorizontalBand { get { return (Style)GetValue(PathHorizontalBandProperty); } set { SetValue(PathHorizontalBandProperty, value); } }
		#endregion
		#region series paths
		public Style PathLineSeries { get { return (Style)GetValue(PathLineSeriesProperty); } set { SetValue(PathLineSeriesProperty, value); } }
		public Style PathColumnSeries { get { return (Style)GetValue(PathColumnSeriesProperty); } set { SetValue(PathColumnSeriesProperty, value); } }
		public Style PathMarkerSeries { get { return (Style)GetValue(PathMarkerSeriesProperty); } set { SetValue(PathMarkerSeriesProperty, value); } }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="LabelAxisLeft"/> DP.
		/// </summary>
		public static readonly DependencyProperty LabelAxisLeftProperty = DependencyProperty.Register(nameof(LabelAxisLeft), typeof(Style), typeof(ChartTheme), new PropertyMetadata(null));
		/// <summary>
		/// Identifies <see cref="LabelAxisRight"/> DP.
		/// </summary>
		public static readonly DependencyProperty LabelAxisRightProperty = DependencyProperty.Register(nameof(LabelAxisRight), typeof(Style), typeof(ChartTheme), new PropertyMetadata(null));
		/// <summary>
		/// Identifies <see cref="LabelAxisTop"/> DP.
		/// </summary>
		public static readonly DependencyProperty LabelAxisTopProperty = DependencyProperty.Register(nameof(LabelAxisTop), typeof(Style), typeof(ChartTheme), new PropertyMetadata(null));
		/// <summary>
		/// Identifies <see cref="LabelAxisBottom"/> DP.
		/// </summary>
		public static readonly DependencyProperty LabelAxisBottomProperty = DependencyProperty.Register(nameof(LabelAxisBottom), typeof(Style), typeof(ChartTheme), new PropertyMetadata(null));
		/// <summary>
		/// Identifies <see cref="PathAxisCategory"/> DP.
		/// </summary>
		public static readonly DependencyProperty PathAxisCategoryProperty = DependencyProperty.Register(nameof(PathAxisCategory), typeof(Style), typeof(ChartTheme), new PropertyMetadata(null));
		/// <summary>
		/// Identifies <see cref="PathAxisValue"/> DP.
		/// </summary>
		public static readonly DependencyProperty PathAxisValueProperty = DependencyProperty.Register(nameof(PathAxisValue), typeof(Style), typeof(ChartTheme), new PropertyMetadata(null));
		/// <summary>
		/// Identifies <see cref="PathGridValue"/> DP.
		/// </summary>
		public static readonly DependencyProperty PathGridValueProperty = DependencyProperty.Register(nameof(PathGridValue), typeof(Style), typeof(ChartTheme), new PropertyMetadata(null));
		/// <summary>
		/// Identifies <see cref="PathHorizontalRule"/> DP.
		/// </summary>
		public static readonly DependencyProperty PathHorizontalRuleProperty = DependencyProperty.Register(nameof(PathHorizontalRule), typeof(Style), typeof(ChartTheme), new PropertyMetadata(null));
		/// <summary>
		/// Identifies <see cref="PathHorizontalBand"/> DP.
		/// </summary>
		public static readonly DependencyProperty PathHorizontalBandProperty = DependencyProperty.Register(nameof(PathHorizontalBand), typeof(Style), typeof(ChartTheme), new PropertyMetadata(null));
		/// <summary>
		/// Identifies <see cref="PathLineSeries"/> DP.
		/// </summary>
		public static readonly DependencyProperty PathLineSeriesProperty = DependencyProperty.Register(nameof(PathLineSeries), typeof(Style), typeof(ChartTheme), new PropertyMetadata(null));
		/// <summary>
		/// Identifies <see cref="PathColumnSeries"/> DP.
		/// </summary>
		public static readonly DependencyProperty PathColumnSeriesProperty = DependencyProperty.Register(nameof(PathColumnSeries), typeof(Style), typeof(ChartTheme), new PropertyMetadata(null));
		/// <summary>
		/// Identifies <see cref="PathMarkerSeries"/> DP.
		/// </summary>
		public static readonly DependencyProperty PathMarkerSeriesProperty = DependencyProperty.Register(nameof(PathMarkerSeries), typeof(Style), typeof(ChartTheme), new PropertyMetadata(null));
		#endregion
	}
}
