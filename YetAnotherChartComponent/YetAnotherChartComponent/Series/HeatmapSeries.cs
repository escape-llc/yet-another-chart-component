using eScape.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region Evaluators2
	/// <summary>
	/// The package of <see cref="BindingEvaluator"/> in one place, evaluated once.
	/// </summary>
	internal class Evaluators2 {
		#region data
		/// <summary>
		/// Category 1 (x-axis) index path; MUST NOT be NULL.
		/// </summary>
		public readonly BindingEvaluator bc1;
		/// <summary>
		/// Category 2 (y-axis) index path.  MUST NOT be NULL.
		/// </summary>
		public readonly BindingEvaluator bc2;
		/// <summary>
		/// Value path.  MUST NOT be NULL.
		/// </summary>
		public readonly BindingEvaluator by;
		/// <summary>
		/// Value label path.  MAY be NULL.
		/// </summary>
		public readonly BindingEvaluator byl;
		#endregion
		#region properties
		/// <summary>
		/// Return whether the <see cref="bc2"/> evaluator got initialized.
		/// </summary>
		public bool IsValid { get { return bc1 != null &&  bc2 != null && by != null; } }
		#endregion
		#region ctors
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="category1Path">Path to the category 1 index; MUST NOT be NULL.</param>
		/// <param name="category2Path">Path to the category 2 index; MUST NOT be NULL.</param>
		/// <param name="valuePath">Path to the value; MUST NOT be NULL.</param>
		/// <param name="valueLabelPath">Path to the value label; MAY be NULL.</param>
		public Evaluators2(String category1Path, String category2Path, String valuePath, String valueLabelPath) {
			bc2 = !String.IsNullOrEmpty(category2Path) ? new BindingEvaluator(category2Path) : null;
			bc1 = !String.IsNullOrEmpty(category1Path) ? new BindingEvaluator(category1Path) : null;
			by = !String.IsNullOrEmpty(valuePath) ? new BindingEvaluator(valuePath) : null;
			byl = !String.IsNullOrEmpty(valueLabelPath) ? new BindingEvaluator(valueLabelPath) : null;
		}
		/// <summary>
		/// Copy ctor.
		/// </summary>
		/// <param name="be1">Category 1 evaluator.</param>
		/// <param name="be2">Category 2 evaluator.</param>
		/// <param name="by">Value evaluator.</param>
		/// <param name="byl">Value label evaluator.</param>
		public Evaluators2(BindingEvaluator be1, BindingEvaluator be2, BindingEvaluator by, BindingEvaluator byl) { this.bc1 = be1; this.bc2 = be2; this.by = by; this.byl = byl; }
		#endregion
		#region public
		/// <summary>
		/// Use the <see cref="bc1"/> evaluator to return the x-axis value, or index if it is NULL.
		/// </summary>
		/// <param name="ox">Object to evaluate.</param>
		/// <returns></returns>
		public int Category1For(object ox) {
			var valuex = (int)bc1.For(ox);
			return valuex;
		}
		public int Category2For(object ox) {
			var valuex = (int)bc2.For(ox);
			return valuex;
		}
		/// <summary>
		/// Wrapper to call <see cref="DataSeries.CoerceValue(object, BindingEvaluator)"/>.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public double ValueFor(object item) {
			var valuey = DataSeries.CoerceValue(item, by);
			return valuey;
		}
		/// <summary>
		/// Evaluate the label or NULL.
		/// <para/>
		/// If NULL, either <see cref="byl"/> was NULL, OR the evaluation result was NULL.
		/// </summary>
		/// <param name="item">Source instance.</param>
		/// <returns>Evaluated value or NULL.</returns>
		public object LabelFor(object item) {
			return byl?.For(item);
		}
		/// <summary>
		/// Force (non-NULL) results to be a <see cref="String"/>.
		/// <para/>
		/// If <see cref="String.Empty"/>, <see cref="byl"/> was NULL. If NULL, the evaluation result was NULL.
		/// </summary>
		/// <param name="item">Source instance.</param>
		/// <returns>Evaluated string, NULL, or <see cref="String.Empty"/>.</returns>
		public String LabelStringFor(object item) {
			return byl != null ? byl.For(item)?.ToString() : String.Empty;
		}
		#endregion
	}
	#endregion
	#region Styles
	#region ICategory2StyleContext
	/// <summary>
	/// Context for formatting a 2-dimensional value.
	/// </summary>
	public interface ICategory2StyleContext {
		/// <summary>
		/// First axis.
		/// </summary>
		IChartAxis Category1Axis { get; }
		/// <summary>
		/// Second axis.
		/// </summary>
		IChartAxis Category2Axis { get; }
		/// <summary>
		/// Value extents.
		/// </summary>
		IProvideSeriesValueExtents ValueExtents { get; }
		/// <summary>
		/// [0] value, [1] c1v, [2] c2v
		/// OR Length == 0.
		/// </summary>
		double[] Values { get; }
	}
	/// <summary>
	/// Default impl.
	/// </summary>
	public class Category2StyleContext : ICategory2StyleContext {
		#region properties
		/// <summary>
		/// First axis.
		/// </summary>
		public IChartAxis Category1Axis { get; private set; }
		/// <summary>
		/// Second axis.
		/// </summary>
		public IChartAxis Category2Axis { get; private set; }
		/// <summary>
		/// Value extents.
		/// </summary>
		public IProvideSeriesValueExtents ValueExtents { get; private set; }
		/// <summary>
		/// [0] value, [1] c1v, [2] c2v
		/// OR Length == 0.
		/// </summary>
		public double[] Values { get; private set; }
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="a1"></param>
		/// <param name="a2"></param>
		/// <param name="ipsve"></param>
		/// <param name="vxs"></param>
		public Category2StyleContext(IChartAxis a1, IChartAxis a2, IProvideSeriesValueExtents ipsve, double[] vxs) {
			Category1Axis = a1;
			Category2Axis = a2;
			ValueExtents = ipsve;
			Values = vxs;
		}
		#endregion
	}
	#endregion
	#region HeatmapStyleGenerator
	/// <summary>
	/// Abstract base for heatmap style generators.
	/// </summary>
	public abstract class HeatmapStyleGenerator {
		#region properties
		/// <summary>
		/// Style to base generated styles on.
		/// </summary>
		public Style BasedOn { get; set; }
		#endregion
		#region extension points
		/// <summary>
		/// Provide a style or NULL to revert.
		/// </summary>
		/// <param name="ic2sc">Value context.</param>
		/// <returns>Style or NULL.</returns>
		public abstract Style For(ICategory2StyleContext ic2sc);
		/// <summary>
		/// Reset any caches in use for a fresh run.
		/// </summary>
		public abstract void Reset();
		/// <summary>
		/// Generate the legend VM (with initial values).
		/// </summary>
		/// <param name="ic2sc">Context.</param>
		/// <param name="title">Series title.</param>
		/// <param name="pathstyle">Use for other style elements.</param>
		/// <returns>New instance, but with STABLE entries.</returns>
		public abstract IEnumerable<LegendBase> LegendFor(ICategory2StyleContext ic2sc, string title, Style pathstyle);
		/// <summary>
		/// Update legend VM.
		/// </summary>
		/// <param name="ic2sc">Context.</param>
		public abstract void UpdateLegend(ICategory2StyleContext ic2sc);
		#endregion
	}
	#endregion
	#region HeatmapStyle_Continuous
	/// <summary>
	/// Create a style that color varies over the given HSV range.
	/// Each color is allocated only once and styles shared.
	/// </summary>
	public class HeatmapStyle_Continuous : HeatmapStyleGenerator {
		#region properties
		/// <summary>
		/// HSV Hue for starting range. [0..360).
		/// </summary>
		public double HueStart { get; set; }
		/// <summary>
		/// Hue range.  Make negative to go "backwards".
		/// Default value is 300.
		/// </summary>
		public double HueRange { get; set; }
		/// <summary>
		/// HSV Saturation.
		/// Default value is 1.
		/// </summary>
		public double Saturation { get; set; }
		/// <summary>
		/// HSV Value.
		/// Default value is 1.
		/// </summary>
		public double Value { get; set; }
		/// <summary>
		/// Alpha value 0..255.
		/// Default value is 255.
		/// </summary>
		public byte Alpha { get; set; }
		/// <summary>
		/// Cache styles already in use.
		/// </summary>
		protected Dictionary<string, Style> StyleMap { get; set; } = new Dictionary<string, Style>();
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		public HeatmapStyle_Continuous() {
			HueRange = 300;
			Saturation = 1f;
			Value = 1f;
			Alpha = 255;
		}
		#endregion
		#region extensions
		/// <summary>
		/// Clear the map.
		/// </summary>
		public override void Reset() {
			StyleMap.Clear();
		}
		/// <summary>
		/// Create a style or NULL.
		/// </summary>
		/// <param name="ic2sc">Item context.</param>
		/// <returns>Style or NULL.</returns>
		public override Style For(ICategory2StyleContext ic2sc) {
			if (BasedOn == null) return null;
			InfoFor(ic2sc.ValueExtents.Minimum, ic2sc.ValueExtents.Maximum, ic2sc.Values[0], out int red, out int green, out int blue);
			string skey = $"r{red}_g{green}_b{blue}";
			if (StyleMap.TryGetValue(skey, out Style stx)) {
				return stx;
			}
			var brush = BrushFor(red, green, blue);
			var style = BasedOn.Override(Path.FillProperty, brush);
			StyleMap.Add(skey, style);
			return style;
		}
		/// <summary>
		/// Generate the legend VM (with initial values).
		/// </summary>
		/// <param name="ic2sc">Context.</param>
		/// <param name="title">Series title.</param>
		/// <param name="pathstyle">Use for other style elements.</param>
		/// <returns>New instance, but with STABLE entries.</returns>
		public override IEnumerable<LegendBase> LegendFor(ICategory2StyleContext ic2sc, string title, Style pathstyle) {
			return new LegendBase[] { EnsureLegend(ic2sc, title, pathstyle) };
		}
		/// <summary>
		/// Update legend VM.
		/// </summary>
		/// <param name="ic2sc">Context.</param>
		public override void UpdateLegend(ICategory2StyleContext ic2sc) {
			if (_legend == null) return;
			_legend.Fill = CreateBrush(ic2sc);
			_legend.Minimum = ic2sc.ValueExtents.Minimum;
			_legend.Maximum = ic2sc.ValueExtents.Maximum;
		}
		#endregion
		#region helpers
		/// <summary>
		/// Take incoming values and convert to HSV and then to RGB.
		/// </summary>
		/// <param name="min">Range min.</param>
		/// <param name="max">Range max.</param>
		/// <param name="value">Source value (SHOULD be between min/max).</param>
		/// <param name="red">Output Red.</param>
		/// <param name="green">Output Green.</param>
		/// <param name="blue">Output Blue.</param>
		void InfoFor(double min, double max, double value, out int red, out int green, out int blue) {
			double vx = double.IsNaN(value) ? min : value;
			double frac = vx / (double)(max - min + 1);
			frac = Math.Max(0.0, frac);
			frac = Math.Min(1.0, frac);
			double hue = HueStart + (frac * HueRange);
			// function will normalize hue [0..360)
			ColorSupport.HsvToRgb(hue, Saturation, Value, out red, out green, out blue);
		}
		Brush BrushFor(int red, int green, int blue) {
			return new SolidColorBrush(Color.FromArgb(Alpha, (byte)red, (byte)green, (byte)blue));
		}
		private LegendValueRange _legend;
		Legend EnsureLegend(ICategory2StyleContext ic2sc, string title, Style pathstyle) {
			var brush = CreateBrush(ic2sc);
			if (_legend != null) {
				_legend.Fill = brush;
				return _legend;
			}
			_legend = new LegendValueRange() { Title = title, Minimum = ic2sc.ValueExtents.Minimum, Maximum = ic2sc.ValueExtents.Maximum, Fill = brush, Stroke = pathstyle.Find<Brush>(Path.StrokeProperty) };
			return _legend;
		}
		Brush CreateBrush(ICategory2StyleContext ic2sc) {
			var gsc = new GradientStopCollection();
			InfoFor(ic2sc.ValueExtents.Minimum, ic2sc.ValueExtents.Maximum, ic2sc.ValueExtents.Minimum, out int red, out int green, out int blue);
			gsc.Add(new GradientStop() { Color = Color.FromArgb(Alpha, (byte)red, (byte)green, (byte)blue), Offset = 1 });
			InfoFor(ic2sc.ValueExtents.Minimum, ic2sc.ValueExtents.Maximum, ic2sc.ValueExtents.Maximum, out red, out green, out blue);
			gsc.Add(new GradientStop() { Color = Color.FromArgb(Alpha, (byte)red, (byte)green, (byte)blue), Offset = 0 });
			return new LinearGradientBrush(gsc, 90);
		}
		#endregion
	}
	#endregion
	#region HeatmapStyle_Discrete
	/// <summary>
	/// Half-open interval [Min,Max) for selecting a color.
	/// Setting one end to <see cref="double.NaN"/> creates an open-ended range of the corresponding type.
	/// </summary>
	public class DiscreteLegendEntry {
		/// <summary>
		/// Low end of range (GTE).
		/// </summary>
		public double Minimum { get; set; } = double.NaN;
		/// <summary>
		/// High end of range (LT).
		/// </summary>
		public double Maximum { get; set; } = double.NaN;
		/// <summary>
		/// The color for this range.
		/// </summary>
		public Brush Color { get; set; }
		/// <summary>
		/// The "name" of this discrete range.
		/// </summary>
		public string Title { get; set; }
		/// <summary>
		/// Calculated description of the range.
		/// </summary>
		public string Description {
			get {
				if (double.IsNaN(Minimum) && double.IsNaN(Maximum)) return "-";
				return double.IsNaN(Minimum) ? $"< {Maximum}" : ( double.IsNaN(Maximum) ? $">= {Minimum}" : $"[{Minimum} .. {Maximum})");
			}
		}
		/// <summary>
		/// Determine whether value is in this range.
		/// </summary>
		/// <param name="value">Candidate.</param>
		/// <returns></returns>
		public bool Compare(double value) {
			if (double.IsNaN(Minimum) && double.IsNaN(Maximum)) return false;
			if(double.IsNaN(Minimum)) {
				return value < Maximum;
			}
			else if(double.IsNaN(Maximum)) {
				return value >= Minimum;
			}
			else {
				return value >= Minimum && value < Maximum;
			}
		}
	}
	/// <summary>
	/// Required for XAML consumption.
	/// </summary>
	public class DiscreteLegendEntryCollection : List<DiscreteLegendEntry> { }
	/// <summary>
	/// Discrete version of heatmap style generator.
	/// This consists of a list of ranges that are checked in order.
	/// </summary>
	[ContentProperty(Name = nameof(Entries))]
	public class HeatmapStyle_Discrete : HeatmapStyleGenerator {
		/// <summary>
		/// The list of range entries.
		/// </summary>
		public DiscreteLegendEntryCollection Entries { get; set; } = new DiscreteLegendEntryCollection();
		/// <summary>
		/// Produce a style for the given context.
		/// </summary>
		/// <param name="ic2sc">Context.</param>
		/// <returns>Style or NULL.</returns>
		public override Style For(ICategory2StyleContext ic2sc) {
			if (BasedOn == null) return null;
			var match = Entries.SingleOrDefault(xx => xx.Compare(ic2sc.Values[0]));
			if (match == null) return null;
			var style = BasedOn.Override(Path.FillProperty, match.Color);
			return style;
		}
		List<LegendBase> _legend;
		/// <summary>
		/// Establish the legend for this style generator.
		/// </summary>
		/// <param name="ic2sc">Context.</param>
		/// <param name="title">Series title.</param>
		/// <param name="pathstyle">Use for other style properties as required.</param>
		/// <returns>Cached list enumerator.</returns>
		public override IEnumerable<LegendBase> LegendFor(ICategory2StyleContext ic2sc, string title, Style pathstyle) {
			if(_legend == null) {
				_legend = new List<LegendBase>();
				foreach(var ex in Entries) {
					var leg = new LegendValueRange() { Title = ex.Title, Minimum = ex.Minimum, Maximum = ex.Maximum, Fill = ex.Color, Stroke = BasedOn.Find<Brush>(Path.StrokeProperty) };
					_legend.Add(leg);
				}
			}
			return _legend;
		}
		/// <summary>
		/// Not used.  Once built, the cache is statically valid.
		/// </summary>
		public override void Reset() { }
		/// <summary>
		/// Nothing to update; NOT dependent on value range.
		/// </summary>
		/// <param name="ic2sc"></param>
		public override void UpdateLegend(ICategory2StyleContext ic2sc) { }
	}
	#endregion
	#endregion
	#region HeatmapSeries
	/// <summary>
	/// Heatmap is two category axis presenting rectangular areas of a third (axis-less) value, usually with color-coding of the values.
	/// </summary>
	public class HeatmapSeries : DataSeries, IDataSourceRenderer, IProvideLegend, IProvideSeriesValueExtents, IProvideSeriesItemValues, IRequireChartTheme, IRequireEnterLeave, IRequireTransforms, IRequireCategoryAxis2, IRequireAfterAxesFinalized {
		static readonly LogTools.Flag _trace = LogTools.Add("HeatmapSeries", LogTools.Level.Error);
		#region item state
		/// <summary>
		/// Item offers second dimension (use instead of Value).
		/// </summary>
		protected interface IProvideYValue {
			/// <summary>
			/// Second dimension.
			/// </summary>
			double YValue { get; }
		}
		/// <summary>
		/// Implementation for item state custom label.
		/// Provides placement information.
		/// TODO MAY have to reconstitute the "full" x-coordinates for <see cref="Placement"/> if those ever get used (currently not).
		/// This one is used when <see cref="DataSeriesWithValue.ValueLabelPath"/> is set.
		/// </summary>
		protected class SeriesItemState_Custom : ItemStateCustomWithPlacement<Path>, IProvideYValue {
			/// <summary>
			/// Second dimension.
			/// </summary>
			public double YValue { get; private set; }
			/// <summary>
			/// Extract the rectangle geometry and create placement.
			/// </summary>
			/// <returns></returns>
			protected override Placement CreatePlacement() { return new RectanglePlacement(Placement.DOWN_LEFT, DataFor().Rect); }
			internal SeriesItemState_Custom(int idx, double xv, double xvo, double c2v, double yv, object cs, Path ele) : base(idx, xv, xvo, yv, cs, ele, 0) { YValue = c2v; }
			RectangleGeometry DataFor() {
				if (Element.DataContext is GeometryWith2OffsetShim<RectangleGeometry>) {
					var p1 = new Point(XValue, YValue);
					var p2 = new Point(XValue + 1, YValue + 1);
					_trace.Verbose($"placement p1:{p1} p2:{p2} vx,vy:({XValue},{YValue}) v:{Value}");
					return new RectangleGeometry() { Rect = new Rect(p1, p2) };
				}
				return Element.Data as RectangleGeometry;
			}
		}
		/// <summary>
		/// Implementation for item state.
		/// Provides placement information.
		/// This one is used when <see cref="DataSeriesWithValue.ValueLabelPath"/> is NOT set.
		/// </summary>
		protected class SeriesItemState_Double : ItemStateWithPlacement<Path>, IProvideYValue {
			/// <summary>
			/// Second dimension.
			/// </summary>
			public double YValue { get; private set; }
			/// <summary>
			/// Extract the rectangle geometry and create placement.
			/// </summary>
			/// <returns></returns>
			protected override Placement CreatePlacement() { return new RectanglePlacement(Placement.DOWN_LEFT, DataFor().Rect); }
			internal SeriesItemState_Double(int idx, double xv, double xvo, double c2v, double yv, Path ele) : base(idx, xv, xvo, yv, ele, 0) { YValue = c2v; }
			RectangleGeometry DataFor() {
				if (Element.DataContext is GeometryWith2OffsetShim<RectangleGeometry>) {
					var p1 = new Point(XValue, YValue);
					var p2 = new Point(XValue + 1, YValue + 1);
					_trace.Verbose($"placement p1:{p1} p2:{p2} vx,vy:({XValue},{YValue}) v:{Value}");
					return new RectangleGeometry() { Rect = new Rect(p1, p2) };
				}
				return Element.Data as RectangleGeometry;
			}
		}
		#endregion
		#region properties
		/// <summary>
		/// Return current state as read-only.
		/// </summary>
		public IEnumerable<ISeriesItem> SeriesItemValues { get { return ItemState.AsReadOnly(); } }
		/// <summary>
		/// Holder for IRequireChartTheme interface.
		/// </summary>
		public IChartTheme Theme { get; set; }
		/// <summary>
		/// Component name of first <see cref="CategoryAxis"/>.
		/// Referenced component MUST implement <see cref="IChartAxis"/>.
		/// Referenced axis MUST be Horizontal.
		/// </summary>
		public String CategoryAxisName { get; set; }
		/// <summary>
		/// Component name of second <see cref="CategoryAxis"/>.
		/// Referenced component MUST implement <see cref="IChartAxis"/>.
		/// Referenced axis MUST be Vertical.
		/// </summary>
		public String CategoryAxis2Name { get; set; }
		/// <summary>
		/// Dereferenced category 1 axis.
		/// </summary>
		protected IChartAxis CategoryAxis1 { get; set; }
		/// <summary>
		/// Dereferenced category 2 axis.
		/// </summary>
		protected IChartAxis CategoryAxis2 { get; set; }
		/// <summary>
		/// Binding path to the <see cref="CategoryAxis"/> value.
		/// MAY be NULL, in which case the data-index is used instead.
		/// </summary>
		public String CategoryPath { get { return (String)GetValue(CategoryPathProperty); } set { SetValue(CategoryPathProperty, value); } }
		/// <summary>
		/// Binding path to the <see cref="CategoryAxis"/> value.
		/// MAY be NULL, in which case the data-index is used instead.
		/// </summary>
		public String Category2Path { get { return (String)GetValue(Category2PathProperty); } set { SetValue(Category2PathProperty, value); } }
		/// <summary>
		/// Binding path to the value axis value.
		/// MUST be non-NULL.
		/// </summary>
		public String ValuePath { get { return (String)GetValue(ValuePathProperty); } set { SetValue(ValuePathProperty, value); } }
		/// <summary>
		/// Binding path to the value axis label.
		/// MAY be NULL.
		/// If specified, this value will augment the one used for All Channels in <see cref="ISeriesItemValue"/>.
		/// </summary>
		public String ValueLabelPath { get { return (String)GetValue(ValueLabelPathProperty); } set { SetValue(ValueLabelPathProperty, value); } }
		/// <summary>
		/// The minimum value seen.
		/// </summary>
		public double Minimum { get; protected set; } = double.NaN;
		/// <summary>
		/// The maximum value seen.
		/// </summary>
		public double Maximum { get; protected set; } = double.NaN;
		/// <summary>
		/// The title for the values.
		/// </summary>
		public String Title { get { return (String)GetValue(TitleProperty); } set { SetValue(TitleProperty, value); } }
		/// <summary>
		/// Template to use for generated paths.
		/// If set, this overrides applying <see cref="PathStyle"/> (assumed <see cref="Style"/> inside the template).
		/// If this is not set, then <see cref="IChartTheme.PathTemplate"/> is used and <see cref="PathStyle"/> applied (if set).
		/// If Theme is not set, then <see cref="Path"/> is used (via ctor) and <see cref="PathStyle"/> applied (if set).
		/// </summary>
		public DataTemplate PathTemplate { get { return (DataTemplate)GetValue(PathTemplateProperty); } set { SetValue(PathTemplateProperty, value); } }
		/// <summary>
		/// The style to use for Path geometry.
		/// SHOULD be non-NULL.
		/// </summary>
		public Style PathStyle { get { return (Style)GetValue(PathStyleProperty); } set { SetValue(PathStyleProperty, value); } }
		/// <summary>
		/// Used to dynamically style the heatmap cells.
		/// </summary>
		public HeatmapStyleGenerator StyleGenerator { get; set; }
		/// <summary>
		/// The layer for components.
		/// </summary>
		protected IChartLayer Layer { get; set; }
		/// <summary>
		/// Data needed for current state.
		/// </summary>
		protected List<ItemState<Path>> ItemState { get; set; }
		/// <summary>
		/// Save the binding evaluators.
		/// TODO must re-create when any of the DPs change!
		/// </summary>
		Evaluators2 BindPaths { get; set; }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="PathTemplate"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty PathTemplateProperty = DependencyProperty.Register(
			nameof(PathTemplate), typeof(DataTemplate), typeof(HeatmapSeries), new PropertyMetadata(null)
		);
		/// <summary>
		/// Identifies <see cref="PathStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty PathStyleProperty = DependencyProperty.Register(
			nameof(PathStyle), typeof(Style), typeof(HeatmapSeries), new PropertyMetadata(null)
		);
		/// <summary>
		/// Identifies <see cref="CategoryPath"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty CategoryPathProperty = DependencyProperty.Register(
			nameof(CategoryPath), typeof(string), typeof(HeatmapSeries), new PropertyMetadata(null, new PropertyChangedCallback(PropertyChanged_ValueDirty))
		);
		/// <summary>
		/// Identifies <see cref="Category2Path"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty Category2PathProperty = DependencyProperty.Register(
			nameof(Category2Path), typeof(string), typeof(HeatmapSeries), new PropertyMetadata(null, new PropertyChangedCallback(PropertyChanged_ValueDirty))
		);
		/// <summary>
		/// Identifies <see cref="ValuePath"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ValuePathProperty = DependencyProperty.Register(
			nameof(ValuePath), typeof(string), typeof(HeatmapSeries), new PropertyMetadata(null, new PropertyChangedCallback(PropertyChanged_ValueDirty))
		);
		/// <summary>
		/// Identifies <see cref="ValueLabelPath"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ValueLabelPathProperty = DependencyProperty.Register(
			nameof(ValueLabelPath), typeof(string), typeof(HeatmapSeries), new PropertyMetadata(null, new PropertyChangedCallback(PropertyChanged_ValueDirty))
		);
		/// <summary>
		/// Identifies <see cref="Title"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
			nameof(Title), typeof(String), typeof(HeatmapSeries), new PropertyMetadata("Title")
		);
		#endregion
		#region ctor
		/// <summary>
		/// Default ctor.
		/// </summary>
		public HeatmapSeries() {
			ItemState = new List<ItemState<Path>>();
		}
		#endregion
		#region helpers
		/// <summary>
		/// Resolve axis references with error info.
		/// </summary>
		/// <param name="icrc">The context.</param>
		protected void EnsureAxes(IChartComponentContext icrc) {
			IChartErrorInfo icei = icrc as IChartErrorInfo;
			if (CategoryAxis2 == null) {
				if (!String.IsNullOrEmpty(CategoryAxis2Name)) {
					CategoryAxis2 = icrc.Find(CategoryAxis2Name) as IChartAxis;
					if (CategoryAxis2 == null) {
						icei?.Report(new ChartValidationResult(NameOrType(), $"Value axis '{CategoryAxis2Name}' was not found", new[] { nameof(CategoryAxis2), nameof(CategoryAxis2Name) }));
					}
				}
				else {
					icei?.Report(new ChartValidationResult(NameOrType(), $"Property '{nameof(CategoryAxis2)}' was not set", new[] { nameof(CategoryAxis2), nameof(CategoryAxis2) }));
				}
			}
			if (CategoryAxis1 == null) {
				if (!String.IsNullOrEmpty(CategoryAxisName)) {
					CategoryAxis1 = icrc.Find(CategoryAxisName) as IChartAxis;
					if (CategoryAxis1 == null) {
						icei?.Report(new ChartValidationResult(NameOrType(), $"Value axis '{CategoryAxisName}' was not found", new[] { nameof(CategoryAxis1), nameof(CategoryAxisName) }));
					}
				}
				else {
					icei?.Report(new ChartValidationResult(NameOrType(), $"Property '{nameof(CategoryAxisName)}' was not set", new[] { nameof(CategoryAxis1), nameof(CategoryAxisName) }));
				}
			}
		}
		/// <summary>
		/// Report an error if the <see cref="ValuePath"/> was not configured.
		/// </summary>
		/// <param name="iccc"></param>
		protected void EnsureValuePath(IChartComponentContext iccc) {
			if (String.IsNullOrEmpty(ValuePath)) {
				(iccc as IChartErrorInfo)?.Report(new ChartValidationResult(NameOrType(), $"{nameof(ValuePath)} was not set, no values will generate", new[] { nameof(ValuePath) }));
			}
		}
		/// <summary>
		/// Reset the value and category limits to <see cref="double.NaN"/>.
		/// Sets <see cref="ChartComponent.Dirty"/> = true.
		/// </summary>
		protected void ResetLimits() {
			Minimum = double.NaN; Maximum = double.NaN;
			//CategoryMinimum = double.NaN; CategoryMaximum = double.NaN;
			Dirty = true;
		}
		/// <summary>
		/// Update value and category limits.
		/// If a value is <see cref="double.NaN"/>, it is effectively ignored because NaN is NOT GT/LT ANY number, even itself.
		/// </summary>
		/// <param name="vx">Value.  MAY be NaN.</param>
		protected void UpdateLimits(double vx) {
			if (double.IsNaN(Minimum) || vx < Minimum) { Minimum = vx; }
			if (double.IsNaN(Maximum) || vx > Maximum) { Maximum = vx; }
		}
		/// <summary>
		/// Path factory for recycler.
		/// </summary>
		/// <param name="isp">Not used.</param>
		/// <returns></returns>
		Path CreatePath(ItemState<Path> isp) {
			var path = default(Path);
			if (PathTemplate != null) {
				path = PathTemplate.LoadContent() as Path;
			}
			else if (Theme?.PathTemplate != null) {
				path = Theme.PathTemplate.LoadContent() as Path;
				if (PathStyle != null) {
					BindTo(this, nameof(PathStyle), path, FrameworkElement.StyleProperty);
				}
			}
			return path;
		}
		/// <summary>
		/// Core element processing.
		/// The <see cref="RectangleGeometry"/> inside the <see cref="Path"/> is now location-invariant wrt x-axis.
		/// This means that during incremental updates, no re-calculation is required, only adjusting the <see cref="Canvas.LeftProperty"/>.
		/// </summary>
		/// <param name="index">Data index.</param>
		/// <param name="valuec1">C1 (x-axis) index.</param>
		/// <param name="valuec2">C2 (y-axis) index.</param>
		/// <param name="value">Cell value.</param>
		/// <param name="item"></param>
		/// <param name="recycler"></param>
		/// <param name="byl"></param>
		/// <returns></returns>
		ItemState<Path> ElementPipeline(int index, int valuec1, int valuec2, double value,  object item, Recycler<Path, ItemState<Path>> recycler, BindingEvaluator byl) {
			var path = recycler.Next(null);
			if (path == null) return null;
			var shim = new GeometryWith2OffsetShim<RectangleGeometry>() {
				// MUST use invariant geometry here so it can translate.
				PathData = new RectangleGeometry() { Rect = new Rect(new Point(0, 0), new Point(1, 1)) }
			};
			path.Item2.DataContext = shim;
			// connect the shim to template root element's Visibility
			BindTo(shim, nameof(shim.Visibility), path.Item2, UIElement.VisibilityProperty);
			BindTo(shim, nameof(shim.Offset), path.Item2, Canvas.LeftProperty);
			BindTo(shim, nameof(shim.Offset2), path.Item2, Canvas.TopProperty);
			if (byl == null) {
				return new SeriesItemState_Double(index, valuec1, 0, valuec2, value, path.Item2);
			}
			else {
				var cs = byl.For(item);
				return new SeriesItemState_Custom(index, valuec1, 0, valuec2, value, cs, path.Item2);
			}
		}
		#endregion
		#region IProvideLegend
		private LegendBase _legend;
		IEnumerable<LegendBase> IProvideLegend.LegendItems {
			get {
				if(StyleGenerator != null) {
					return StyleGenerator.LegendFor(new Category2StyleContext(CategoryAxis1, CategoryAxis2, this, new double[0]), Title, PathStyle);
				}
				if (_legend == null) _legend = Legend(); return new[] { _legend };
			}
		}
		Legend Legend() {
			return new Legend() { Title = Title, Fill = PathStyle.Find<Brush>(Path.FillProperty), Stroke = PathStyle.Find<Brush>(Path.StrokeProperty) };
		}
		#endregion
		#region IRequireEnterLeave
		/// <summary>
		/// Initialize after entering VT.
		/// </summary>
		/// <param name="icelc"></param>
		void IRequireEnterLeave.Enter(IChartEnterLeaveContext icelc) {
			EnsureAxes(icelc as IChartComponentContext);
			EnsureValuePath(icelc as IChartComponentContext);
			Layer = icelc.CreateLayer();
			_trace.Verbose($"{Name} enter c2(v):{CategoryAxis2Name} {CategoryAxis2} c1:{CategoryAxisName} {CategoryAxis1} d:{DataSourceName}");
			if (PathTemplate == null) {
				if (Theme?.PathTemplate == null) {
					(icelc as IChartErrorInfo)?.Report(new ChartValidationResult(NameOrType(), $"No {nameof(PathTemplate)} and {nameof(Theme.PathTemplate)} was not found", new[] { nameof(PathTemplate), nameof(Theme.PathTemplate) }));
				}
			}
			AssignFromRef(icelc as IChartErrorInfo, NameOrType(), nameof(PathStyle), nameof(Theme.PathColumnSeries),
				PathStyle == null, Theme != null, Theme.PathColumnSeries != null,
				() => PathStyle = Theme.PathColumnSeries
			);
			BindPaths = new Evaluators2(CategoryPath, Category2Path, ValuePath, ValueLabelPath);
			if (!BindPaths.IsValid) {
				(icelc as IChartErrorInfo)?.Report(new ChartValidationResult(NameOrType(), $"ValuePath: must be specified", new[] { nameof(ValuePath) }));
			}
		}
		/// <summary>
		/// Undo effects of Enter().
		/// </summary>
		/// <param name="icelc"></param>
		void IRequireEnterLeave.Leave(IChartEnterLeaveContext icelc) {
			_trace.Verbose($"{Name} leave");
			BindPaths = null;
			CategoryAxis2 = null;
			CategoryAxis1 = null;
			icelc.DeleteLayer(Layer);
			Layer = null;
		}
		#endregion
		#region IRequireTransforms
		/// <summary>
		/// Adjust transforms for the various components.
		/// Geometry: scaled to actual values in cartesian coordinates as indicated by axes.
		/// </summary>
		/// <param name="icrc"></param>
		void IRequireTransforms.Transforms(IChartRenderContext icrc) {
			if (CategoryAxis1 == null || CategoryAxis2 == null) return;
			if (ItemState.Count == 0) return;
			// TODO quad depends on orientations of both axes this is assuming default orientations
			var mat = MatrixSupport.DataArea(CategoryAxis1, CategoryAxis2, icrc.SeriesArea, 4);
			var matmp = MatrixSupport.Multiply(mat.Item1, mat.Item2);
			_trace.Verbose($"{Name} matmp:{matmp} clip:{icrc.SeriesArea}");
			var mt = new MatrixTransform() { Matrix = matmp };
			foreach (var state in ItemState) {
				if (state.Element.DataContext is GeometryWith2OffsetShim<RectangleGeometry> gs) {
					gs.GeometryTransform = mt;
					var output = matmp.Transform(new Point(state.XValue, (state as IProvideYValue).YValue));
					gs.Offset = output.X - icrc.SeriesArea.Left;
					gs.Offset2 = output.Y - icrc.SeriesArea.Top;
				}
				else {
					state.Element.Data.Transform = mt;
				}
				if (ClipToDataRegion) {
					var cg = new RectangleGeometry() { Rect = icrc.SeriesArea };
					state.Element.Clip = cg;
				}
			}
		}
		#endregion
		#region IRequireAfterAxesFinalized
		void IRequireAfterAxesFinalized.AxesFinalized(IChartRenderContext icrc) {
			if (icrc.Type == RenderType.TransformsOnly) return;
			var mat = MatrixSupport.DataArea(CategoryAxis1, CategoryAxis2, icrc.SeriesArea, 4);
			var matmp = MatrixSupport.Multiply(mat.Item1, mat.Item2);
			_trace.Verbose($"{Name} matmp:{matmp} clip:{icrc.SeriesArea}");
			var mt = new MatrixTransform() { Matrix = matmp };
			StyleGenerator?.Reset();
			foreach (var istate in ItemState) {
				if (istate.Element.DataContext is GeometryWith2OffsetShim<RectangleGeometry> gs) {
					gs.GeometryTransform = mt;
				}
				else {
					istate.Element.Data.Transform = mt;
				}
				if(StyleGenerator != null) {
					var style = StyleGenerator.For(new Category2StyleContext(CategoryAxis1, CategoryAxis2, this, new double[3] { istate.Value, istate.XValue, (istate as IProvideYValue).YValue }));
					if(style != null) {
						istate.Element.Style = style;
					}
					else {
						if (PathStyle != null) {
							BindTo(this, nameof(PathStyle), istate.Element, FrameworkElement.StyleProperty);
						}
					}
				}
			}
			StyleGenerator?.UpdateLegend(new Category2StyleContext(CategoryAxis1, CategoryAxis2, this, new double[0]));
		}
		#endregion
		#region IDataSourceRenderer
		internal class RenderState_Heatmap<SIS, EL> : RenderStateCore<SIS, EL> where SIS : class where EL : FrameworkElement {
			/// <summary>
			/// Evaluators for core values.
			/// </summary>
			internal readonly Evaluators2 evs;
			/// <summary>
			/// Ctor.
			/// </summary>
			/// <param name="state">Starting state; SHOULD be empty.</param>
			/// <param name="rc">The recycler.</param>
			/// <param name="evs">Evaluators.</param>
			internal RenderState_Heatmap(List<SIS> state, Recycler<EL, SIS> rc, Evaluators2 evs) : base(state, rc) {
#pragma warning disable IDE0016 // Use 'throw' expression
				if (evs == null) throw new ArgumentNullException(nameof(evs));
				if (evs.by == null) throw new ArgumentNullException(nameof(evs.by));
#pragma warning restore IDE0016 // Use 'throw' expression
				this.evs = evs;
			}
		}
		object IDataSourceRenderer.Preamble(IChartRenderContext icrc) {
			if (CategoryAxis1 == null || CategoryAxis2 == null) return null;
			if (BindPaths == null || !BindPaths.IsValid) return null;
			ResetLimits();
			var paths = ItemState.Select(ms => ms.Element).Where(el => el != null);
			var recycler = new Recycler<Path, ItemState<Path>>(paths, CreatePath);
			return new RenderState_Heatmap<ItemState<Path>, Path>(new List<ItemState<Path>>(), recycler, BindPaths);
		}
		void IDataSourceRenderer.Render(object state, int index, object item) {
			var st = state as RenderState_Heatmap<ItemState<Path>, Path>;
			var valuec1 = st.evs.Category1For(item);
			var valuec2 = st.evs.Category2For(item);
			var value = st.evs.ValueFor(item);
			st.ix = index;
			// short-circuit if it's NaN
			if (double.IsNaN(value)) {
				return;
			}
			UpdateLimits(value);
			var istate = ElementPipeline(index, valuec1, valuec2, value, item, st.recycler, st.evs.byl);
			if (istate != null) st.itemstate.Add(istate);
		}
		void IDataSourceRenderer.RenderComplete(object state) {}
		void IDataSourceRenderer.Postamble(object state) {
			var st = state as RenderState_Heatmap<ItemState<Path>, Path>;
			ItemState = st.itemstate;
			Layer.Remove(st.recycler.Unused);
			Layer.Add(st.recycler.Created);
			Dirty = false;
		}
		#endregion
	}
	#endregion
}