using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nox.XR.OpenVR.Bindings {
	/// <summary>
	/// Liste des <see cref="OpenVRBindingPack"/> livrés par le mod
	/// (<c>Assets/openvr/Bindings/Packs.asset</c>).
	///
	/// <para>
	/// Le jeu de packs est un asset pour rester éditable sans recompiler : on ajoute le support
	/// d'un nouveau casque en créant un pack et en l'ajoutant ici.
	/// </para>
	/// </summary>
	[CreateAssetMenu(menuName = "Nox/XR/OpenVR Binding Pack Set", fileName = "Packs")]
	public sealed class OpenVRBindingPackSet : ScriptableObject {
		[Tooltip("Packs de bindings OpenVR. Un pack sans critère de correspondance (ni layout, ni produit) est le pack générique, utilisé pour les modèles inconnus.")]
		[SerializeField] private OpenVRBindingPack[] packs = Array.Empty<OpenVRBindingPack>();

		/// <summary>Packs du set, dans l'ordre de l'inspecteur.</summary>
		public IReadOnlyList<OpenVRBindingPack> Packs
			=> packs ?? Array.Empty<OpenVRBindingPack>();
	}
}
