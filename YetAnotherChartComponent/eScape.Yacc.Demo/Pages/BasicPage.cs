using eScape.Core;
using eScape.Core.Host;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Yacc.Demo.Pages {
	/// <summary>
	/// Basic page that handles VM creation, <see cref="IRequireRefresh "/> and <see cref="IRequireRelease"/>/<see cref="IRequireReleaseAsync"/> interfaces.
	/// You MUST declare your XAML page element as "local:BasicPage" where "local" is the namespace of your <see cref="Page"/> subclass.
	/// Your <see cref="Page"/> subclass must also derive from <see cref="BasicPage"/>.  You MAY have transient compile errors while making these edits.
	/// If a subclass needs to release resources itself after the VM, it should also implement <see cref="IRequireReleaseAsync"/> or <see cref="IRequireRelease"/> as appropriate.
	/// </summary>
	public abstract class BasicPage : Page {
		static LogTools.Flag _trace = LogTools.Add("BasicPage", LogTools.Level.Error);
		/// <summary>
		/// Create and initialize the view model.
		/// When overriding, MUST declare method as async!
		/// If the override ends up NOT being async, you can use "#pragma warning disable/restore 1998" around it to suppress warning.
		/// </summary>
		/// <returns>The view model.</returns>
		protected abstract Task<object> InitializeDataContextAsync();
		/// <summary>
		/// Create the VM and call down IRequireRefresh if necessary.
		/// </summary>
		/// <param name="e"></param>
		protected override async void OnNavigatedTo(NavigationEventArgs e) {
			base.OnNavigatedTo(e);
			try {
				DataContext = await InitializeDataContextAsync();
				if (DataContext == null)
					_trace.Error($"{nameof(DataContext)} was NULL");
				if (DataContext is IRequireRefresh irr) {
					await irr.RefreshAsync();
				}
			}
			catch(Exception ex) {
				_trace.Error($"OnNavigatedTo.unhandled ${ex}");
			}
		}
		/// <summary>
		/// Call down <see cref="IRequireReleaseAsync"/>/<see cref="IRequireRelease"/> if necessary.
		/// </summary>
		/// <param name="e"></param>
		protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e) {
			try {
				// VM first
				if (DataContext is IRequireReleaseAsync irra) {
					await irra.ReleaseAsync();
				}
				else if (DataContext is IRequireRelease irr) {
					irr.Release();
				}
				// now the page
				if(this is IRequireReleaseAsync pirra) {
					await pirra.ReleaseAsync();
				}
				else if(this is IRequireRelease pirr) {
					pirr.Release();
				}
			} catch (Exception ex) {
				_trace.Error($"OnNavigatingFrom.unhandled ${ex}");
			} finally {
				base.OnNavigatingFrom(e);
			}
		}
	}
}
