using System;
using Windows.Foundation;

namespace eScapeLLC.UWP.Charts {
	#region WorldRect
#pragma warning disable CS0419 // Ambiguous reference in cref attribute
	/// <summary>
	/// World Coordinate "cartesian" version of <see cref="Rect"/>.
	/// Y-axis is OPPOSITE of DC!
	/// </summary>
#pragma warning restore CS0419 // Ambiguous reference in cref attribute
	public struct WorldRect : IFormattable {
		/// <summary>
		/// Minimum of x-components.
		/// </summary>
		public double Left { get; private set; }
		/// <summary>
		/// Maximum of x-components.
		/// </summary>
		public double Right { get; private set; }
		/// <summary>
		/// Maximum of y-components.
		/// </summary>
		public double Top { get; private set; }
		/// <summary>
		/// Minimum of y-components.
		/// </summary>
		public double Bottom { get; private set; }
		/// <summary>
		/// Width of rectangle.
		/// </summary>
		public double Width => Math.Abs(Right - Left);
		/// <summary>
		/// Height of rectangle.
		/// </summary>
		public double Height => Top - Bottom;
		/// <summary>
		/// True if rectangle is "negative" (below zero).
		/// </summary>
		public bool IsInverted => Bottom < 0;
		/// <summary>
		/// Return the half-dimension (for placement).
		/// </summary>
		public Size HalfDimension => new Size(Width / 2, Height / 2);
		/// <summary>
		/// Return the center point (for placement).
		/// </summary>
		public Point Center => new Point(Left + Width / 2, Top - Height / 2);
		/// <summary>
		/// Ctor.
		/// Accepts any two "diagonally-opposing" corners.
		/// </summary>
		/// <param name="p1">One corner.</param>
		/// <param name="p2">Opposite corner.</param>
		public WorldRect(Point p1, Point p2) {
			Left = Math.Min(p1.X, p2.X);
			Right = Math.Max(p1.X, p2.X);
			Top = Math.Max(p1.Y, p2.Y);
			Bottom = Math.Min(p1.Y, p2.Y);
		}
		/// <summary>
		/// "Flip" the rect by exchanging the X and Y components.
		/// </summary>
		/// <returns></returns>
		public WorldRect Flip() {
			return new WorldRect(new Point(Top, Left), new Point(Bottom, Right));
		}
		/// <summary>
		/// Override.  Uses equality operator.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) {
			if (!(obj is WorldRect wr)) return false;
			return this == wr;
		}
		/// <summary>
		/// Calculate a hash code.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() {
			unchecked {
				// Overflow is fine, just wrap
				int hash = 17;
				hash = (hash * 23) + Left.GetHashCode();
				hash = (hash * 23) + Top.GetHashCode();
				hash = (hash * 23) + Right.GetHashCode();
				hash = (hash * 23) + Bottom.GetHashCode();
				return hash;
			}
		}
		/// <summary>
		/// Equality operator.
		/// Performs exact comparison on numerics.
		/// </summary>
		/// <param name="r1"></param>
		/// <param name="r2"></param>
		/// <returns></returns>
		public static bool operator ==(WorldRect r1, WorldRect r2) {
			return r1.Left == r2.Left && r1.Right == r2.Right && r1.Top == r2.Top && r1.Bottom == r2.Bottom;
		}
		/// <summary>
		/// Inequality operator.
		/// Performs exact comparison on numerics.
		/// </summary>
		/// <param name="r1"></param>
		/// <param name="r2"></param>
		/// <returns></returns>
		public static bool operator !=(WorldRect r1, WorldRect r2) {
			return r1.Left != r2.Left || r1.Right != r2.Right || r1.Top != r2.Top || r1.Bottom != r2.Bottom;
		}
		/// <summary>
		/// Format the value.
		/// </summary>
		/// <param name="format"></param>
		/// <param name="formatProvider"></param>
		/// <returns></returns>
		public string ToString(string format, IFormatProvider formatProvider) {
			return IsInverted ? $"!({Left},{Top})({Right},{Bottom}) ({Width} x {Height})" : $"({Left},{Top})({Right},{Bottom}) ({Width} x {Height})";
		}
	}
	#endregion
}
