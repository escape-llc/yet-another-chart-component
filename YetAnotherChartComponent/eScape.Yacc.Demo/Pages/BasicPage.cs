using eScape.Core;
using eScape.Core.Host;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Yacc.Demo.Pages {
	/// <summary>
	/// Basic page that handles IRequireRefresh and IRequireRelease/IRequireReleaseAsync interfaces.
	/// </summary>
	public abstract class BasicPage : Page {
		static LogTools.Flag _trace = LogTools.Add("BasicPage", LogTools.Level.Error);
		protected abstract Task<object> InitializeDataContextAsync();
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
		protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e) {
			try {
				if (DataContext is IRequireReleaseAsync irra) {
					await irra.ReleaseAsync();
				}
				if (DataContext is IRequireRelease irr) {
					irr.Release();
				}
			} catch (Exception ex) {
				_trace.Error($"OnNavigatingFrom.unhandled ${ex}");
			} finally {
				base.OnNavigatingFrom(e);
			}
		}
	}
}
