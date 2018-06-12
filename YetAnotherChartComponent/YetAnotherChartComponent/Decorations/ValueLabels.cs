#undef COMPOSITION_ENABLED
using eScape.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

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
		protected class SeriesItemState : ItemState<FrameworkElement> {
			internal Point Direction { get; set; }
			internal Point CanvasLocation { get; set; }
			internal object CustomValue { get; set; }
			internal SeriesItemState(int idx, double xv, double xvo, double yv, FrameworkElement ele, int ch) : base(idx, xv, xvo, yv, ele, ch) { }
			internal void Locate(FrameworkElement fe, Point offs) {
				var hw = fe.ActualWidth / 2;
				var hh = fe.ActualHeight / 2;
				fe.SetValue(Canvas.LeftProperty, CanvasLocation.X - hw + hw * offs.X * Direction.X);
				fe.SetValue(Canvas.TopProperty, CanvasLocation.Y - hh + hh * offs.Y * Direction.Y);
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
		/// Uses <see cref="String"/> for label override.  Return a new label or NULL to opt out.
		/// Uses <see cref="Style"/> for style override.  Return a style or NULL to opt out.
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
		/// Get the transform from the source, or based on axes, whichever hits first.
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
				fe = Theme.TextBlockTemplate.LoadContent() as TextBlock;
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
#if COMPOSITION_ENABLED
				UniversalApiContract.v3.CompositionSupport.AttachAnimations(fe);
#endif
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
				state.Locate(fe, LabelOffset);
			}
		}
		#endregion
		#region evhs
		/// <summary>
		/// Forward incremental updates.
		/// </summary>
		/// <param name="sender">The Source.</param>
		/// <param name="e">Incremental update info.</param>
		private void Ipsiu_ItemUpdates(object sender, SeriesItemUpdateEventArgs e) {
			_trace.Verbose($"itemUpdates {Name} {e.Action} @{e.StartAt}+{e.Items.Count}");
			switch(e.Action) {
			case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
				break;
			case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
				var reproc = IncrementalRemove<SeriesItemState>(e.StartAt, e.Items as IList, ItemState, null, (rpc, istate) => {
					// TODO must track the originating series item (to capture updated values after incr-update)
					// TODO position depends upon placement data
					// var index = istate.Index - rpc;
					//istate.Move(index, leftx, offsetx);
					// update geometry
					var rg = istate.Element as TextBlock;
				});
				Layer.Remove(reproc.Select(xx => xx.Element));
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
			if(Source is IProvideSeriesItemUpdates && icrc.Type == RenderType.Incremental) {
				// already handled it
				return;
			}
			if(Source is IProvideSeriesItemValues ipsiv) {
				// preamble
				var elements = ItemState.Select(ms => ms.Element).Where(el => el != null);
				var recycler = new Recycler<FrameworkElement, ISeriesItemValueDouble>(elements, CreateElement);
				var itemstate = new List<SeriesItemState>();
				// render
				foreach (var siv in ipsiv.SeriesItemValues) {
					ISeriesItemValue target = null;
					if (siv is ISeriesItemValue isiv) {
						if (isiv.Channel == ValueChannel) {
							target = isiv;
						}
					}
					else if(siv is ISeriesItemValues isivs) {
						target = isivs.YValues.SingleOrDefault(yv => yv.Channel == ValueChannel);
					}
					if(target is ISeriesItemValueDouble isivd && !double.IsNaN(isivd.Value)) {
						var createit = true;
						if(LabelSelector != null) {
							// apply LabelSelector
							var ctx = new SelectorContext(ipsiv, target);
							var ox = LabelSelector.Convert(ctx, typeof(bool), null, System.Globalization.CultureInfo.CurrentUICulture.Name);
							if(ox is bool bx) {
								createit = bx;
							}
							else {
								createit = ox != null;
							}
						}
						if (!createit) continue;
						var el = recycler.Next(isivd);
						if (!el.Item1 && el.Item2.DataContext is TextShim shim) {
							// recycling; update values
							var txt = isivd.Value.ToString(String.IsNullOrEmpty(LabelFormatString) ? "G" : LabelFormatString);
							shim.Visibility = Visibility;
							shim.Text = txt;
							if(shim is ObjectShim oshim && isivd is ISeriesItemValueCustom isivc) {
								oshim.CustomValue = isivc.CustomValue;
							}
							// restore binding if we are using a LabelFormatter
							if (LabelFormatter != null && LabelStyle != null) {
								BindTo(this, nameof(LabelStyle), el.Item2, FrameworkElement.StyleProperty);
							}
						}
						if (LabelFormatter != null) {
							var ctx = new SelectorContext(ipsiv, target);
							// TODO could call for typeof(object) and replace CustomValue
							var format = LabelFormatter.Convert(ctx, typeof(Tuple<Style, String>), null, System.Globalization.CultureInfo.CurrentUICulture.Name);
							if (format is Tuple<Style, String> ovx) {
								if (ovx.Item1 != null) {
									el.Item2.Style = ovx.Item1;
								}
								if (ovx.Item2 != null) {
									if (el.Item2.DataContext is TextShim ts) {
										ts.Text = ovx.Item2;
									}
								}
							}
						}
						var pmt = (target as IProvidePlacement)?.Placement;
						switch(pmt) {
						case RectanglePlacement rp:
							var pt = rp.Transform(PlacementOffset);
							_trace.Verbose($"rp c:{rp.Center} d:{rp.Direction} hd:{rp.HalfDimensions} pt:{pt}");
							var sis = new SeriesItemState(siv.Index, siv.XValue, siv.XValue + CategoryAxisOffset, pt.Y, el.Item2, target.Channel) {
								Direction = rp.Direction,
								CustomValue = target is ISeriesItemValueCustom isivc ? isivc.CustomValue : null
								};
							itemstate.Add(sis);
							break;
						case MidpointPlacement mp:
							var pt2 = mp.Transform(PlacementOffset);
							_trace.Verbose($"mp {mp.Midpoint} d:{mp.Direction} hd:{mp.HalfDimension} pt:{pt2}");
							var sis2 = new SeriesItemState(siv.Index, siv.XValue, pt2.X, pt2.Y, el.Item2, target.Channel) {
								Direction = mp.Direction,
								CustomValue = target is ISeriesItemValueCustom isivc2 ? isivc2.CustomValue : null
							};
							itemstate.Add(sis2);
							break;
						default:
							var sis3 = new SeriesItemState(siv.Index, siv.XValue, siv.XValue + CategoryAxisOffset, isivd.Value, el.Item2, target.Channel) {
								Direction = Placement.UP_RIGHT,
								CustomValue = target is ISeriesItemValueCustom isivc3 ? isivc3.CustomValue : null
							};
							itemstate.Add(sis3);
							break;
						}
					}
				}
				// postamble
				ItemState = itemstate;
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
				state.CanvasLocation = matx.Transform(new Point(state.XValueAfterOffset, state.Value));
				_trace.Verbose($"{Name} el:{state.Element} ds:{state.Element.DesiredSize} as:{state.Element.ActualWidth},{state.Element.ActualHeight}");
				// Position element now because it WILL NOT invoke EVH if size didn't actually change
				state.Locate(state.Element, LabelOffset);
				if (icrc.Type != RenderType.TransformsOnly) {
					// doing render so (try to) trigger the SizeChanged handler
					state.Element.InvalidateMeasure();
					state.Element.InvalidateArrange();
				}
#if false
				if (ClipToDataRegion) {
					// TODO this does not work "correctly" the TB gets clipped no matter what
					// this is because the clip coordinate system is for "inside" the text block (gotta verify this)
					// must find intersection of the TB bounds and the icrc.SeriesArea, and make that the clip.
					//state.Element.Clip = new RectangleGeometry() { Rect = icrc.SeriesArea };
				}
#endif
				_trace.Verbose($"{Name} matx:{matx} pt:({state.XValue},{state.Value}) dcc:{state.CanvasLocation}");
			}
		}
		#endregion
	}
	#endregion
}
