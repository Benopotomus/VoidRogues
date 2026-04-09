using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoidRogues
{
    public partial class SceneCamera : SceneService
    {
        // ────────────────────────────────────────────────────────────────
        // Camera Shake Fields
        // ────────────────────────────────────────────────────────────────
        [Header("Camera Shake Profiles")]
        [SerializeField] private NoiseSettings _fireShakeSettings;
        [SerializeField] private NoiseSettings _takeDamageShakeSettings;
        [SerializeField] private NoiseSettings _aoeShakeSettings;

        [Header("Default Shake Values (fallback when params are -1)")]
        [SerializeField] private float _defaultShakeDuration = 0.35f;
        [SerializeField] private float _defaultShakeAmplitude = 1.8f;
        [SerializeField] private float _defaultShakeFrequency = 12f;

        // Tracks all currently running shakes
        private List<ActiveShake> _activeShakes = new List<ActiveShake>();

        private class ActiveShake
        {
            public float currentAmplitude;
            public Coroutine routine;
            public NoiseSettings profile;
            public float frequency;
            public ECameraShakeType? type;  // null = custom shake (always stacks)
        }

        [Serializable]
        public struct CameraShakeParams
        {
            public NoiseSettings profile;
            public float duration;
            public float peakAmplitude;
            public float peakFrequency;
            public float fadeInTime;
            public float fadeOutTime;
            public float sustainTime;

            public CameraShakeParams(
                NoiseSettings profile,
                float duration = 0.5f,
                float peakAmplitude = 2f,
                float peakFrequency = 10f,
                float fadeIn = 0.12f,
                float fadeOut = 0.25f,
                float sustain = 0f)
            {
                this.profile = profile;
                this.duration = duration;
                this.peakAmplitude = peakAmplitude;
                this.peakFrequency = peakFrequency;
                this.fadeInTime = fadeIn;
                this.fadeOutTime = fadeOut;
                this.sustainTime = sustain;
            }

            public static CameraShakeParams Fire(NoiseSettings profile)
                => new(profile, 0.25f, 1f, 1f, 0.05f, 0.1f, 0f);

            public static CameraShakeParams Damage(NoiseSettings profile)
                => new(profile, 0.3f, 3.5f, 14f, 0.08f, 0.1f, 0f);

            public static CameraShakeParams AOE(NoiseSettings profile)
                => new(profile, 1.1f, 4.2f, 9f, 0.15f, 0.50f, 0.45f);
        }

        // ────────────────────────────────────────────────────────────────
        // Public shake methods
        // ────────────────────────────────────────────────────────────────
        public void Shake(
            ECameraShakeType type,
            float overrideAmplitude = -1f,
            float overrideDuration = -1f,
            float overrideFrequency = -1f,
            float overrideFadeIn = -1f,
            float overrideFadeOut = -1f,
            float overrideSustain = -1f)
        {
            var (profile, baseParams) = GetBaseShake(type);
            if (profile == null)
            {
                Debug.LogWarning($"[Camera Shake] Profile for {type} is not assigned!");
                return;
            }

            var p = new CameraShakeParams(
                profile,
                overrideDuration >= 0 ? overrideDuration : baseParams.duration,
                overrideAmplitude >= 0 ? overrideAmplitude : baseParams.peakAmplitude,
                overrideFrequency >= 0 ? overrideFrequency : baseParams.peakFrequency,
                overrideFadeIn >= 0 ? overrideFadeIn : baseParams.fadeInTime,
                overrideFadeOut >= 0 ? overrideFadeOut : baseParams.fadeOutTime,
                overrideSustain >= 0 ? overrideSustain : baseParams.sustainTime
            );

            StartNewShake(p, type);
        }

        public void ShakeCamera(CameraShakeParams shakeParams)
        {
            if (shakeParams.profile == null)
            {
                Debug.LogWarning("[Camera Shake] No NoiseSettings profile provided!");
                return;
            }
            StartNewShake(shakeParams, null); // custom → no type, can stack
        }

        public void ShakeCamera(
            NoiseSettings profile,
            float duration = -1f,
            float amplitude = -1f,
            float frequency = -1f,
            float fadeIn = 0.12f,
            float fadeOut = 0.25f,
            float sustain = 0f)
        {
            var p = new CameraShakeParams(
                profile,
                duration < 0 ? _defaultShakeDuration : duration,
                amplitude < 0 ? _defaultShakeAmplitude : amplitude,
                frequency < 0 ? _defaultShakeFrequency : frequency,
                fadeIn,
                fadeOut,
                sustain
            );
            ShakeCamera(p);
        }

        // ────────────────────────────────────────────────────────────────
        // Stacking + Per-Type Replacement
        // ────────────────────────────────────────────────────────────────
        private void StartNewShake(CameraShakeParams p, ECameraShakeType? shakeType = null)
        {
            // Replace any existing shake of the same type
            if (shakeType.HasValue)
            {
                for (int i = _activeShakes.Count - 1; i >= 0; i--)
                {
                    if (_activeShakes[i].type == shakeType.Value)
                    {
                        if (_activeShakes[i].routine != null)
                            StopCoroutine(_activeShakes[i].routine);
                        _activeShakes.RemoveAt(i);
                    }
                }
            }

            var shake = new ActiveShake
            {
                currentAmplitude = 0f,
                profile = p.profile,
                frequency = p.peakFrequency,
                type = shakeType
            };

            shake.routine = StartCoroutine(ShakeWithFadeCoroutine(p, shake));
            _activeShakes.Add(shake);
        }

        private IEnumerator ShakeWithFadeCoroutine(CameraShakeParams p, ActiveShake thisShake)
        {
            float elapsed = 0f;

            float requestedTotal = p.duration;
            float idealTotal = p.fadeInTime + p.sustainTime + p.fadeOutTime;
            float scale = (idealTotal > 0f && requestedTotal > 0f) ? requestedTotal / idealTotal : 1f;

            float fadeInEnd = p.fadeInTime * scale;
            float sustainEnd = fadeInEnd + (p.sustainTime * scale);
            float totalEnd = requestedTotal;

            // Phase 1: Fade IN
            while (elapsed < fadeInEnd)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeInEnd);
                thisShake.currentAmplitude = Mathf.Lerp(0f, p.peakAmplitude, t);
                UpdateCombinedAmplitude();
                yield return null;
            }

            // Phase 2: Sustain
            while (elapsed < sustainEnd && elapsed < totalEnd)
            {
                elapsed += Time.deltaTime;
                thisShake.currentAmplitude = p.peakAmplitude;
                UpdateCombinedAmplitude();
                yield return null;
            }

            // Phase 3: Fade OUT
            float fadeOutStart = elapsed;
            float fadeOutDuration = Mathf.Max(0.01f, totalEnd - fadeOutStart);

            while (elapsed < totalEnd)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01((elapsed - fadeOutStart) / fadeOutDuration);
                thisShake.currentAmplitude = Mathf.Lerp(p.peakAmplitude, 0f, t);
                UpdateCombinedAmplitude();
                yield return null;
            }

            // Cleanup
            thisShake.currentAmplitude = 0f;
            UpdateCombinedAmplitude();
            _activeShakes.Remove(thisShake);
        }

        private void UpdateCombinedAmplitude()
        {
            float totalAmplitude = 0f;
            NoiseSettings dominantProfile = null;
            float dominantFreq = _defaultShakeFrequency;
            float highestAmp = -1f;

            foreach (var shake in _activeShakes)
            {
                totalAmplitude += shake.currentAmplitude;

                if (shake.currentAmplitude > highestAmp)
                {
                    highestAmp = shake.currentAmplitude;
                    dominantProfile = shake.profile;
                    dominantFreq = shake.frequency;
                }
            }

            SetNoiseParams(topDownCam, dominantProfile, dominantFreq, totalAmplitude);
        }

        private void SetNoiseParams(CinemachineVirtualCamera vcam, NoiseSettings profile, float freqGain, float ampGain)
        {
            if (vcam == null) return;

            var noise = vcam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            if (noise == null) return;

            if (profile != null)
                noise.m_NoiseProfile = profile;

            noise.m_FrequencyGain = freqGain;
            noise.m_AmplitudeGain = ampGain;
        }

        // ────────────────────────────────────────────────────────────────
        // Utility
        // ────────────────────────────────────────────────────────────────
        public void StopAllShakes()
        {
            foreach (var shake in _activeShakes)
            {
                if (shake.routine != null)
                    StopCoroutine(shake.routine);
            }
            _activeShakes.Clear();
            SetNoiseParams(topDownCam, null, 0f, 0f);
        }

        public void ResetAll()
        {
            StopAllShakes();

            if (topDownCam != null)
            {
                var noise = topDownCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                if (noise != null)
                {
                    noise.m_AmplitudeGain = 0f;
                    noise.m_FrequencyGain = 0f;
                }
            }

            Debug.Log("Full camera shake reset complete");
        }

        private (NoiseSettings profile, CameraShakeParams baseParams) GetBaseShake(ECameraShakeType type)
        {
            return type switch
            {
                ECameraShakeType.Fire => (_fireShakeSettings, CameraShakeParams.Fire(_fireShakeSettings)),
                ECameraShakeType.Damage => (_takeDamageShakeSettings, CameraShakeParams.Damage(_takeDamageShakeSettings)),
                ECameraShakeType.AOE => (_aoeShakeSettings, CameraShakeParams.AOE(_aoeShakeSettings)),
                _ => throw new ArgumentException($"Unsupported ShakeType: {type}")
            };
        }
    }

    public enum ECameraShakeType
    {
        Fire,
        Damage,
        AOE,
    }
}
