using System;

namespace eScapeLLC.UWP.Charts {
	/// <summary>
	/// Helpers for color conversion.
	/// </summary>
	public static class ColorSupport {
		#region public static
		/// <summary>
		/// HSV to RGB conversion.
		/// </summary>
		/// <param name="hue">HSV hue.</param>
		/// <param name="sat">HSV saturation.</param>
		/// <param name="value">HSV value.</param>
		/// <param name="rr">Output Red value.</param>
		/// <param name="gg">Output Green value.</param>
		/// <param name="bb">Output Blue value.</param>
		public static void HsvToRgb(double hue, double sat, double value, out int rr, out int gg, out int bb) {
			double huex = hue;
			while (huex < 0) { huex += 360; };
			while (huex >= 360) { huex -= 360; };
			double red, green, blue;
			if (value <= 0) { red = green = blue = 0; }
			else if (sat <= 0) {
				red = green = blue = value;
			}
			else {
				double hf = huex / 60.0;
				int ix = (int)Math.Floor(hf);
				double fx = hf - ix;
				double pv = value * (1 - sat);
				double qv = value * (1 - sat * fx);
				double tv = value * (1 - sat * (1 - fx));
				switch (ix) {
					// Red is the dominant color
					case 0:
						red = value;
						green = tv;
						blue = pv;
						break;
					// Green is the dominant color
					case 1:
						red = qv;
						green = value;
						blue = pv;
						break;
					case 2:
						red = pv;
						green = value;
						blue = tv;
						break;
					// Blue is the dominant color
					case 3:
						red = pv;
						green = qv;
						blue = value;
						break;
					case 4:
						red = tv;
						green = pv;
						blue = value;
						break;
					// Red is the dominant color
					case 5:
						red = value;
						green = pv;
						blue = qv;
						break;
					// Just in case we overshoot on our math by a little, we put these here.
					case 6:
						red = value;
						green = tv;
						blue = pv;
						break;
					case -1:
						red = value;
						green = pv;
						blue = qv;
						break;
					// The color is not defined
					default:
						red = green = blue = value; // Just pretend its black/white
						break;
				}
			}
			rr = Clamp((int)(red * 255.0));
			gg = Clamp((int)(green * 255.0));
			bb = Clamp((int)(blue * 255.0));
		}
		/// <summary>
		/// Clamp a value to 0-255
		/// </summary>
		/// <param name="i">Source value.</param>
		public static int Clamp(int i) {
			if (i < 0) return 0;
			if (i > 255) return 255;
			return i;
		}
		#endregion
	}
}
