using System;
using System.Collections.Generic;
using System.Linq;
using Nox.XR.Bindings;
using Nox.XR.OpenVR.Bindings;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.XR;

namespace Nox.XR.OpenVR {
	/// <summary>
	/// Chemins de bindings OpenVR : quel contrôle d'Input System alimente chaque binding logique,
	/// selon le modèle de manette réellement connecté.
	///
	/// <para>
	/// Contrairement à OpenXR, OpenVR n'expose pas un layout unique : le plugin Valve construit un
	/// layout par modèle de manette (<c>ViveWand</c>, <c>OpenVRControllerIndex</c>,
	/// <c>OpenVROculusTouchController</c>, <c>OpenVRControllerWMR</c>, ...) dont les contrôles
	/// viennent du plugin natif. On choisit donc le <see cref="OpenVRBindingPack"/> du modèle
	/// connecté à chaque main, puis on compose le chemin du binding demandé — avec repli sur les
	/// usages génériques si le modèle n'expose pas le contrôle.
	/// </para>
	///
	/// <para>
	/// Les packs sont des assets du mod (<c>Assets/openvr/Bindings/*.asset</c>, listés par
	/// <c>Packs.asset</c>) : en ajouter un suffit à supporter un nouveau casque.
	/// </para>
	///
	/// <para>
	/// Utilisé par <see cref="OpenVRBindings"/>, qui enregistre ces chemins et répond aux lectures.
	/// </para>
	/// </summary>
	internal static class OpenVRBindingProvider {
		/// <summary>
		/// Bindings OpenVR à lier, avec le chemin retenu pour la manette connectée à la main visée.
		/// </summary>
		public static IEnumerable<(XRBinding Binding, string Path)> GetBindings() {
			// Une seule interrogation des devices pour toute la livraison.
			var packs = new Dictionary<XRBindingHand, OpenVRBindingPack> {
				{ XRBindingHand.Left, PackFor(XRBindingHand.Left) },
				{ XRBindingHand.Right, PackFor(XRBindingHand.Right) },
			};

			foreach (XRBinding binding in Enum.GetValues(typeof(XRBinding))) {
				var path = ResolvePath(binding, packs[binding.GetHand()]);
				if (string.IsNullOrWhiteSpace(path))
					continue;

				yield return (binding, path);
			}
		}

		/// <summary>
		/// Pack décrivant la manette qui porte <paramref name="hand"/> — le pack générique quand
		/// elle n'est pas encore connectée, ses chemins en usages se liant à son arrivée.
		/// </summary>
		private static OpenVRBindingPack PackFor(XRBindingHand hand)
			=> OpenVRBindingPacks.Match(FindDevice(hand));

		/// <summary>
		/// Chemin du binding pour <paramref name="pack"/>, complété par les candidats génériques.
		/// </summary>
		private static string ResolvePath(XRBinding binding, OpenVRBindingPack pack) {
			var hand    = binding.GetHand();
			var generic = OpenVRBindingPacks.Generic;

			var candidates = new List<string>();
			foreach (var control in pack?.Controls(binding) ?? Array.Empty<string>())
				candidates.Add(XRBindingPaths.Build(hand, pack.Layout, control));

			// Le pack du modèle est prioritaire, mais un contrôle qu'il ne connaît pas peut
			// exister sous son usage générique : on complète avec le pack générique.
			if (generic != null && !ReferenceEquals(pack, generic))
				foreach (var control in generic.Controls(binding))
					candidates.Add(XRBindingPaths.Build(hand, null, control));

			return XRBindingPaths.Resolve(candidates);
		}

		/// <summary>
		/// Manette qui porte <paramref name="hand"/>, ou <c>null</c> si elle n'est pas (encore)
		/// connectée — cas normal quand le casque se connecte avant les manettes.
		///
		/// <para>
		/// Les trackers de full body tracking sont volontairement écartés : un tracker Vive peut
		/// porter l'usage <c>LeftHand</c>/<c>RightHand</c> (et exposer un trigger) sans être la
		/// manette du joueur, il ne doit donc pas capter les bindings de la main.
		/// </para>
		/// </summary>
		private static InputDevice FindDevice(XRBindingHand hand) {
			var usage = new InternedString(XRBindingPaths.GetUsage(hand));

			InputDevice tracker = null;
			foreach (var device in InputSystem.devices) {
				if (device is not XRController)
					continue;
				if (!device.usages.Any(u => u == usage))
					continue;

				if (IsTracker(device)) {
					tracker ??= device;
					continue;
				}

				return device;
			}

			return tracker;
		}

		/// <summary>
		/// Reconnaît un tracker (Vive tracker, tracker de pied...) à son produit ou à son layout.
		/// </summary>
		private static bool IsTracker(InputDevice device)
			=> Contains(device?.description.product, "tracker")
				|| Contains(device?.layout, "tracker");

		private static bool Contains(string haystack, string needle)
			=> !string.IsNullOrEmpty(haystack)
				&& haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
	}
}
