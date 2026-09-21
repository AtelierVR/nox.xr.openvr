using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Mods.Assets;
using UnityEngine.InputSystem;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.XR.OpenVR.Bindings {
	/// <summary>
	/// Registre des <see cref="OpenVRBindingPack"/> connus.
	///
	/// <para>
	/// Les packs sont des <b>assets</b> vivant à côté du loader :
	/// <c>Assets/openvr/Bindings/*.asset</c>, listés par
	/// <c>Assets/openvr/Bindings/Packs.asset</c> (<see cref="AssetSet"/>). Ajouter le support
	/// d'un nouveau modèle de manette = créer un <see cref="OpenVRBindingPack"/> et l'ajouter au
	/// set, sans toucher à nox.xr ni à un seul fichier de code.
	/// </para>
	///
	/// <para>
	/// <see cref="Register"/> reste disponible pour un mod qui voudrait fournir un pack sans
	/// asset (il écrase alors le pack de même nom).
	/// </para>
	/// </summary>
	public static class OpenVRBindingPacks {
		/// <summary>
		/// Set de packs livré par le mod. L'espace de noms <c>openvr</c> est le dossier sous
		/// <c>Assets/</c> : c'est la convention de l'API d'assets des mods
		/// (<c>&lt;Assets&gt;/&lt;namespace&gt;/&lt;chemin&gt;</c>).
		/// </summary>
		public const string AssetSet = "openvr:Bindings/Packs.asset";

		private static readonly List<OpenVRBindingPack> Registered = new();

		/// <summary>Packs connus, du plus spécifique au plus générique.</summary>
		public static IReadOnlyList<OpenVRBindingPack> All
			=> Registered
				.OrderByDescending(p => p.Priority)
				.ToList();

		/// <summary>
		/// Pack générique (sans critère de correspondance) : utilisé pour tout device qu'aucun
		/// pack spécifique ne reconnaît, et complément d'un pack spécifique qui ne prévoit rien
		/// pour un binding. <c>null</c> si le set ne le contient pas.
		/// </summary>
		public static OpenVRBindingPack Generic
			=> All.LastOrDefault(p => p.IsCatchAll);

		/// <summary>
		/// Charge le set de packs du mod. Appelé par <c>OpenVRLoaderProvider</c> à l'initialisation
		/// du loader, une fois les assets du mod enregistrés.
		/// </summary>
		public static void Load(IAssetAPI assets) {
			Clear();

			var set = assets?.GetAsset<OpenVRBindingPackSet>(AssetSet);
			if (set == null) {
				Logger.LogError($"OpenVR binding pack set '{AssetSet}' not found: OpenVR controllers will have no binding.");
				return;
			}

			foreach (var pack in set.Packs)
				Register(pack);

			Logger.LogDebug($"OpenVR binding packs loaded: {string.Join(", ", All.Select(p => $"{p.name} ({p.Priority})"))}");
		}

		/// <summary>
		/// Oublie les packs connus (les assets restent valides, c'est le mod qui s'arrête).
		/// </summary>
		public static void Clear()
			=> Registered.Clear();

		/// <summary>
		/// Ajoute (ou remplace, si un pack porte déjà ce nom) un pack de bindings.
		/// </summary>
		public static void Register(OpenVRBindingPack pack) {
			if (pack == null || string.IsNullOrWhiteSpace(pack.name))
				return;

			Unregister(pack.name);
			Registered.Add(pack);
		}

		/// <summary>
		/// Retire un pack par son nom.
		/// </summary>
		/// <returns><c>true</c> si un pack a été retiré.</returns>
		public static bool Unregister(string name)
			=> !string.IsNullOrWhiteSpace(name) && Registered.RemoveAll(p => p.name == name) > 0;

		/// <summary>
		/// Pack décrivant <paramref name="device"/>, ou <see cref="Generic"/> si aucun ne le
		/// reconnaît (ou si le device n'est pas encore connu : les chemins du pack générique sont
		/// des usages, ils se lieront dès qu'une manette apparaîtra).
		/// </summary>
		public static OpenVRBindingPack Match(InputDevice device)
			=> device == null
				? Generic
				: All.FirstOrDefault(pack => pack.Matches(device)) ?? Generic;
	}
}
