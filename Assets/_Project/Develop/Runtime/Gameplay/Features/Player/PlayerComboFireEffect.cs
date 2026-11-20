using System;
using PrimeTween;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _Project.Gameplay.Features.Player
{
    internal sealed class PlayerComboFireEffect : IDisposable
    {
        private const int TextureSize = 32;
        private const int AuraTextureSize = 96;
        private const float AuraDiameter = 3.8f;
        private ParticleSystem _flames;
        private Material _material;
        private Texture2D _texture;
        private SpriteRenderer _aura;
        private Texture2D _auraTexture;
        private Tween _auraTween;

        public void SetActive(bool active, Transform target, SpriteRenderer sprite)
        {
            if (_auraTween.isAlive) _auraTween.Stop();

            if (active)
            {
                if (_flames == null) CreateFlames(target, sprite);
                if (_flames != null) _flames.Play();
                if (_aura == null) CreateAura(target, sprite);

                _aura.gameObject.SetActive(true);
                ApplyAuraPulse(_aura, 0f);
                _auraTween = Tween.Custom(
                    _aura,
                    0f,
                    1f,
                    0.36f,
                    ApplyAuraPulse,
                    Ease.InOutSine,
                    cycles: -1,
                    cycleMode: CycleMode.Yoyo);
                return;
            }

            if (_flames != null)
            {
                _flames.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }

            if (_aura != null)
            {
                _auraTween = Tween.Alpha(_aura, 0f, 0.18f, Ease.OutSine)
                    .OnComplete(_aura, static renderer => renderer.gameObject.SetActive(false));
            }
        }

        private void CreateAura(Transform target, SpriteRenderer sprite)
        {
            var view = new GameObject("Player Combo Flame Aura");
            view.transform.SetParent(target, false);
            _aura = view.AddComponent<SpriteRenderer>();
            _auraTexture = CreateAuraTexture();
            _aura.sprite = Sprite.Create(
                _auraTexture,
                new Rect(0f, 0f, AuraTextureSize, AuraTextureSize),
                new Vector2(0.5f, 0.5f),
                AuraTextureSize / AuraDiameter,
                0,
                SpriteMeshType.FullRect);
            _aura.sortingOrder = -2;
            if (sprite != null)
            {
                _aura.sortingLayerID = sprite.sortingLayerID;
                _aura.sortingOrder = sprite.sortingOrder - 2;
            }
        }

        private static void ApplyAuraPulse(SpriteRenderer renderer, float progress)
        {
            renderer.transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.10f, progress);
            renderer.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.72f, 1f, progress));
        }

        private static Texture2D CreateAuraTexture()
        {
            var texture = new Texture2D(AuraTextureSize, AuraTextureSize, TextureFormat.RGBA32, false)
            {
                name = "Combo Flame Aura",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var orange = new Color(1f, 0.32f, 0.06f);
            var gold = new Color(1f, 0.79f, 0.30f);
            var pixels = new Color32[AuraTextureSize * AuraTextureSize];
            const float coordinateScale = 2f / (AuraTextureSize - 1);

            for (var y = 0; y < AuraTextureSize; y++)
            {
                for (var x = 0; x < AuraTextureSize; x++)
                {
                    var point = new Vector2(x * coordinateScale - 1f, y * coordinateScale - 1f);
                    var distance = point.magnitude;
                    var angle = Mathf.Atan2(point.y, point.x);
                    var radius = 0.72f + 0.018f * Mathf.Sin(angle * 7f) + 0.010f * Mathf.Sin(angle * 11f);
                    var edgeDistance = Mathf.Abs(distance - radius);
                    var core = Mathf.Clamp01(1f - edgeDistance / 0.06f);
                    var halo = Mathf.Clamp01(1f - edgeDistance / 0.23f);
                    var innerGlow = Mathf.Clamp01(1f - distance / radius) * 0.13f;
                    var color = Color.Lerp(orange, gold, core);
                    color.a = Mathf.Clamp01(core * 0.68f + halo * halo * 0.36f + innerGlow);
                    pixels[y * AuraTextureSize + x] = color;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void CreateFlames(Transform target, SpriteRenderer sprite)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) return;

            var view = new GameObject("Player Combo Fire");
            view.SetActive(false);
            view.transform.SetParent(target, false);
            view.transform.localPosition = new Vector3(0f, -0.3f, 0f);
            _flames = view.AddComponent<ParticleSystem>();
            var main = _flames.main;
            main.playOnAwake = false;
            main.loop = true;
            main.maxParticles = 36;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.24f, 0.38f);
            main.startSpeed = 0f;
            main.startSize3D = true;
            main.startSizeX = new ParticleSystem.MinMaxCurve(0.42f, 0.75f);
            main.startSizeY = new ParticleSystem.MinMaxCurve(0.85f, 1.45f);
            main.startSizeZ = 1f;

            var emission = _flames.emission;
            emission.rateOverTime = 40f;
            var shape = _flames.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.52f;
            var velocity = _flames.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.y = 2.8f;

            var gradient = new Gradient();
            gradient.SetKeys(new[]
            {
                new GradientColorKey(new Color(1f, 0.90f, 0.44f), 0f),
                new GradientColorKey(new Color(1f, 0.50f, 0.12f), 0.4f),
                new GradientColorKey(new Color(0.81f, 0.19f, 0.06f), 1f)
            }, new[]
            {
                new GradientAlphaKey(0.85f, 0f),
                new GradientAlphaKey(0.66f, 0.47f),
                new GradientAlphaKey(0f, 1f)
            });
            var color = _flames.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(gradient);
            var size = _flames.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));

            _texture = CreateFlameTexture();
            _material = new Material(shader) { mainTexture = _texture };
            var renderer = view.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = _material;
            if (sprite != null)
            {
                renderer.sortingLayerID = sprite.sortingLayerID;
                renderer.sortingOrder = sprite.sortingOrder + 1;
            }

            view.SetActive(true);
        }

        private static Texture2D CreateFlameTexture()
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                name = "Combo Flame",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[TextureSize * TextureSize];
            for (var y = 0; y < TextureSize; y++)
            {
                var height = y / (float)(TextureSize - 1);
                var width = Mathf.Max(0.001f, 0.94f * Mathf.Sin(Mathf.PI * Mathf.Pow(height, 0.65f)));
                var center = 0.30f * height * height - 0.10f;
                for (var x = 0; x < TextureSize; x++)
                {
                    var offset = Mathf.Abs(x * (2f / (TextureSize - 1)) - 1f - center);
                    var alpha = Mathf.Clamp01((1f - offset / width) * 2.4f);
                    alpha *= Mathf.Clamp01(Mathf.Min(height, 1f - height) * 10f);
                    pixels[y * TextureSize + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        public void Dispose()
        {
            if (_auraTween.isAlive) _auraTween.Stop();
            if (_aura != null) Object.Destroy(_aura.sprite);
            if (_auraTexture != null) Object.Destroy(_auraTexture);
            if (_material != null) Object.Destroy(_material);
            if (_texture != null) Object.Destroy(_texture);
        }
    }
}
