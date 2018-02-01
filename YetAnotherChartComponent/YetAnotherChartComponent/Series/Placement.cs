using System;
using Windows.Foundation;

namespace eScapeLLC.UWP.Charts {
	#region Placement
	/// <summary>
	/// Abstract base for placement data.
	/// </summary>
	public abstract class Placement {
		#region direction vectors
		/// <summary>
		/// Direction vector: up.
		/// </summary>
		public static readonly Point UP_ONLY = new Point(0, 1);
		/// <summary>
		/// Direction vector: up and right.
		/// </summary>
		public static readonly Point UP_RIGHT = new Point(1, 1);
		/// <summary>
		/// Direction vector: up and left.
		/// </summary>
		public static readonly Point UP_LEFT = new Point(-1, 1);
		/// <summary>
		/// Direction vector: right.
		/// </summary>
		public static readonly Point RIGHT_ONLY = new Point(1, 0);
		/// <summary>
		/// Direction vector: down.
		/// </summary>
		public static readonly Point DOWN_ONLY = new Point(0, -1);
		/// <summary>
		/// Direction vector: down and right.
		/// </summary>
		public static readonly Point DOWN_RIGHT = new Point(1, -1);
		/// <summary>
		/// Direction vector: down and left.
		/// </summary>
		public static readonly Point DOWN_LEFT = new Point(-1, -1);
		/// <summary>
		/// Direction vector: left.
		/// </summary>
		public static readonly Point LEFT_ONLY = new Point(-1, 0);
		#endregion
		/// <summary>
		/// Take the placement coordinates and transform them.
		/// (0,0) is the "center".
		/// (1,0) is the "end".
		/// (-1,0) is the "start".
		/// The directions are "relative" as defined by subclasses.
		/// </summary>
		/// <param name="pt">Input point.</param>
		/// <returns>Transformed point.</returns>
		public abstract Point Transform(Point pt);
	}
	#endregion
	#region RectanglePlacement
	/// <summary>
	/// Rectangle placement uses a center point of (0,0) at the center of the rectangle.
	/// Coordinates extend in one unit along each axis, relative to a "direction" vector
	/// that can be used to "flip" the coordinate system for mirroring purposes.
	/// </summary>
	public class RectanglePlacement : Placement {
		#region properties
		/// <summary>
		/// Which way figure is "pointing".
		/// For a rectangle SHOULD be axis-aligned.
		/// MUST be one of (-1, 0, 1) in each dimension!  MAY use Zero to "lock" an axis, or negative one to "flip" an axis.
		/// The X,Y are multiplied with the incoming values in <see cref="Transform"/>.
		/// </summary>
		public Point Direction { get; private set; }
		/// <summary>
		/// Center point of the rectangle.
		/// </summary>
		public Point Center { get; private set; }
		/// <summary>
		/// Half-dimensions of the rectangle.
		/// </summary>
		public Size HalfDimensions { get; private set; }
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="direction">Direction vector.</param>
		/// <param name="center">Center point.</param>
		/// <param name="hd">Half dimensions.</param>
		public RectanglePlacement(Point direction, Point center, Size hd) {
			Direction = direction;
			Center = center;
			HalfDimensions = hd;
		}
		/// <summary>
		/// Ctor.
		/// Explicit direction vector.
		/// </summary>
		/// <param name="direction"></param>
		/// <param name="rc"></param>
		public RectanglePlacement(Point direction, Rect rc) : this(direction, new Point(rc.Left + rc.Width / 2, rc.Top + rc.Height / 2), new Size(rc.Width / 2, rc.Height / 2)) { }
		/// <summary>
		/// Ctor.
		/// Infers direction from the coordinates of the rectangle.
		/// IST: the Rect is always DC so its direction vector is (-,1) because the most-negative y-coordinate becomes the TOP.
		/// </summary>
		/// <param name="rc">A rectangle.</param>
		public RectanglePlacement(Rect rc) : this(new Point(Math.Sign(rc.Right - rc.Left), Math.Sign(rc.Bottom - rc.Top)), rc) { }
		#endregion
		#region extensions
		/// <summary>
		/// Take the placement coordinates and transform them.
		/// (0,0) is the "center".
		/// (1,0) is the "end".
		/// (-1,0) is the "start".
		/// The directions are relative to the <see cref="Direction"/> vector.
		/// </summary>
		/// <param name="pt"></param>
		/// <returns></returns>
		public override Point Transform(Point pt) { return new Point(Center.X + pt.X * HalfDimensions.Width * Direction.X, Center.Y + pt.Y * HalfDimensions.Height * Direction.Y); }
		#endregion
	}
	/// <summary>
	/// Placement for a midpoint segment.
	/// </summary>
	public class MidpointPlacement : Placement {
		#region properties
		/// <summary>
		/// Midpoint of the segment.
		/// </summary>
		public Point Midpoint { get; private set; }
		/// <summary>
		/// Half-unit dimension.  This gets to either "end" of the segment from midpoint.
		/// </summary>
		public double HalfDimension { get; private set; }
		/// <summary>
		/// Which way segment is "pointing".
		/// For a wedge SHOULD be actual angle (cos/sin) from center point toward circumference.
		/// The X,Y are multiplied with the incoming values in <see cref="Transform"/>.
		/// </summary>
		public Point Direction { get; private set; }
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="mp"></param>
		/// <param name="dir"></param>
		/// <param name="hd"></param>
		public MidpointPlacement(Point mp, Point dir, double hd) { Midpoint = mp; Direction = dir; HalfDimension = hd; }
		#endregion
		#region extensions
		/// <summary>
		/// Project the point along the line segment.
		/// </summary>
		/// <param name="pt">Position offset.</param>
		/// <returns>Transformed point.</returns>
		public override Point Transform(Point pt) {
			return new Point(Midpoint.X + pt.X * HalfDimension * Direction.X, Midpoint.Y + pt.Y * HalfDimension * Direction.Y);
		}
		#endregion
	}
	/// <summary>
	/// Ability to provide placement data for a channel.
	/// </summary>
	public interface IProvidePlacement {
		/// <summary>
		/// Provide the placement information.
		/// </summary>
		Placement Placement { get; }
	}
	#endregion
}
