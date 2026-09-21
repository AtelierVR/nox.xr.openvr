using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.CCK.XR;
using Nox.XR.Bindings;
using Nox.XR.Loaders;
using Nox.XR.OpenVR.Bindings;
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
		/// <summary>Bindings OpenVR, exposés à nox.xr tant que le loader est initialisé.</summary>
		private IBinding _binding;

		/// <summary>
		/// nox.xr s'initialise avant ses mods de loader : c'est ici qu'on lui signale le nôtre, et
		/// qu'on construit les bindings que <see cref="Binding"/> exposera.
		/// </summary>
		public void OnInitializeMain(IMainModCoreAPI api) {
			XRLoaderEditorRegistry.Register(this);

			// Les packs de bindings viennent des assets du mod : chargés avant tout Refresh().
			OpenVRBindingPacks.Load(api?.AssetAPI);
			_binding = new OpenVRBindings(api);
		}

		public void OnDisposeMain() {
			XRLoaderEditorRegistry.Unregister(this);
			_binding?.Clear();
			_binding = null;
			OpenVRBindingPacks.Clear();
		}

		/// <summary>
		/// Bindings du runtime OpenVR : c'est ce runtime qui les enregistre et répond aux lectures
		/// (<see cref="OpenVRBindings"/>), nox.xr ne fait que déclencher leur (re)liaison.
		/// </summary>
		public IBinding Binding
			=> _binding;

		/// <summary>Priorité du loader OpenVR : sous OpenXR (20), au-dessus du repli générique (0).</summary>
		public const int DefaultPriority = 10;

		/// <summary>
		/// Identifiant du loader. C'est aussi celui que nox.xr cherche côté bindings
		/// (<see cref="OpenVRBindingProvider.Id"/>), pour associer le provider au loader actif.
		/// </summary>
		public const string DefaultId = "openvr";

		public string Id
			=> DefaultId;

		public int Priority
			=> DefaultPriority;

		public bool IsSupported(Platform platform)
			=> IsPlatformSupported(platform);

		public XRLoader Loader
			=> XRLoaderAssets.Find<OpenVRLoader>();

		/// <summary>
		/// Indique si OpenVR est réellement utilisable ici : plateforme supportée <b>et</b> loader
		/// déclaré dans XR Plug-in Management. Sert au loader comme au provider de bindings.
		/// </summary>
		public static bool IsAvailable
			=> IsPlatformSupported(PlatformExtensions.CurrentPlatform)
				// Sans loader OpenVR configuré, XR Plug-in Management n'a rien à démarrer :
				// ce provider s'efface (`StartAsync<OpenVRLoader>()` échouerait de toute façon).
				&& XRManagementLoader.HasLoader<OpenVRLoader>();

		public bool IsValid
			=> IsAvailable;

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
