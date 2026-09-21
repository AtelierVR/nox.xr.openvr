using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.XR.Loaders;
using Nox.XR.Runtime.Loaders;
using Unity.XR.OpenVR;

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
	/// </summary>
	public sealed class OpenVRLoaderProvider : IXRLoaderProvider, IMainModInitializer {
		/// <summary>Priorité du loader OpenVR : sous OpenXR (20), au-dessus du repli générique (0).</summary>
		public const int DefaultPriority = 10;

		public string Id
			=> "openvr";

		public int Priority
			=> DefaultPriority;

		public bool IsValid {
			get {
				if (!IsPlatformSupported(PlatformExtensions.CurrentPlatform))
					return false;

				// Le loader doit être celui configuré dans XR Plug-in Management, sinon
				// XRManagementLoader.StartAsync démarrerait autre chose que ce qu'on annonce.
				return XRManagementLoader.HasLoader<OpenVRLoader>();
			}
		}

		/// <summary>
		/// Le plugin OpenVR de Valve ne fournit de loader que pour Windows et Linux.
		/// </summary>
		public static bool IsPlatformSupported(Platform platform)
			=> platform is Platform.Windows or Platform.Linux;

		public UniTask<bool> InitializeAsync()
			=> XRManagementLoader.StartAsync();

		public void Deinitialize()
			=> XRManagementLoader.Stop();
	}
}
