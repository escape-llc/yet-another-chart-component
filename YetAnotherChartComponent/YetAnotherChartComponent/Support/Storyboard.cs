using System;
using System.Reflection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

namespace eScapeLLC.UWP.Charts {
	/// <summary>
	/// Extension methods for <see cref="Storyboard"/> and friends.
	/// </summary>
	public static class StoryboardExtensions {
		/// <summary>
		/// Clone the <see cref="Timeline"/> via reflection on writable public instance properties.
		/// </summary>
		/// <param name="tl">Source.</param>
		/// <returns>New instance.</returns>
		public static Timeline Clone(this Timeline tl) {
			if (tl == null) return null;
			var clone = Activator.CreateInstance(tl.GetType());
			// NOTE any collections within TL just get shallow-copied, e.g. KeyFrameCollection
			foreach (var pi in tl.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
				if (pi.CanWrite) {
					pi.SetValue(clone, pi.GetValue(tl));
				}
			}
			return clone as Timeline;
		}
		/// <summary>
		/// Clone the given <see cref="Storyboard"/> and its children.
		/// </summary>
		/// <param name="sb">Source.</param>
		/// <param name="fe">Target.</param>
		/// <returns></returns>
		public static Storyboard Clone(this Storyboard sb, FrameworkElement fe) {
			if (sb == null) return null;
			var clone = new Storyboard() {
				AutoReverse = sb.AutoReverse,
				BeginTime = sb.BeginTime,
				Duration = sb.Duration,
				FillBehavior = sb.FillBehavior,
				RepeatBehavior = sb.RepeatBehavior,
				SpeedRatio = sb.SpeedRatio
			};
			foreach (var tl in sb.Children) {
				var tclone = tl.Clone();
				if (tclone == null) continue;
				// cloned it; do attached properties
				var tname = Storyboard.GetTargetName(tl);
				if(!String.IsNullOrEmpty(tname)) {
					Storyboard.SetTargetName(tclone, tname);
				}
				Storyboard.SetTargetProperty(tclone, Storyboard.GetTargetProperty(tl));
				clone.Children.Add(tclone);
			}
			Storyboard.SetTarget(clone, fe);
			return clone;
		}
	}
}
