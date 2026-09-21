using System;
using System.Collections.Generic;
using Nox.XR.Bindings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nox.XR.OpenVR.Bindings {
	/// <summary>
	/// Jeu de bindings d'un modèle de manette OpenVR : à quels contrôles d'Input System
	/// correspondent les bindings logiques de nox.xr sur ce device.
	///
	/// <para>
	/// OpenVR n'expose pas le layout standard <c>XRController</c> renseigné par OpenXR : le
	/// plugin Valve construit un layout par modèle (<c>ViveWand</c>, <c>OpenVRControllerIndex</c>,
	/// <c>OpenVROculusTouchController</c>, <c>OpenVRControllerWMR</c>, ...) dont les contrôles
	/// viennent du plugin natif. Un pack décrit donc, pour un modèle donné, la liste ordonnée des
	/// contrôles à essayer pour chaque binding (le premier qui existe réellement sur la manette
	/// connectée est retenu : voir <c>XRBindingPaths.Resolve</c>).
	/// </para>
	///
	/// <para>
	/// Les contrôles sont exprimés en <b>usages</b> (<c>{Trigger}</c>, <c>{Primary2DAxis}</c>,
	/// <c>{IndexFinger}</c>, ...) car les noms de contrôles d'un device OpenVR sont générés à
	/// partir des descripteurs du plugin natif, alors que les usages, eux, sont stables et
	/// documentés (<c>CommonUsages</c> / <c>kUnityXRInputFeatureUsage*</c>).
	/// </para>
	///
	/// <para>
	/// Un pack est un <b>asset</b> (<c>Assets/openvr/Bindings/*.asset</c>) référencé par le set
	/// <c>Packs.asset</c> : ajouter le support d'un nouveau casque ne demande aucune modification
	/// de code, seulement un pack et son ajout au set.
	/// </para>
	/// </summary>
	[CreateAssetMenu(menuName = "Nox/XR/OpenVR Binding Pack", fileName = "New OpenVR Binding Pack")]
	public sealed class OpenVRBindingPack : ScriptableObject {
		/// <summary>
		/// Contrôles candidats d'un binding logique.
		/// </summary>
		[Serializable]
		public struct Entry {
			[Tooltip("Binding logique de nox.xr visé.")]
			public XRBinding Key;

			[Tooltip("Contrôles d'Input System, par ordre de préférence : le premier qui existe réellement sur la manette connectée est retenu. "
			         + "Utiliser des usages ({Trigger}, {Primary2DAxis}, {IndexFinger}, ...), les noms de contrôles d'un device OpenVR n'étant pas stables.")]
			public string[] Controls;
		}

		[Header("Sélection du modèle")]
		[Tooltip("Priorité de sélection : la plus élevée gagne quand plusieurs packs correspondent au même device. Le pack générique doit rester le plus bas.")]
		[SerializeField] private int priority = 100;

		[Tooltip("Layout du device, utilisé pour construire les chemins (ex. ViveWand, OpenVRControllerIndex), et critère de correspondance. "
		         + "Vide = <XRController>, dont dérivent tous les contrôleurs XR.")]
		[SerializeField] private string layout = "";

		[Tooltip("Fragments du nom produit des devices concernés, insensible à la casse (ex. Knuckles). "
		         + "Vide = correspondance par layout uniquement ; layout et produits vides = pack générique, qui accepte tout device.")]
		[SerializeField] private string[] products = Array.Empty<string>();

		[Header("Bindings")]
		[Tooltip("Contrôles par binding logique. Un binding absent n'est pas lié par ce pack : le pack générique prend alors le relais.")]
		[SerializeField] private Entry[] bindings = Array.Empty<Entry>();

		private Dictionary<XRBinding, string[]> _controls;

		/// <summary>Priorité de sélection.</summary>
		public int Priority
			=> priority;

		/// <summary>Layout utilisé pour construire les chemins, ou vide pour <c>&lt;XRController&gt;</c>.</summary>
		public string Layout
			=> layout;

		/// <summary>Fragments de nom produit acceptés.</summary>
		public IReadOnlyList<string> Products
			=> products ?? Array.Empty<string>();

		/// <summary>Fragments de layout acceptés.</summary>
		public IReadOnlyList<string> MatchLayouts
			=> string.IsNullOrWhiteSpace(layout) ? Array.Empty<string>() : new[] { layout };

		/// <summary>
		/// Vrai si ce pack n'a aucun critère de correspondance : il accepte tous les devices.
		/// C'est le pack générique, utilisé pour tout modèle non reconnu et en complément d'un
		/// pack spécifique (voir <see cref="OpenVRBindingPacks.Generic"/>).
		/// </summary>
		public bool IsCatchAll
			=> Products.Count == 0 && MatchLayouts.Count == 0;

		/// <summary>
		/// Contrôles candidats pour <paramref name="binding"/>, ou une liste vide si ce pack ne
		/// prévoit rien pour ce binding.
		/// </summary>
		public IReadOnlyList<string> Controls(XRBinding binding)
			=> Lookup.TryGetValue(binding, out var controls)
				? controls
				: Array.Empty<string>();

		/// <summary>
		/// Table binding → contrôles, construite depuis les entrées sérialisées puis mise en
		/// cache (invalidée à l'activation et à chaque modification dans l'inspecteur).
		/// </summary>
		private Dictionary<XRBinding, string[]> Lookup {
			get {
				if (_controls != null)
					return _controls;

				_controls = new Dictionary<XRBinding, string[]>();
				foreach (var entry in bindings ?? Array.Empty<Entry>())
					if (entry.Controls is { Length: > 0 })
						_controls[entry.Key] = entry.Controls;

				return _controls;
			}
		}

		private void OnEnable()
			=> _controls = null;

#if UNITY_EDITOR
		private void OnValidate()
			=> _controls = null;
#endif

		/// <summary>
		/// Indique si ce pack décrit <paramref name="device"/> (par nom produit ou par layout).
		/// Un pack sans critère (<see cref="IsCatchAll"/>) accepte tous les devices : c'est le
		/// pack générique de repli.
		/// </summary>
		public bool Matches(InputDevice device) {
			if (device == null)
				return false;

			if (IsCatchAll)
				return true;

			var product = device.description.product;
			foreach (var candidate in Products)
				if (!string.IsNullOrWhiteSpace(candidate) && Contains(product, candidate))
					return true;

			var layout = device.layout;
			foreach (var candidate in MatchLayouts)
				if (!string.IsNullOrWhiteSpace(candidate) && Contains(layout, candidate))
					return true;

			return false;
		}

		public override string ToString()
			=> $"{GetType().Name}[{name}, priority {Priority}, layout {(string.IsNullOrWhiteSpace(Layout) ? "XRController" : Layout)}]";

		private static bool Contains(string haystack, string needle)
			=> !string.IsNullOrEmpty(haystack)
				&& haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
	}
}
