using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region StyleExtensions
	/// <summary>
	/// Extension methods for <see cref="Style"/>.
	/// </summary>
	public static class StyleExtensions {
		/// <summary>
		/// Find the <see cref="Setter"/> and get its value, ELSE return a default value.
		/// </summary>
		/// <typeparam name="T">Return type.</typeparam>
		/// <param name="style">Style to search.</param>
		/// <param name="property">DP to locate.</param>
		/// <param name="defv">Default value to return if nothing found OR the style is NULL.</param>
		/// <returns>The value or DEFV.</returns>
		public static T Find<T>(this Style style, DependencyProperty property, T defv = default(T)) {
			if (style == null) return defv;
			var xx = style.Find(property);
			return xx == null ? defv : (T)xx.Value;
		}
		/// <summary>
		/// Search style and all sub-styles for the given DP, ELSE return a default value.
		/// Searches depth-first.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="style"></param>
		/// <param name="property"></param>
		/// <param name="defv"></param>
		/// <returns></returns>
		public static T FindRecursive<T>(this Style style, DependencyProperty property, T defv = default(T)) {
			if (style == null) return defv;
			if (style.BasedOn != null) {
				var sx = style.BasedOn.Find(property);
				if (sx != null) {
					return (T)sx.Value;
				}
			}
			var xx = style.Find(property);
			return xx == null ? defv : (T)xx.Value;
		}
		/// <summary>
		/// Return the <see cref="Setter"/> for the given DP on the immediate style.
		/// </summary>
		/// <param name="style"></param>
		/// <param name="property"></param>
		/// <returns>The <see cref="Setter"/> or NULL.</returns>
		public static Setter Find(this Style style, DependencyProperty property) {
			if (style == null) return null;
			foreach (var xx in style.Setters) {
				if (xx is Setter sx) {
					if (sx.Property == property) {
						return sx;
					}
				}
			}
			return null;
		}
	}
	#endregion
	#region StyleGenerator
	/// <summary>
	/// Abstract base for <see cref="Style"/> generator.
	/// </summary>
	public abstract class StyleGenerator : DependencyObject {
		#region properties
		/// <summary>
		/// The underlying style to use when creating new styles.
		/// </summary>
		public Style BaseStyle { get { return (Style)GetValue(BaseStyleProperty); } set { SetValue(BaseStyleProperty, value); } }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies the <see cref="BaseStyle"/> DP.
		/// </summary>
		public static readonly DependencyProperty BaseStyleProperty = DependencyProperty.Register(
			nameof(BaseStyle), typeof(string), typeof(StyleGenerator), new PropertyMetadata(null)
		);
		#endregion
		#region extension points
		/// <summary>
		/// Return the "next" style.
		/// </summary>
		/// <returns></returns>
		public abstract Style NextStyle();
		/// <summary>
		/// Reset the style sequence, if applicable.
		/// </summary>
		public abstract void Reset();
		#endregion
		#region helpers
		/// <summary>
		/// Convenience method to copy a <see cref="Style"/> and replace given <see cref="DependencyProperty"/>with given value.
		/// </summary>
		/// <param name="source">Source style.</param>
		/// <param name="tp">Target DP.</param>
		/// <param name="pvalue">New value.</param>
		/// <returns>New instance.</returns>
		/// <typeparam name="PT">Property value type.</typeparam>
		protected static Style Override<PT>(Style source, DependencyProperty tp, PT pvalue) {
			var style = new Style(source.TargetType) {
				BasedOn = source.BasedOn
			};
			var did = false;
			foreach (var setter in source.Setters) {
				if (setter is Setter sx && sx.Property == tp) {
					style.Setters.Add(new Setter(tp, pvalue));
					did = true;
				} else {
					style.Setters.Add(setter);
				}
			}
			if (!did) {
				style.Setters.Add(new Setter(tp, pvalue));
			}
			return style;
		}
		#endregion
	}
	/// <summary>
	/// Identity.  Returns the BaseStyle over-and-over.
	/// </summary>
	public sealed class IdentityStyleGenerator : StyleGenerator {
		/// <summary>
		/// Return the base style always.
		/// </summary>
		/// <returns></returns>
		public override Style NextStyle() { return BaseStyle; }
		/// <summary>
		/// No Action.
		/// </summary>
		public override void Reset() { }
	}
	/// <summary>
	/// Rotates through a hard-coded set of pre-defined colors.
	/// </summary>
	public sealed class DefaultColorsGenerator : StyleGenerator {
		#region data
		int current;
		Dictionary<Brush, Style> stylemap = new Dictionary<Brush, Style>();
		static Brush[] _presetBrushes = new Brush[] {
			new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x66, 0x00)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0xFC, 0xD2, 0x02)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0xB0, 0xDE, 0x09)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0x0D, 0x8E, 0xCF)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0x2A, 0x0C, 0xD0)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0xCD, 0x0D, 0x74)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0xCC, 0x00, 0x00)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0xCC, 0x00)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x00, 0xCC)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0xDD, 0xDD, 0xDD)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0x99, 0x99, 0x99)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0x33, 0x33, 0x33)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0x99, 0x00, 0x00))
		};
		#endregion
		#region extensions
		/// <summary>
		/// Reset the counter but don't clear the style cache.
		/// </summary>
		public override void Reset() {
			current = 0;
		}
		/// <summary>
		/// Make a new style for each color, caching them as they are created.
		/// </summary>
		/// <returns></returns>
		public override Style NextStyle() {
			var cbrush = _presetBrushes[current];
			if (stylemap.ContainsKey(cbrush)) return stylemap[cbrush];
			var style = Override(BaseStyle, Path.FillProperty, cbrush);
			// advance
			stylemap.Add(cbrush, style);
			current = (current + 1) % _presetBrushes.Length;
			return style;
		}
		#endregion
	}
	#endregion
}
