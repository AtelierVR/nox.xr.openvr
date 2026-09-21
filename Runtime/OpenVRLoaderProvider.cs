using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.CCK.XR;
using Nox.XR.Loaders;
using Unity.XR.OpenVR;
using UnityEngine.XR.Management;

namespace Nox.XR.OpenVR {
	/// <summary>
	/// Loader OpenVR (SteamVR) pour nox.xr.
	///
	/// <para>
	/// C'est le loader de repli de nox.xr sur Windows (OpenXR reste prioritaire) et le loader
	/// utilisé sur Linux, où Unity ne fournit pas de plugin OpenXR. Le plugin Valve
	/// <c>com.valvesoftware.unity.openvr</c> fournit le rendu ; l'input vient de SteamVR.
	/// </para>
	///
	/// <para>
	/// L'assembly n'est compilée que si le paquet Valve est installé
	/// (<c>defineConstraints: NOX_HAS_OPENVR</c>) : sinon nox.xr retombe simplement sur
	/// <c>XRManagementLoaderProvider</c>.
	/// </para>
	///
	/// <para>
	/// nox.xr ne démarre jamais « le premier loader qui répond » : <see cref="Initialize"/> impose
	/// <see cref="OpenVRLoader"/>, donc ce provider ne peut pas se retrouver à piloter OpenXR.
	/// </para>
	/// </summary>
	public sealed class OpenVRLoaderProvider : IXRLoaderEditorProvider, IMainModInitializer {
		/// <summary>
		/// nox.xr s'initialise avant ses mods de loader : c'est ici qu'on lui signale le nôtre.
		/// </summary>
		public void OnInitializeMain(IMainModCoreAPI api)
			=> XRLoaderEditorRegistry.Register(this);

		public void OnDisposeMain()
			=> XRLoaderEditorRegistry.Unregister(this);

		/// <summary>Priorité du loader OpenVR : sous OpenXR (20), au-dessus du repli générique (0).</summary>
		public const int DefaultPriority = 10;

		public string Id
			=> "openvr";

		public int Priority
			=> DefaultPriority;

		public bool IsSupported(Platform platform)
			=> IsPlatformSupported(platform);

		public XRLoader Loader
			=> XRLoaderAssets.Find<OpenVRLoader>();

		public bool IsValid
			=> IsPlatformSupported(PlatformExtensions.CurrentPlatform)
				// Sans loader OpenVR configuré, XR Plug-in Management n'a rien à démarrer :
				// ce provider s'efface (`StartAsync<OpenVRLoader>()` échouerait de toute façon).
				&& XRManagementLoader.HasLoader<OpenVRLoader>();

		/// <summary>
		/// Le plugin OpenVR de Valve ne fournit de loader que pour Windows et Linux.
		/// </summary>
		public static bool IsPlatformSupported(Platform platform)
			=> platform is Platform.Windows or Platform.Linux;

		public UniTask<bool> Initialize()
			=> XRManagementLoader.StartAsync<OpenVRLoader>();

		public UniTask Deinitialize()
			=> XRManagementLoader.Stop();
	}
}
