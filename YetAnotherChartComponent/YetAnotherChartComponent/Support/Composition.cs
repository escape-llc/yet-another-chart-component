using System;
using Windows.Foundation.Metadata;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;

namespace eScapeLLC.UWP.Charts.UniversalApiContract.v3 {
	/// <summary>
	/// Static helpers for composition APIs.
	/// ALL PUBLIC methods MUST check CONTRACT/VERSION!
	/// XAML and <see cref="Visual"/> sync:
	/// In general, SHOULD only modify the XAML version of properties (Offset, Opacity, Size), because that is the "natural" direction it flows.
	/// Changing a <see cref="Visual"/> directly can de-synchronize it with its XAML "partner" and cause issues.
	/// 
	/// The following is a "handy" reference for accessing APIs.
	/// APIs in v1:
	/// class <see cref="Compositor"/>
	/// CreateContainerVisual	CreateCubicBezierEasingFunction	CreateEffectFactory	CreateExpressionAnimation
	/// CreateInsetClip	CreateLinearEasingFunction	CreatePropertySet	CreateScalarKeyFrameAnimation
	/// CreateTargetForCurrentView	CreateVector(2/3/4)KeyFrameAnimation
	/// class <see cref="ElementCompositionPreview"/>
	/// APIs in v2:
	/// class <see cref="Compositor"/>
	/// CreateColorBrush	CreateColorKeyFrameAnimation	CreateQuaternionKeyFrameAnimation	CreateScopedBatch
	/// CreateSpriteVisual	CreateSurfaceBrush	GetCommitBatch
	/// class <see cref="ElementCompositionPreview"/>
	/// GetElementChildVisual	GetElementVisual	GetScrollViewerManipulationPropertySet	SetElementChildVisual
	/// APIs in v3:
	/// class <see cref="Compositor"/>
	/// CreateAmbientLight	CreateAnimationGroup	CreateBackdropBrush	CreateDistantLight	CreateDropShadow
	/// CreateImplictAnimationCollection	CreateLayerVisual	CreateMaskBrush	CreateNineGridBrush	CreatePointLight
	/// CreateSpotLight	CreateStepEasingFunction
	/// APIs in v4:
	/// CreateHostbackdropBrush
	/// class <see cref="ElementCompositionPreview"/>
	/// GetPointerPositionPropertySet	SetImplicitHideAnimation	SetImplicitShowAnimation	SetIsTranslationEnabled
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
		static readonly bool _supported = ApiInformation.IsApiContractPresent(CONTRACT, VERSION);
		/// <summary>
		/// Get whether we have API support.
		/// </summary>
		public static bool IsSupported { get => _supported; }
		#endregion
		#region internal (doesn't check CONTRACT)
		/// <summary>
		/// Create the <see cref="CompositionAnimationGroup"/>.
		/// Doesn't check for CONTRACT.
		/// </summary>
		/// <param name="compositor"></param>
		/// <returns>New instance.</returns>
		private static CompositionAnimationGroup CreateAnimationGroup(Compositor compositor) {
			// Define Offset Animation for the Animation group
			var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
			offsetAnimation.Target = nameof(Visual.Offset);
			offsetAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
			offsetAnimation.Duration = TimeSpan.FromMilliseconds(250);
			var animationGroup = compositor.CreateAnimationGroup();
			animationGroup.Add(offsetAnimation);
			return animationGroup;
		}
		#endregion
		#region external (MUST check CONTRACT)
		/// <summary>
		/// Attach implicit animations to given element.
		/// Creates new instances of everything.
		/// </summary>
		/// <param name="uix"></param>
		public static void AttachAnimations(UIElement uix) {
			if (!IsSupported) return;
			var elementVisual = ElementCompositionPreview.GetElementVisual(uix);
			var compositor = elementVisual.Compositor;
			var elementImplicitAnimation = compositor.CreateImplicitAnimationCollection();
			// Define trigger and animation that should play when the trigger is triggered. 
			elementImplicitAnimation[nameof(Visual.Offset)] = CreateAnimationGroup(compositor);
			elementVisual.ImplicitAnimations = elementImplicitAnimation;
		}
		/// <summary>
		/// Detach implicit animations from given element.
		/// </summary>
		/// <param name="uix"></param>
		public static void DetachAnimations(UIElement uix) {
			if (!IsSupported) return;
			var elementVisual = ElementCompositionPreview.GetElementVisual(uix);
			elementVisual.ImplicitAnimations = null;
		}
		#endregion
	}
}