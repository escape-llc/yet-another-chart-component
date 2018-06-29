using eScape.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace eScapeLLC.UWP.Charts {
	#region ILabelSelectorContext
	/// <summary>
	/// Context passed to the <see cref="IValueConverter"/> for <see cref="Style"/> selection.
	/// </summary>
	public interface ILabelSelectorContext {
		/// <summary>
		/// The source of the item values.
		/// </summary>
		IProvideSeriesItemValues Source { get; }
		/// <summary>
		/// The value in question.
		/// </summary>
		ISeriesItemValue ItemValue { get; }
	}
	#endregion
	#region ValueLabels
	/// <summary>
	/// Decoration that creates value labels.
	/// </summary>
	public class ValueLabels : ChartComponent, IRequireChartTheme, IRequireEnterLeave, IRequireRender, IRequireTransforms {
		static LogTools.Flag _trace = LogTools.Add("ValueLabels", LogTools.Level.Error);
		#region SeriesItemState
		/// <summary>
		/// The item state.
		/// </summary>
		protected class SeriesItemState : ItemStateDependent<FrameworkElement> {
			internal Point Direction { get; private set; }
			internal Point CanvasLocation { get; private set; }
			internal object CustomValue { get; private set; }
			internal SeriesItemState(ISeriesItem isi, ISeriesItemValueDouble isivd, Point loc, Point dir, object cv)
			: base(isi, isivd, loc.X, loc.Y, null) {
				Direction = dir;
				CustomValue = cv;
			}
			/// <summary>
			/// Set <see cref="CanvasLocation"/> and the canvas properties based on actual size.
			/// </summary>
			/// <param name="matx">Use to calculate the new <see cref="CanvasLocation"/>.</param>
			/// <param name="offs">Additional offset from <see cref="CanvasLocation"/> in direction of <see cref="Direction"/>.</param>
			/// <param name="rt">Render type.  Used to trigger invalidation of the element.</param>
			internal void Locate(Matrix matx, Point offs, RenderType rt) {
				CanvasLocation = matx.Transform(new Point(XValueAfterOffset, Value));
				Locate(offs);
				if (rt != RenderType.TransformsOnly) {
					// doing render so (try to) trigger the SizeChanged handler
					Element.InvalidateMeasure();
					Element.InvalidateArrange();
				}
			}
			/// <summary>
			/// Re-locate based on current <see cref="CanvasLocation"/> and actual size.
			/// </summary>
			/// <param name="offs">Additional offset from <see cref="CanvasLocation"/> in direction of <see cref="Direction"/>.</param>
			internal void Locate(Point offs) {
				if (Element == null) return;
				var hw = Element.ActualWidth / 2;
				var hh = Element.ActualHeight / 2;
				Element.SetValue(Canvas.LeftProperty, CanvasLocation.X - hw + hw * offs.X * Direction.X);
				Element.SetValue(Canvas.TopProperty, CanvasLocation.Y - hh + hh * offs.Y * Direction.Y);
			}
			/// <summary>
			/// Recalculate placement values and update state.
			/// </summary>
			/// <param name="offs">Placement offset.</param>
			/// <param name="aoffset">Axis unit offset.</param>
			internal void UpdatePlacement(Point offs, double aoffset) {
				var pmt = GetPlacement(ValueSource, offs, aoffset);
				XOffset = pmt.Item1.X;
				Value = pmt.Item1.Y;
				Direction = pmt.Item2;
			}
		}
		#endregion
		#region SelectorContext
		/// <summary>
		/// Default implementation of <see cref="ILabelSelectorContext"/>.
		/// </summary>
		protected class SelectorContext : ILabelSelectorContext {
			/// <summary>
			/// <see cref="ILabelSelectorContext.Source"/>.
			/// </summary>
			public IProvideSeriesItemValues Source { get; private set; }
			/// <summary>
			/// <see cref="ILabelSelectorContext.ItemValue"/>.
			/// </summary>
			public ISeriesItemValue ItemValue { get; private set; }
			/// <summary>
			/// Ctor.
			/// </summary>
			/// <param name="ipsiv"></param>
			/// <param name="isiv"></param>
			public SelectorContext(IProvideSeriesItemValues ipsiv, ISeriesItemValue isiv) { Source = ipsiv; ItemValue = isiv; }
		}
		#endregion
		#region properties
		/// <summary>
		/// Holder for IRequireChartTheme interface.
		/// </summary>
		public IChartTheme Theme { get; set; }
		/// <summary>
		/// The name of the source in the Components collection.
		/// The item values are obtained from this series.
		/// </summary>
		public String SourceName { get { return (String)GetValue(SourceNameProperty); } set { SetValue(SourceNameProperty, value); } }
		/// <summary>
		/// The value channel to display values for.
		/// </summary>
		public int ValueChannel { get { return (int)GetValue(ValueChannelProperty); } set { SetValue(ValueChannelProperty, value); } }
		/// <summary>
		/// The style to apply to (non-templated) labels.
		/// When using <see cref="LabelFormatter"/> this style can be overriden.
		/// </summary>
		public Style LabelStyle { get { return (Style)GetValue(LabelStyleProperty); } set { SetValue(LabelStyleProperty, value); } }
		/// <summary>
		/// If set, the template to use for labels.
		/// This overrides <see cref="LabelStyle"/>.
		/// If this is not set, then <see cref="TextBlock"/>s are used and <see cref="LabelStyle"/> applied to them.
		/// </summary>
		public DataTemplate LabelTemplate { get { return (DataTemplate)GetValue(LabelTemplateProperty); } set { SetValue(LabelTemplateProperty, value); } }
		/// <summary>
		/// Alternate format string for labels.
		/// </summary>
		public String LabelFormatString { get; set; }
		/// <summary>
		/// Offset in Category axis offset in [0..1].
		/// Use with ColumnSeries to get the "points" to align with the column(s) layout in their cells.
		/// </summary>
		public double CategoryAxisOffset { get; set; }
		/// <summary>
		/// LabelOffset is translation from the "center" of the TextBlock.
		/// Units are PX coordinates, in Half-dimension based on TextBlock size.
		/// Y-up is negative.
		/// Default value is (0,0).
		/// </summary>
		public Point LabelOffset { get; set; } = new Point(0, 0);
		/// <summary>
		/// Placment offset is translation from "center" of a region.
		/// Units are WORLD coordinates.
		/// Y-up is positive.
		/// Default value is (0,0).
		/// </summary>
		public Point PlacementOffset { get; set; } = new Point(0, 0);
		/// <summary>
		/// Converter to use as the element <see cref="FrameworkElement.Style"/> and <see cref="TextShim.Text"/> selector.
		/// These are already set to their "standard" values before this is called, so it MAY selectively opt out of setting them.
		/// The <see cref="IValueConverter.Convert"/> targetType parameter is used to determine which value is requested.
		/// Uses <see cref="Tuple{Style,String}"/> for style/label override.  Return a new value or NULL (in each "slot") to opt in/out.
		/// </summary>
		public IValueConverter LabelFormatter { get; set; }
		/// <summary>
		/// Converter to use as the label creation selector.
		/// If it returns True, the label is created.
		/// The <see cref="IValueConverter.Convert"/> targetType parameter is <see cref="bool"/>.
		/// SHOULD return a <see cref="bool"/> but MAY return NULL/not-NULL.
		/// </summary>
		public IValueConverter LabelSelector { get; set; }
		/// <summary>
		/// Whether to create layer with composition animations enabled.
		/// </summary>
		public bool UseImplicitAnimations { get; set; }
		/// <summary>
		/// Label entry storyboard.
		/// </summary>
		public Storyboard EnterStoryboard { get; set; }
		/// <summary>
		/// Label exit storyboard.
		/// </summary>
		public Storyboard LeaveStoryboard { get; set; }
		/// <summary>
		/// Dereferenced value axis.
		/// </summary>
		protected IChartAxis ValueAxis { get; set; }
		/// <summary>
		/// Dereferenced category axis.
		/// </summary>
		protected IChartAxis CategoryAxis { get; set; }
		/// <summary>
		/// Dereferenced component to interrogate for values.
		/// </summary>
		protected ChartComponent Source { get; set; }
		/// <summary>
		/// The layer for components.
		/// </summary>
		protected IChartLayer Layer { get; set; }
		/// <summary>
		/// Current item state.
		/// </summary>
		protected List<SeriesItemState> ItemState { get; set; }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="ValueChannel"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ValueChannelProperty = DependencyProperty.Register(
			nameof(ValueChannel), typeof(int), typeof(ValueLabels), new PropertyMetadata(0, new PropertyChangedCallback(PropertyChanged_ValueDirty))
		);
		/// <summary>
		/// Identifies <see cref="SourceName"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty SourceNameProperty = DependencyProperty.Register(
			nameof(SourceName), typeof(string), typeof(ValueLabels), new PropertyMetadata(null, new PropertyChangedCallback(PropertyChanged_ValueDirty))
		);
		/// <summary>
		/// Identifies <see cref="LabelStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty LabelStyleProperty = DependencyProperty.Register(
			nameof(LabelStyle), typeof(Style), typeof(ValueLabels), new PropertyMetadata(null)
		);
		/// <summary>
		/// Identifies <see cref="LabelTemplate"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty LabelTemplateProperty = DependencyProperty.Register(
			nameof(LabelTemplate), typeof(DataTemplate), typeof(ValueLabels), new PropertyMetadata(null)
		);
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		public ValueLabels() {
			ItemState = new List<SeriesItemState>();
		}
		#endregion
		#region helpers
		/// <summary>
		/// Resolve component and axis references.
		/// </summary>
		/// <param name="icrc">The context.</param>
		protected void EnsureComponents(IChartComponentContext icrc) {
			if (LabelTemplate == null) {
				if (Theme?.TextBlockTemplate == null) {
					if (icrc is IChartErrorInfo icei) {
						icei.Report(new ChartValidationResult(NameOrType(), $"No {nameof(LabelTemplate)} and {nameof(Theme.TextBlockTemplate)} was not found", new[] { nameof(LabelTemplate), nameof(Theme.TextBlockTemplate) }));
					}
				}
			}
			if (Source == null && !String.IsNullOrEmpty(SourceName)) {
				Source = icrc.Find(SourceName);
			} else {
				if (icrc is IChartErrorInfo icei) {
					icei.Report(new ChartValidationResult(NameOrType(), $"Source '{SourceName}' was not found", new[] { nameof(Source), nameof(SourceName) }));
				}
			}
			if (Source == null) {
				if (icrc is IChartErrorInfo icei) {
					icei.Report(new ChartValidationResult(NameOrType(), $"Source '{SourceName}' lookup failed; no other components can resolve", new[] { nameof(Source), nameof(SourceName) }));
				}
				return;
			}
			else {
				if(!(Source is IProvideSeriesItemValues)) {
					if (icrc is IChartErrorInfo icei) {
						icei.Report(new ChartValidationResult(Source.NameOrType(), $"Source '{SourceName}' does not provide values; no labels will generate", new[] { nameof(Source), nameof(SourceName) }));
					}
					return;
				}
			}
			if (Source is IProvideValueExtents ipve) {
				if (ValueAxis == null && !String.IsNullOrEmpty(ipve.ValueAxisName)) {
					ValueAxis = icrc.Find(ipve.ValueAxisName) as IChartAxis;
				} else {
					if (icrc is IChartErrorInfo icei) {
						icei.Report(new ChartValidationResult(Source.NameOrType(), $"Value axis '{ipve.ValueAxisName}' was not found", new[] { nameof(ValueAxis), nameof(ipve.ValueAxisName) }));
					}
				}
			}
			if (Source is IRequireCategoryAxis ipce) {
				if (CategoryAxis == null && !String.IsNullOrEmpty(ipce.CategoryAxisName)) {
					CategoryAxis = icrc.Find(ipce.CategoryAxisName) as IChartAxis;
				} else {
					if (icrc is IChartErrorInfo icei) {
						icei.Report(new ChartValidationResult(Source.NameOrType(), $"Category axis '{ipce.CategoryAxisName}' was not found", new[] { nameof(CategoryAxis), nameof(ipce.CategoryAxisName) }));
					}
				}
			}
		}
		/// <summary>
		/// Hit all the view models to update visibility.
		/// </summary>
		/// <param name="viz"></param>
		protected void UpdateVisibility(Visibility viz) {
			foreach(var st in ItemState) {
				if(st.Element.DataContext is DataTemplateShim dts) {
					dts.Visibility = viz;
				}
			}
		}
		/// <summary>
		/// Undo any bookkeeping done in <see cref="ElementPipeline"/>.
		/// </summary>
		/// <param name="fes"></param>
		protected void TeardownElements(IEnumerable<FrameworkElement> fes) {
			foreach (var fe in fes) {
				fe.DataContext = null;
				fe.SizeChanged -= Element_SizeChanged;
			}
		}
		/// <summary>
		/// Get the transform from <see cref="IProvideCustomTransform"/>, or based on axes, whichever hits first.
		/// If there's no <see cref="ValueAxis"/> return Identity matrix.
		/// </summary>
		/// <param name="icrc">Use for the area.</param>
		/// <returns>Matrix or DEFAULT.</returns>
		protected Matrix ObtainMatrix(IChartRenderContext icrc) {
			if (Source is IProvideCustomTransform ipct) {
				return ipct.TransformFor(icrc.Area);
			}
			if (ValueAxis == null) return default(Matrix);
			var matx = CategoryAxis != null ? MatrixSupport.TransformFor(icrc.Area, CategoryAxis, ValueAxis) : MatrixSupport.TransformFor(icrc.Area, ValueAxis);
			return matx;
		}
		/// <summary>
		/// Element factory for recycler.
		/// Comes from a <see cref="DataTemplate"/> if the <see cref="LabelTemplate"/> was set.
		/// Otherwise comes from <see cref="IChartTheme.TextBlockTemplate"/>.
		/// </summary>
		/// <param name="isiv">Item value.</param>
		/// <returns>New element or NULL.</returns>
		FrameworkElement CreateElement(ISeriesItemValueDouble isiv) {
			var fe = default(FrameworkElement);
			if (LabelTemplate != null) {
				fe = LabelTemplate.LoadContent() as FrameworkElement;
			} else if (Theme.TextBlockTemplate != null) {
				fe = Theme.TextBlockTemplate.LoadContent() as FrameworkElement;
				if (LabelStyle != null) {
					BindTo(this, nameof(LabelStyle), fe, FrameworkElement.StyleProperty);
				}
			}
			if(fe != null) {
				// complete configuration
				var shim = CreateShim(isiv);
				// connect the shim to template root element's Visibility
				BindTo(shim, nameof(Visibility), fe, UIElement.VisibilityProperty);
				fe.DataContext = shim;
				fe.SizeChanged += Element_SizeChanged;
			}
			return fe;
		}
		/// <summary>
		/// Create the shim along with setting the initial (default) text value.
		/// </summary>
		/// <param name="isiv">Source value.</param>
		/// <returns>New instance.</returns>
		DataTemplateShim CreateShim(ISeriesItemValueDouble isiv) {
			var txt = isiv.Value.ToString(String.IsNullOrEmpty(LabelFormatString) ? "G" : LabelFormatString);
			if(isiv is ISeriesItemValueCustom isivc) {
				return new ObjectShim() { Visibility = Visibility, Text = txt, CustomValue = isivc.CustomValue };
			}
			return new TextShim() { Visibility = Visibility, Text = txt };
		}
		/// <summary>
		/// Extract value info that matches our <see cref="ValueChannel"/>.
		/// </summary>
		/// <param name="siv">Candidate.</param>
		/// <returns>Matching value or NULL.</returns>
		ISeriesItemValueDouble ValueFor(ISeriesItem siv) {
			ISeriesItemValue target = null;
			if (siv is ISeriesItemValue isiv) {
				if (isiv.Channel == ValueChannel) {
					target = isiv;
				}
			} else if (siv is ISeriesItemValues isivs) {
				target = isivs.YValues.SingleOrDefault(yv => yv.Channel == ValueChannel);
			}
			return target as ISeriesItemValueDouble;
		}
		/// <summary>
		/// Extract placement info.
		/// </summary>
		/// <param name="isivd">The item.</param>
		/// <param name="offset">Placement offset.</param>
		/// <param name="xo">Default x-offset, depending on placement found.</param>
		/// <returns>Item1=location(.X=XOffset,.Y=Value);Item2=direction.</returns>
		static Tuple<Point,Point> GetPlacement(ISeriesItemValueDouble isivd, Point offset, double xo) {
			var pmt = (isivd as IProvidePlacement)?.Placement;
			switch (pmt) {
			case RectanglePlacement rp:
				var pt = rp.Transform(offset);
				_trace.Verbose($"rp c:{rp.Center} d:{rp.Direction} hd:{rp.HalfDimensions} pt:{pt}");
				return new Tuple<Point,Point>(new Point(xo, pt.Y), rp.Direction);
			case MidpointPlacement mp:
				var pt2 = mp.Transform(offset);
				// convert into XOffset!
				pt2.X = pt2.X - (isivd as ISeriesItem).XValue;
				_trace.Verbose($"mp {mp.Midpoint} d:{mp.Direction} hd:{mp.HalfDimension} pt:{pt2}");
				return new Tuple<Point, Point>(pt2, mp.Direction);
			default:
				return new Tuple<Point, Point>(new Point(xo, isivd.Value), Placement.UP_RIGHT);
			}
		}
		/// <summary>
		/// Create the state only; defer UI creation.
		/// </summary>
		/// <param name="siv">Source.  SHOULD be <see cref="ISeriesItemValueDouble"/>.</param>
		/// <returns>New instance or NULL.</returns>
		SeriesItemState CreateState(ISeriesItem siv) {
			ISeriesItemValueDouble target = ValueFor(siv);
			if (target != null && !double.IsNaN(target.Value)) {
				var place = GetPlacement(target, PlacementOffset, CategoryAxisOffset);
				var cv = target is ISeriesItemValueCustom isivc ? isivc.CustomValue : null;
				return new SeriesItemState(siv, target, place.Item1, place.Item2, cv);
			}
			return null;
		}
		/// <summary>
		/// If <see cref="LabelFormatter"/> and Element are defined, evaluate the formatter
		/// and apply the results.
		/// </summary>
		/// <param name="ipsiv">Used for evaluation context.</param>
		/// <param name="state">Target state.  The Element MUST be assigned for any effect.</param>
		void ApplyFormatter(IProvideSeriesItemValues ipsiv, SeriesItemState state) {
			if (LabelFormatter == null) return;
			if (state.Element == null) return;
			var ctx = new SelectorContext(ipsiv, state.ValueSource);
			// TODO could call for typeof(object) and replace CustomValue
			var format = LabelFormatter.Convert(ctx, typeof(Tuple<Style, String>), null, System.Globalization.CultureInfo.CurrentUICulture.Name);
			if (format is Tuple<Style, String> ovx) {
				if (ovx.Item1 != null) {
					// TODO use error control because style may not match the template
					state.Element.Style = ovx.Item1;
				}
				if (ovx.Item2 != null) {
					if (state.Element.DataContext is TextShim ts) {
						ts.Text = ovx.Item2;
					}
				}
			}
		}
		/// <summary>
		/// If <see cref="LabelSelector"/> is defined, evaluate it and return the results.
		/// </summary>
		/// <param name="ipsiv">Used for evaluation context.</param>
		/// <param name="target">Item value.</param>
		/// <returns>true: select for label; false: not selected.</returns>
		bool ApplySelector(IProvideSeriesItemValues ipsiv, ISeriesItemValueDouble target) {
			if (LabelSelector == null) return true;
			// apply
			var ctx = new SelectorContext(ipsiv, target);
			var ox = LabelSelector.Convert(ctx, typeof(bool), null, System.Globalization.CultureInfo.CurrentUICulture.Name);
			if (ox is bool bx) {
				return bx;
			} else {
				return ox != null;
			}
		}
		/// <summary>
		/// Re-evaluate the dynamic logic for given item.
		/// Directly manipulates item creation and the layer.
		/// </summary>
		/// <param name="ipsiv">Used for evaluation context.</param>
		/// <param name="state">Target state.</param>
		void UpdateElement(IProvideSeriesItemValues ipsiv, SeriesItemState state) {
			var createit = ApplySelector(ipsiv, state.ValueSource);
			// compare decision with current state
			if(state.Element == null) {
				if(createit) {
					// need one
					var el = CreateElement(state.ValueSource);
					state.Element = el;
					ApplyFormatter(ipsiv, state);
					Layer.Add(el);
				}
			} else {
				if(!createit) {
					// remove this one
					TeardownElements(new[] { state.Element });
					Layer.Remove(state.Element);
					state.Element = null;
				}
				else {
					// just re-style
					ApplyFormatter(ipsiv, state);
				}
			}
		}
		/// <summary>
		/// Re-initialize a recycled element for a new application.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="fe"></param>
		/// <param name="shim"></param>
		void RecycleElement(ISeriesItemValueDouble target, FrameworkElement fe, TextShim shim) {
			// recycling; update values
			var txt = target.Value.ToString(String.IsNullOrEmpty(LabelFormatString) ? "G" : LabelFormatString);
			shim.Visibility = Visibility;
			shim.Text = txt;
			if (shim is ObjectShim oshim && target is ISeriesItemValueCustom isivc2) {
				oshim.CustomValue = isivc2.CustomValue;
			}
			// restore binding if we are using a LabelFormatter
			if (LabelFormatter != null && LabelStyle != null) {
				BindTo(this, nameof(LabelStyle), fe, FrameworkElement.StyleProperty);
			}
		}
		/// <summary>
		/// Item state factory method.
		/// </summary>
		/// <param name="ipsiv">Used in contexts.</param>
		/// <param name="siv">The item to track.</param>
		/// <param name="recycler">For element creation.</param>
		/// <returns>New instance or NULL.</returns>
		SeriesItemState ElementPipeline(IProvideSeriesItemValues ipsiv, ISeriesItem siv, Recycler<FrameworkElement, ISeriesItemValueDouble> recycler) {
			var sis = CreateState(siv);
			if (sis != null) {
				if (ApplySelector(ipsiv, sis.ValueSource)) {
					var el = recycler.Next(sis.ValueSource);
					if (!el.Item1 && el.Item2.DataContext is TextShim shim) {
						RecycleElement(sis.ValueSource, el.Item2, shim);
					}
					sis.Element = el.Item2;
					ApplyFormatter(ipsiv, sis);
				}
				return sis;
			}
			return null;
		}
		/// <summary>
		/// Handle bookkeeping for incremental add.
		/// </summary>
		/// <param name="startAt">Starting index.</param>
		/// <param name="items">Items affected.</param>
		void IncrementalAdd(int startAt, IList<ISeriesItem> items) {
			var reproc = new List<SeriesItemState>();
			var recycler = new Recycler<FrameworkElement, ISeriesItemValueDouble>(CreateElement);
			var ipsiv = Source as IProvideSeriesItemValues;
			foreach (var item in items) {
				var target = ElementPipeline(ipsiv, item, recycler);
				if (target != null) {
					reproc.Add(target);
				}
			}
			// adjust indices based on removed items
			foreach (var itx in ItemState.Where(ix => ix.Index >= startAt)) {
				itx.UpdatePlacement(PlacementOffset, CategoryAxisOffset);
			}
			// re-evaluate everything except the new ones
			foreach(var itx in ItemState) {
				UpdateElement(ipsiv, itx);
			}
			if (reproc.Count > 0) {
				// add collected items and re-sort by index
				ItemState.AddRange(reproc);
				if (ItemState.Count > 1) {
					ItemState.Sort((i1, i2) => i1.Index.CompareTo(i2.Index));
				}
			}
			Layer.Add(recycler.Created);
			// everything else will re-calc in Transforms()
		}
		/// <summary>
		/// Handle bookkeeping for incremental remove.
		/// </summary>
		/// <param name="startAt">Starting index.</param>
		/// <param name="items">Items affected.</param>
		void IncrementalRemove(int startAt, IList<ISeriesItem> items) {
			var reproc = new List<SeriesItemState>();
			foreach (var item in items) {
				var source = item is IProvideOriginalState ipws ? ipws.Original : item;
				var target = ItemState.SingleOrDefault(xx => xx.Source == source && xx.Channel == ValueFor(item)?.Channel);
				if (target != null) {
					// found one remove it
					ItemState.Remove(target);
					reproc.Add(target);
				}
			}
			// adjust indices based on removed items
			foreach (var itx in ItemState.Where(ix => ix.Index >= startAt)) {
				itx.UpdatePlacement(PlacementOffset, CategoryAxisOffset);
			}
			// re-evaluate everything
			foreach (var itx in ItemState) {
				UpdateElement(Source as IProvideSeriesItemValues, itx);
			}
			var els = reproc.Select(xx => xx.Element).Where(el => el != null);
			TeardownElements(els);
			Layer.Remove(els);
			// everything else will re-calc in Transforms()
		}
		#endregion
		#region evhs
		/// <summary>
		/// Follow-up handler to re-position the label element at exactly the right spot after it's done with (asynchronous) measure/arrange.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Element_SizeChanged(object sender, SizeChangedEventArgs e) {
#if false
			var vm = fe.DataContext as TextShim;
			_trace.Verbose($"{Name} sizeChanged ps:{e.PreviousSize} ns:{e.NewSize} text:{vm?.Text}");
#endif
			var fe = sender as FrameworkElement;
			var state = ItemState.SingleOrDefault((sis) => sis.Element == fe);
			if (state != null) {
				_trace.Verbose($"{Name} sizeChanged loc:{state.CanvasLocation} yv:{state.Value} ns:{e.NewSize}");
				state.Locate(LabelOffset);
			}
		}
		/// <summary>
		/// Forward incremental updates.
		/// </summary>
		/// <param name="sender">The Source.</param>
		/// <param name="e">Incremental update info.</param>
		private void Ipsiu_ItemUpdates(object sender, SeriesItemUpdateEventArgs e) {
			_trace.Verbose($"itemUpdates '{Name}' {e.Action} @{e.StartAt}+{e.Items.Count}");
			switch(e.Action) {
			case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
				IncrementalAdd(e.StartAt, e.Items);
				break;
			case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
				IncrementalRemove(e.StartAt, e.Items);
				break;
			}
		}
		/// <summary>
		/// Cascade visibility changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="dp"></param>
		private void PropertyChanged_Visibility(DependencyObject sender, DependencyProperty dp) {
			_trace.Verbose($"inst.vizChanged {Name} {Visibility}");
			if (dp == VisibilityProperty) {
				UpdateVisibility(Visibility);
			}
		}
		#endregion
		#region IRequireEnterLeave
		long token;
		void IRequireEnterLeave.Enter(IChartEnterLeaveContext icelc) {
			EnsureComponents(icelc as IChartComponentContext);
			Layer = icelc.CreateLayer();
			if (Layer is IChartLayerAnimation icla) {
				// pass through storyboards
				icla.Enter = EnterStoryboard;
				icla.Leave = LeaveStoryboard;
			}
			AssignFromRef(icelc as IChartErrorInfo, NameOrType(), nameof(LabelStyle), nameof(Theme.LabelAxisTop),
				LabelStyle == null, Theme != null, Theme.LabelAxisTop != null,
				() => LabelStyle = Theme.LabelAxisTop
			);
			if(Source is IProvideSeriesItemUpdates ipsiu) {
				ipsiu.ItemUpdates += Ipsiu_ItemUpdates;
			}
			_trace.Verbose($"{Name} enter s:{SourceName} {Source} v:{ValueAxis} c:{CategoryAxis}");
			token = RegisterPropertyChangedCallback(UIElement.VisibilityProperty, PropertyChanged_Visibility);
		}
		void IRequireEnterLeave.Leave(IChartEnterLeaveContext icelc) {
			_trace.Verbose($"{Name} leave");
			UnregisterPropertyChangedCallback(VisibilityProperty, token);
			if (Source is IProvideSeriesItemUpdates ipsiu) {
				ipsiu.ItemUpdates -= Ipsiu_ItemUpdates;
			}
			ValueAxis = null;
			CategoryAxis = null;
			Source = null;
			icelc.DeleteLayer(Layer);
			Layer = null;
		}
		#endregion
		#region IRequireRender
		void IRequireRender.Render(IChartRenderContext icrc) {
			if (LabelTemplate == null && Theme?.TextBlockTemplate == null) {
			// already reported an error so this should be no surprise
				return;
			}
			#if true
			if(Source is IProvideSeriesItemUpdates && icrc.Type == RenderType.Incremental) {
				// already handled it
				return;
			}
			#endif
			if(Source is IProvideSeriesItemValues ipsiv) {
				// preamble
				var elements = ItemState.Select(ms => ms.Element).Where(el => el != null);
				var recycler = new Recycler<FrameworkElement, ISeriesItemValueDouble>(elements, CreateElement);
				var itemstate = new List<SeriesItemState>();
				// render
				foreach (var siv in ipsiv.SeriesItemValues) {
					var istate = CreateState(siv);
					if(istate != null) {
						itemstate.Add(istate);
						if(ApplySelector(ipsiv, istate.ValueSource)) {
							var el = recycler.Next(istate.ValueSource);
							if (!el.Item1 && el.Item2.DataContext is TextShim shim) {
								RecycleElement(istate.ValueSource, el.Item2, shim);
							}
							istate.Element = el.Item2;
							ApplyFormatter(ipsiv, istate);
						}
					}
				}
				// postamble
				ItemState = itemstate;
				TeardownElements(recycler.Unused);
				Layer.Remove(recycler.Unused);
				Layer.Add(recycler.Created);
				Dirty = false;
			}
		}
		#endregion
		#region IRequireTransforms
		/// <summary>
		/// Adjust transforms for the current element state.
		/// </summary>
		/// <param name="icrc"></param>
		void IRequireTransforms.Transforms(IChartRenderContext icrc) {
			if (ItemState.Count == 0) return;
			var matx = ObtainMatrix(icrc);
			_trace.Verbose($"{Name} transforms a:{icrc.Area} rx:{CategoryAxis?.Range} ry:{ValueAxis?.Range} matx:{matx}  type:{icrc.Type}");
			if (matx == default(Matrix)) return;
			foreach (var state in ItemState) {
				if (state.Element == null) continue;
				_trace.Verbose($"{Name} el:{state.Element} ds:{state.Element.DesiredSize} as:{state.Element.ActualWidth},{state.Element.ActualHeight}");
				// Recalc and Position element now because it WILL NOT invoke EVH if size didn't actually change
				state.Locate(matx, LabelOffset, icrc.Type);
#if false
				if (ClipToDataRegion) {
					// TODO this does not work "correctly" the TB gets clipped no matter what
					// this is because the clip coordinate system is for "inside" the text block (gotta verify this)
					// must find intersection of the TB bounds and the icrc.SeriesArea, and make that the clip.
					//state.Element.Clip = new RectangleGeometry() { Rect = icrc.SeriesArea };
				}
#endif
				_trace.Verbose($"{Name} matx:{matx} pt:({state.XValue},{state.Value}) canvas:{state.CanvasLocation}");
			}
		}
		#endregion
	}
	#endregion
}
