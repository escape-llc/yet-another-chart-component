using System;
using System.ComponentModel;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace eScapeLLC.UWP.Charts {
	#region ViewModelBase
	/// <summary>
	/// Very lightweight VM base class.
	/// </summary>
	public abstract class ViewModelBase : INotifyPropertyChanged {
		/// <summary>
		/// Implemented for <see cref="Windows.UI.Xaml.Data.INotifyPropertyChanged"/>.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;
		#region helpers
		/// <summary>
		/// Hit the <see cref="PropertyChanged"/> event.
		/// </summary>
		/// <param name="prop">Property that changed.</param>
		protected void Changed(String prop) {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
		}
		#endregion
	}
	#endregion
	#region Shims
	#region DataTemplateShim
	/// <summary>
	/// "Internal" VM used as the <see cref="FrameworkElement.DataContext"/> for a <see cref="DataTemplate"/> used by a <see cref="ChartComponent"/>.
	/// </summary>
	public class DataTemplateShim : ViewModelBase {
		#region data
		Visibility _vis;
		#endregion
		#region properties
		/// <summary>
		/// Current visibility.
		/// </summary>
		public Visibility Visibility { get { return _vis; } set { _vis = value; Changed(nameof(Visibility)); } }
		#endregion
	}
	#endregion
	#region TextShim
	/// <summary>
	/// VM for a text label context.
	/// </summary>
	public class TextShim : DataTemplateShim {
		#region data
		String _text;
		#endregion
		#region properties
		/// <summary>
		/// Current text.
		/// </summary>
		public String Text { get { return _text; } set { _text = value; Changed(nameof(Text)); } }
		#endregion
	}
	#endregion
	#region ObjectShim
	/// <summary>
	/// VM shim for a custom label context.
	/// </summary>
	public class ObjectShim : TextShim {
		#region data
		object _value;
		#endregion
		#region properties
		/// <summary>
		/// Additional custom state.
		/// </summary>
		public object CustomValue { get { return _value; } set { _value = value; Changed(nameof(CustomValue)); } }
		#endregion
	}
	#endregion
	#region GeometryShim<G>
	/// <summary>
	/// VM shim for a path.
	/// Set the <see cref="PathData"/> property to bind the <see cref="Transform"/> to <see cref="Geometry"/>.
	/// </summary>
	/// <typeparam name="G">Geometry expected.</typeparam>
	public class GeometryShim<G> : DataTemplateShim where G : Geometry {
		#region data
		G _gx;
		Transform _matx;
		#endregion
		#region properties
		/// <summary>
		/// Render transform origin.
		/// Does Not do change notification.
		/// Default is (.5,.5) in NDC.
		/// </summary>
		public Point RenderTransformOrigin { get; set; } = new Point(.5, .5);
		/// <summary>
		/// Path geometry.
		/// Also sets binding between <see cref="GeometryTransform"/> and <see cref="Geometry.Transform"/>.
		/// </summary>
		public G PathData {
			get { return _gx; }
			set {
				_gx = value;
				Changed(nameof(PathData));
				if (_gx != null) {
					// bind our GeometryTransform property to the PathData.Transform property
					ChartComponent.BindTo(this, nameof(GeometryTransform), _gx, Geometry.TransformProperty);
				}
			}
		}
		/// <summary>
		/// Geometry transform.
		/// Also set <see cref="Geometry.Transform"/> if <see cref="PathData"/> is set.
		/// </summary>
		public Transform GeometryTransform {
			get { return _matx; }
			set {
				_matx = value;
				Changed(nameof(GeometryTransform));
			}
		}
		#endregion
		#region public
		/// <summary>
		/// Force a notify on <see cref="PathData"/>.
		/// Use this method if changing "internal" parts of the <see cref="PathData"/>, e.g. <see cref="RectangleGeometry.Rect"/>.
		/// </summary>
		public void GeometryUpdated() { Changed(nameof(PathData)); }
		#endregion
	}
	#endregion
	#endregion
}
