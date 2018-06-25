using System;
using Windows.Foundation.Metadata;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;

namespace eScapeLLC.UWP.Charts {
	/// <summary>
	/// Static helpers for basic <see cref="Compositor"/> support, which is V1.
	/// </summary>
	public static class CompositorSupport {
		/// <summary>
		/// Create the <see cref="CompositionAnimationGroup"/>.
		/// Doesn't check for CONTRACT.
		/// </summary>
		/// <param name="compositor"></param>
		/// <param name="target">The animation target.</param>
		/// <param name="duration">Duration of offset animation in MS.</param>
		/// <returns>New instance.</returns>
		public static CompositionAnimationGroup CreateOffsetAnimation(this Compositor compositor, String target, int duration) {
			var animation = compositor.CreateVector3KeyFrameAnimation();
			animation.Target = target;
			animation.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
			animation.Duration = TimeSpan.FromMilliseconds(duration);
			var animationGroup = compositor.CreateAnimationGroup();
			animationGroup.Add(animation);
			return animationGroup;
		}
		/// <summary>
		/// Create the <see cref="CompositionAnimationGroup"/>.
		/// Doesn't check for CONTRACT.
		/// </summary>
		/// <param name="compositor"></param>
		/// <param name="fadein">true: 0..1; false: 1..0.</param>
		/// <param name="duration">Duration of animation in MS.</param>
		/// <returns>New instance.</returns>
		public static CompositionAnimationGroup CreateOpacityAnimation(this Compositor compositor, bool fadein, int duration) {
			var animation = compositor.CreateScalarKeyFrameAnimation();
			animation.Target = nameof(Visual.Opacity);
			if(fadein) {
				animation.InsertKeyFrame(0f, 0f);
				animation.InsertKeyFrame(1f, 1f);
			}
			else {
				animation.InsertKeyFrame(0f, 1f);
				animation.InsertKeyFrame(1f, 0f);
			}
			animation.Duration = TimeSpan.FromMilliseconds(duration);
			animation.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
			var animationGroup = compositor.CreateAnimationGroup();
			animationGroup.Add(animation);
			return animationGroup;
		}
	}
}
namespace eScapeLLC.UWP.Charts.UniversalApiContract.v3 {
	/// <summary>
	/// Static helpers for composition APIs.
	/// ALL PUBLIC methods MUST check CONTRACT/VERSION!
	/// XAML and <see cref="Visual"/> sync:
	/// In general, SHOULD only modify the XAML version of properties (Offset, Opacity, Size), because that is the "natural" direction it flows.
	/// Changing a <see cref="Visual"/> directly can de-synchronize it with its XAML "partner" and cause issues.
	/// <para/>
	/// The following is a "handy" reference for accessing APIs.
	/// <para/>
	/// APIs in v1:
	/// class <see cref="Compositor"/>, <see cref="CompositionObject"/>, <see cref="CompositionAnimation"/>
	/// CreateContainerVisual	CreateCubicBezierEasingFunction	CreateEffectFactory	CreateExpressionAnimation
	/// CreateInsetClip	CreateLinearEasingFunction	CreatePropertySet	CreateScalarKeyFrameAnimation
	/// CreateTargetForCurrentView	CreateVector(2/3/4)KeyFrameAnimation
	/// class <see cref="ElementCompositionPreview"/>
	/// <para/>
	/// APIs in v2:
	/// class <see cref="Compositor"/>
	/// CreateColorBrush	CreateColorKeyFrameAnimation	CreateQuaternionKeyFrameAnimation	CreateScopedBatch
	/// CreateSpriteVisual	CreateSurfaceBrush	GetCommitBatch
	/// class <see cref="ElementCompositionPreview"/>
	/// GetElementChildVisual	GetElementVisual	GetScrollViewerManipulationPropertySet	SetElementChildVisual
	/// <para/>
	/// APIs in v3:
	/// class <see cref="Compositor"/>
	/// CreateAmbientLight	CreateAnimationGroup	CreateBackdropBrush	CreateDistantLight	CreateDropShadow
	/// CreateImplictAnimationCollection	CreateLayerVisual	CreateMaskBrush	CreateNineGridBrush	CreatePointLight
	/// CreateSpotLight	CreateStepEasingFunction
	/// <para/>
	/// APIs in v4:
	/// CreateHostbackdropBrush
	/// class <see cref="ElementCompositionPreview"/>
	/// GetPointerPositionPropertySet	SetImplicitHideAnimation	SetImplicitShowAnimation	SetIsTranslationEnabled
	/// <para/>
	/// APIs in v5, v6:
	/// Not Listed.
	/// </summary>
	public static class CompositionSupport {
		#region API contract info
		/// <summary>
		/// Where to look for contract.
		/// </summary>
		public const String CONTRACT = "Windows.Foundation.UniversalApiContract";
		/// <summary>
		/// Version base APIs appear in.
		/// </summary>
		public const int VERSION = 3;
		/// <summary>
		/// Get whether we have API support.
		/// </summary>
		public static bool IsSupported => ApiInformation.IsApiContractPresent(CONTRACT, VERSION);
		#endregion
		#region external (MUST check CONTRACT)
		/// <summary>
		/// Attach implicit animations to given element.
		/// Creates new instances of everything.
		/// </summary>
		/// <param name="uix">Target element.</param>
		/// <param name="duration">Duration in MS.</param>
		public static void AttachAnimations(UIElement uix, int duration) {
			if (!IsSupported) return;
			var ev = ElementCompositionPreview.GetElementVisual(uix);
			var compositor = ev.Compositor;
			var collection = compositor.CreateImplicitAnimationCollection();
			var prop = nameof(Visual.Offset);
			collection[prop] = compositor.CreateOffsetAnimation(prop, duration);
			ev.ImplicitAnimations = collection;
		}
		/// <summary>
		/// Detach implicit animations from given element.
		/// </summary>
		/// <param name="uix">Target element.</param>
		public static void DetachAnimations(UIElement uix) {
			if (!IsSupported) return;
			var elementVisual = ElementCompositionPreview.GetElementVisual(uix);
			elementVisual.ImplicitAnimations = null;
		}
		#endregion
	}
}
namespace eScapeLLC.UWP.Charts.UniversalApiContract.v4 {
	/// <summary>
	/// Support class for Composition v4 APIs.
	/// <para/>
	/// CreateHostbackdropBrush, class <see cref="ElementCompositionPreview"/>,
	/// GetPointerPositionPropertySet, SetImplicitHideAnimation, SetImplicitShowAnimation, SetIsTranslationEnabled
	/// </summary>
	public static class CompositionSupport {
		#region API contract info
		/// <summary>
		/// Where to look for contract.
		/// </summary>
		public const String CONTRACT = "Windows.Foundation.UniversalApiContract";
		/// <summary>
		/// Version base APIs appear in.
		/// </summary>
		public const int VERSION = 4;
		/// <summary>
		/// Get whether we have API support.
		/// </summary>
		public static bool IsSupported => ApiInformation.IsApiContractPresent(CONTRACT, VERSION);
		#endregion
		#region external (MUST check CONTRACT)
		/// <summary>
		/// Attach implicit show/hide animations to given element.
		/// Creates new instances of everything.
		/// </summary>
		/// <param name="uix"></param>
		/// <param name="duration">Fade duration in MS.</param>
		public static void AttachAnimations(UIElement uix, int duration) {
			if (!IsSupported) return;
			var compositor = ElementCompositionPreview.GetElementVisual(uix).Compositor;
			var show = compositor.CreateOpacityAnimation(true, duration);
			ElementCompositionPreview.SetImplicitShowAnimation(uix, show);
			var hide = compositor.CreateOpacityAnimation(false, duration);
			ElementCompositionPreview.SetImplicitHideAnimation(uix, hide);
		}
		/// <summary>
		/// Detach implicit animations from given element.
		/// </summary>
		/// <param name="uix"></param>
		public static void DetachAnimations(UIElement uix) {
			if (!IsSupported) return;
			//var elementVisual = ElementCompositionPreview.GetElementVisual(uix);
			ElementCompositionPreview.SetImplicitShowAnimation(uix, null);
			ElementCompositionPreview.SetImplicitHideAnimation(uix, null);
		}
		/// <summary>
		/// Enable translation.
		/// </summary>
		/// <param name="uix"></param>
		public static void EnableTranslation(UIElement uix) {
			if (!IsSupported) return;
			ElementCompositionPreview.SetIsTranslationEnabled(uix, true);
		}
		#endregion
	}
}
