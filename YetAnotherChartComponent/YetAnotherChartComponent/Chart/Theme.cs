using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

namespace eScapeLLC.UWP.Charts {
	#region IChartTheme
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
		/// <summary>
		/// Path style for Category axis.
		/// </summary>
		Style PathAxisCategory { get; }
		/// <summary>
		/// Path style for Value axis.
		/// </summary>
		Style PathAxisValue { get; }
		#endregion
		#region decorations
		//Style PathGridCategory { get; }
		/// <summary>
		/// Path style for Value axis Grid.
		/// </summary>
		Style PathGridValue { get; }
		/// <summary>
		/// Path style for horizontal rule and horizontal band rules.
		/// </summary>
		Style PathHorizontalRule { get; }
		/// <summary>
		/// Path style for horizonal band fill.
		/// </summary>
		Style PathHorizontalBand { get; }
		#endregion
		#region series paths
		/// <summary>
		/// Path style for line series.
		/// </summary>
		Style PathLineSeries { get; }
		/// <summary>
		/// Path style for column series.
		/// </summary>
		Style PathColumnSeries { get; }
		/// <summary>
		/// Path style for marker series.
		/// </summary>
		Style PathMarkerSeries { get; }
		#endregion
		#region FrameworkElements
		/// <summary>
		/// Data template for "default" data template.
		/// It MUST be configured to accept a <see cref="DataTemplateShim"/> as its <see cref="FrameworkElement.DataContext"/>.
		/// </summary>
		DataTemplate TextBlockTemplate { get; }
		/// <summary>
		/// Data template for "default" series item containers.
		/// It MUST be configured to accept a <see cref="DataTemplateShim"/> as its <see cref="FrameworkElement.DataContext"/>.
		/// </summary>
		DataTemplate PathTemplate { get; }
		/// <summary>
		/// Data template for "default" series item containers.
		/// It MUST be configured to accept a <see cref="DataTemplateShim"/> as its <see cref="FrameworkElement.DataContext"/>.
		/// </summary>
		DataTemplate ImageTemplate { get; }
		#endregion
		#region Storyboards
		/// <summary>
		/// Storyboard for "enter" chart.
		/// </summary>
		Storyboard EnterAnimation { get; }
		/// <summary>
		/// Storyboard for "leave" chart.
		/// </summary>
		Storyboard LeaveAnimation { get; }
		#endregion
	}
	#endregion
	#region IRequireChartTheme
	/// <summary>
	/// Requirement for the theme.
	/// Theme is provided before <see cref="IRequireEnterLeave.Enter"/>, and revoked after <see cref="IRequireEnterLeave.Leave"/>.
	/// </summary>
	public interface IRequireChartTheme {
		/// <summary>
		/// Holder for the theme.
		/// </summary>
		IChartTheme Theme { get; set; }
	}
	#endregion
	#region ChartTheme
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
		/// <summary>
		/// Path style for Category axis.
		/// </summary>
		public Style PathAxisCategory { get { return (Style)GetValue(PathAxisCategoryProperty); } set { SetValue(PathAxisCategoryProperty, value); } }
		/// <summary>
		/// Path style for Value axis Grid.
		/// </summary>
		public Style PathAxisValue { get { return (Style)GetValue(PathAxisValueProperty); } set { SetValue(PathAxisValueProperty, value); } }
		#endregion
		#region decorations
		//Style PathGridCategory { get; }
		/// <summary>
		/// Path style for Value axis Grid.
		/// </summary>
		public Style PathGridValue { get { return (Style)GetValue(PathGridValueProperty); } set { SetValue(PathGridValueProperty, value); } }
		/// <summary>
		/// Path style for horizontal rule and horizontal band rules.
		/// </summary>
		public Style PathHorizontalRule { get { return (Style)GetValue(PathHorizontalRuleProperty); } set { SetValue(PathHorizontalRuleProperty, value); } }
		/// <summary>
		/// Path style for horizonal band fill.
		/// </summary>
		public Style PathHorizontalBand { get { return (Style)GetValue(PathHorizontalBandProperty); } set { SetValue(PathHorizontalBandProperty, value); } }
		#endregion
		#region series paths
		/// <summary>
		/// Path style for line series.
		/// </summary>
		public Style PathLineSeries { get { return (Style)GetValue(PathLineSeriesProperty); } set { SetValue(PathLineSeriesProperty, value); } }
		/// <summary>
		/// Path style for column series.
		/// </summary>
		public Style PathColumnSeries { get { return (Style)GetValue(PathColumnSeriesProperty); } set { SetValue(PathColumnSeriesProperty, value); } }
		/// <summary>
		/// Path style for marker series.
		/// </summary>
		public Style PathMarkerSeries { get { return (Style)GetValue(PathMarkerSeriesProperty); } set { SetValue(PathMarkerSeriesProperty, value); } }
		#endregion
		#region FrameworkElements
		/// <summary>
		/// Template for text blocks.
		/// </summary>
		public DataTemplate TextBlockTemplate { get { return (DataTemplate)GetValue(TextBlockTemplateProperty); } set { SetValue(TextBlockTemplateProperty, value); } }
		/// <summary>
		/// Data template for "default" series item containers.
		/// MUST be a Path.
		/// </summary>
		public DataTemplate PathTemplate { get { return (DataTemplate)GetValue(PathTemplateProperty); } set { SetValue(PathTemplateProperty, value); } }
		/// <summary>
		/// Data template for "default" series item containers.
		/// MUST be a Image.
		/// </summary>
		public DataTemplate ImageTemplate { get { return (DataTemplate)GetValue(ImageTemplateProperty); } set { SetValue(ImageTemplateProperty, value); } }
		#endregion
		#region Storyboards
		/// <summary>
		/// Storyboard for "enter" chart.
		/// </summary>
		public Storyboard EnterAnimation { get { return (Storyboard)GetValue(EnterAnimationProperty); } set { SetValue(EnterAnimationProperty, value); } }
		/// <summary>
		/// Storyboard for "leave" chart.
		/// </summary>
		public Storyboard LeaveAnimation { get { return (Storyboard)GetValue(LeaveAnimationProperty); } set { SetValue(LeaveAnimationProperty, value); } }
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
		/// <summary>
		/// Identifies <see cref="TextBlockTemplate"/> DP.
		/// </summary>
		public static readonly DependencyProperty TextBlockTemplateProperty = DependencyProperty.Register(nameof(TextBlockTemplate), typeof(DataTemplate), typeof(ChartTheme), new PropertyMetadata(null));
		/// <summary>
		/// Identifies <see cref="PathTemplate"/> DP.
		/// </summary>
		public static readonly DependencyProperty PathTemplateProperty = DependencyProperty.Register(nameof(PathTemplate), typeof(DataTemplate), typeof(ChartTheme), new PropertyMetadata(null));
		/// <summary>
		/// Identifies <see cref="ImageTemplate"/> DP.
		/// </summary>
		public static readonly DependencyProperty ImageTemplateProperty = DependencyProperty.Register(nameof(ImageTemplate), typeof(DataTemplate), typeof(ChartTheme), new PropertyMetadata(null));
		/// <summary>
		/// Identifies <see cref="EnterAnimation"/> DP.
		/// </summary>
		public static readonly DependencyProperty EnterAnimationProperty = DependencyProperty.Register(nameof(EnterAnimation), typeof(Storyboard), typeof(ChartTheme), new PropertyMetadata(null));
		/// <summary>
		/// Identifies <see cref="LeaveAnimation"/> DP.
		/// </summary>
		public static readonly DependencyProperty LeaveAnimationProperty = DependencyProperty.Register(nameof(LeaveAnimation), typeof(Storyboard), typeof(ChartTheme), new PropertyMetadata(null));
		#endregion
	}
	#endregion
}
