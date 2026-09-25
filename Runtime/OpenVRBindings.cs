using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nox.XR.Bindings;
using UnityEngine;
using Valve.VR;
using Logger = Nox.CCK.Utils.Logger;


namespace Nox.XR.OpenVR.Bindings {
    /// <summary>
    /// Implémentation IBinding pour le runtime OpenVR (SteamVR).
    /// </summary>
    public sealed class OpenVRBindings : IBinding, IDisposable {
        private readonly OpenVRLoaderProvider _provider;

        /// <summary>Handles for SteamVR actions, by action path.</summary>
        private readonly Dictionary<string, SteamVR_Action> _handles = new();

        public OpenVRBindings(OpenVRLoaderProvider provider)
            => _provider = provider;

        public async UniTask Initialize() {
            SteamVR.Initialize();
            SteamVR_Input.Initialize();
            await UniTask.CompletedTask;
        }

        public T Get<T>(string path) where T : struct {
            if (!_handles.TryGetValue(path, out SteamVR_Action action)) {
                if (SteamVR_Input.actions != null)
                    foreach (var a in SteamVR_Input.actions)
                        if (a.fullPath == path || a.fullPath.EndsWith(path)) {
                            action = a;
                            break;
                        }

                if (action != null)
                    _handles[path] = action;
                else return default;
            }

            try {
                // Determine the input source from the action
                SteamVR_Input_Sources s = SteamVR_Input_Sources.Any;
                
                if (action is SteamVR_Action_Boolean a0 && typeof(T) == typeof(bool))
                    return (T)(object)a0[s].state;
                else if (action is SteamVR_Action_Single a1 && typeof(T) == typeof(float))
                    return (T)(object)a1[s].axis;
                else if (action is SteamVR_Action_Vector2 a2 && typeof(T) == typeof(Vector2))
                    return (T)(object)a2[s].axis;
                else if (action is SteamVR_Action_Pose a3 && typeof(T) == typeof(Vector3))
                    return (T)(object)a3[s].localPosition;
                else if (action is SteamVR_Action_Pose a4 && typeof(T) == typeof(Quaternion))
                    return (T)(object)a4[s].localRotation;
            } catch (Exception ex) {
                Logger.LogWarning($"OpenVRBindings.Get failed for action {path}: {ex.Message}", tag: nameof(OpenVRBindings));
            }

            return default;
        }

        public async UniTask Deinitialize() {
            _handles.Clear();
            await UniTask.CompletedTask;
        }

        public void Dispose()
            => Deinitialize().Forget();
    }
}