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
		private OpenVRBindings _binding;

		/// <summary><c>true</c> une fois le loader OpenVR démarré par <see cref="Initialize"/>.</summary>
		private bool _started;

		/// <summary>
		/// nox.xr s'initialise <b>avant</b> ses mods de loader : son initialiseur client peut donc
		/// démarrer la XR via <see cref="Initialize"/> avant que cet initialiseur principal ne
		/// construise les bindings que <see cref="Binding"/> exposera. D'où la création à la demande
		/// (<see cref="EnsureBindings"/>).
		/// </summary>
		public void OnInitializeMain(IMainModCoreAPI api) {
			XRLoaderEditorRegistry.Register(this);

			// Les bindings sont créés par `Initialize` : s'ils sont déjà là, ils sont déjà
			// initialisés. Sinon on les crée, et on ne les initialise tout de suite que si la XR
			// tourne déjà (sinon c'est `Initialize` qui s'en chargera).
			var created = _binding == null;
			EnsureBindings();

			if (created && _started)
				_binding.Initialize().Forget();
		}

		public void OnDisposeMain() {
			XRLoaderEditorRegistry.Unregister(this);
			_binding?.Dispose();
			_binding = null;
			_started = false;
		}

		/// <summary>
		/// Crée les bindings si besoin. Contrairement à OpenXR, ils ne dépendent d'aucune API :
		/// ils peuvent donc être créés avant même l'initialiseur principal.
		/// </summary>
		private OpenVRBindings EnsureBindings()
			=> _binding ??= new OpenVRBindings(this);

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
			=> platform == Platform.Windows 
				|| platform == Platform.Linux;

		/// <summary>
		/// Démarre OpenVR, puis initialise nos bindings (SteamVR + action set).
		///
		/// <para>
		/// L'initialisation des bindings se fait ici et non à la construction du provider : les
		/// actions SteamVR n'existent qu'une fois le runtime démarré. Et comme nox.xr démarre la XR
		/// avant l'initialiseur principal de ce mod, <c>_binding</c> peut encore être nul à ce
		/// moment — c'est ce qui provoquait le <c>NullReferenceException</c> qui faisait échouer ce
		/// provider au profit du repli générique.
		/// </para>
		/// </summary>
		public async UniTask<bool> Initialize() {
			if (!await XRManagementLoader.StartAsync<OpenVRLoader>())
				return false;

			_started = true;
			await EnsureBindings().Initialize();
			return true;
		}

		/// <summary>
		/// Arrête OpenVR, puis vide nos bindings : ils pointent sur des actions liées aux devices
		/// que XR Plug-in Management vient de détruire.
		/// </summary>
		public async UniTask Deinitialize() {
			_started = false;
			await XRManagementLoader.Stop();

			if (_binding != null)
				await _binding.Deinitialize();
		}
	}
}
