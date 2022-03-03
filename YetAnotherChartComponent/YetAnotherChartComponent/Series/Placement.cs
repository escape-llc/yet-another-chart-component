using System;
using Windows.Foundation;

namespace eScapeLLC.UWP.Charts {
	#region Placement
	/// <summary>
	/// Abstract base for placement data.
	/// </summary>
	public abstract class Placement {
		#region WC direction vectors
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
		/// <param name="poffset">Placement offset.</param>
		/// <returns>Transformed point.</returns>
		public abstract Point Transform(Point poffset);
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
		public RectanglePlacement(Point direction, WorldRect rc) : this(direction, rc.Center, rc.HalfDimension) { }
		/// <summary>
		/// For compatibility.
		/// </summary>
		/// <param name="direction"></param>
		/// <param name="rc"></param>
		public RectanglePlacement(Point direction, Rect rc) : this(direction, new Point(rc.Left + rc.Width/2, rc.Top + rc.Height/2), new Size(rc.Width/2, rc.Height/2)) { }
		/// <summary>
		/// Ctor.
		/// Infers direction from the coordinates of the rectangle.
		/// IST: the Rect is always DC so its direction vector is (-,1) because the most-negative y-coordinate becomes the TOP.
		/// </summary>
		/// <param name="rc">A rectangle.</param>
		public RectanglePlacement(WorldRect rc) : this(new Point(Math.Sign(rc.Right - rc.Left), Math.Sign(rc.Bottom - rc.Top)), rc) { }
		#endregion
		#region extensions
		/// <summary>
		/// Take the placement coordinates and transform them.
		/// (0,0) is the "center".
		/// (1,0) is the "end".
		/// (-1,0) is the "start".
		/// The directions are relative to the <see cref="Direction"/> vector.
		/// </summary>
		/// <param name="poffset">Placement offset.</param>
		/// <returns></returns>
		public override Point Transform(Point poffset) { return new Point(Center.X + poffset.X * HalfDimensions.Width * Direction.X, Center.Y + poffset.Y * HalfDimensions.Height * Direction.Y); }
		#endregion
	}
	#endregion
	#region MidpointPlacement
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
		/// <param name="poffset">Position offset.</param>
		/// <returns>Transformed point.</returns>
		public override Point Transform(Point poffset) {
			return new Point(Midpoint.X + poffset.X * HalfDimension * Direction.X, Midpoint.Y + poffset.Y * HalfDimension * Direction.Y);
		}
		#endregion
	}
	#endregion
	#region MarkerPlacement
	/// <summary>
	/// Placement at specific World Coordinate with no Label Placement scale, i.e. PX.
	/// </summary>
	public class MarkerPlacement : Placement {
		#region properties
		/// <summary>
		/// Location of the point.
		/// </summary>
		public Point Location { get; private set; }
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
		/// <param name="loc"></param>
		/// <param name="dir"></param>
		public MarkerPlacement(Point loc, Point dir) { Location = loc; Direction = dir; }
		#endregion
		/// <summary>
		/// <inheritdoc/>
		/// Scale is DC in both directions.
		/// </summary>
		/// <param name="poffset">Position offset.</param>
		/// <returns></returns>
		public override Point Transform(Point poffset) {
			return new Point(Location.X + poffset.X * Direction.X, Location.Y + poffset.Y * Direction.Y);
		}
	}
	/// <summary>
	/// Ability to provide placement data for a channel.
	/// </summary>
	public interface IProvidePlacement {
		/// <summary>
		/// Provide the placement information.
		/// DO NOT return NULL; instead DO NOT implement this interface!
		/// </summary>
		Placement Placement { get; }
		/// <summary>
		/// Clear any cached placement.
		/// This is required during incremental updates.
		/// </summary>
		void ClearCache();
	}
	#endregion
}
