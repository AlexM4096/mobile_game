using System;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace _Project.Gameplay.Features.Death
{
    internal sealed class EnemyDefeatParticles : IDisposable
    {
        private const int MaxParticles = 160;
        private const int ShardTextureSize = 32;
        private static readonly Color32 Ivory = new(255, 240, 207, 255);
        private static readonly Color32 Gold = new(255, 209, 129, 255);
        private static readonly Color32 Blue = new(146, 235, 255, 255);

        private ParticleSystem _particles;
        private Material _material;
        private Texture2D _shardTexture;

        public void Emit(Vector3 origin, SpriteRenderer sprite, Vector2 impactDirection, bool milestone)
        {
            if (_particles == null && !TryCreate(sprite)) return;

            var count = milestone ? 6 : 5;
            var perpendicular = new Vector2(-impactDirection.y, impactDirection.x);
            var spread = (milestone ? 45f : 34f) * Mathf.Deg2Rad;
            for (var index = 0; index < count; index++)
            {
                var angle = Mathf.Lerp(-spread, spread, index / (float)(count - 1));
                angle += Random.Range(-0.07f, 0.07f);
                var direction = impactDirection * Mathf.Cos(angle) + perpendicular * Mathf.Sin(angle);
                var particle = new ParticleSystem.EmitParams
                {
                    position = origin + (Vector3)(direction * 0.2f),
                    velocity = direction * Random.Range(6.0f, 10.5f),
                    startColor = milestone && index % 2 == 0
                        ? Blue
                        : index % 2 == 0 ? Gold : Ivory,
                    startSize = Random.Range(0.34f, 0.53f),
                    startLifetime = Random.Range(0.16f, 0.22f),
                    rotation = -Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + Random.Range(-18f, 18f)
                };
                _particles.Emit(particle, 1);
            }
        }

        private bool TryCreate(SpriteRenderer sprite)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) return false;

            var view = new GameObject("Enemy Impact Shards");
            view.SetActive(false);
            _particles = view.AddComponent<ParticleSystem>();
            var main = _particles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = MaxParticles;
            var emission = _particles.emission;
            emission.enabled = false;
            var shape = _particles.shape;
            shape.enabled = false;
            var size = _particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.42f), new GradientAlphaKey(0f, 1f) });
            var color = _particles.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(fade);

            var renderer = view.GetComponent<ParticleSystemRenderer>();
            _shardTexture = CreateShardTexture();
            _material = new Material(shader) { mainTexture = _shardTexture };
            renderer.sharedMaterial = _material;
            renderer.sortingLayerID = sprite.sortingLayerID;
            renderer.sortingOrder = sprite.sortingOrder + 4;
            view.SetActive(true);
            _particles.Play();
            return true;
        }

        private static Texture2D CreateShardTexture()
        {
            var texture = new Texture2D(ShardTextureSize, ShardTextureSize, TextureFormat.RGBA32, false)
            {
                name = "Impact Diamond",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[ShardTextureSize * ShardTextureSize];
            for (var y = 0; y < ShardTextureSize; y++)
            {
                for (var x = 0; x < ShardTextureSize; x++)
                {
                    var point = new Vector2(x, y) * (2f / (ShardTextureSize - 1)) - Vector2.one;
                    var diamond = Mathf.Abs(point.x) / 0.9f + Mathf.Abs(point.y) / 0.56f;
                    var opacity = Mathf.Clamp01((1f - diamond) * 9f);
                    var facet = point.y + 0.08f * point.x > 0f ? (byte)255 : (byte)181;
                    pixels[y * ShardTextureSize + x] = new Color32(facet, facet, facet,
                        (byte)Mathf.RoundToInt(opacity * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        public void Dispose()
        {
            if (_particles != null) Object.Destroy(_particles.gameObject);
            if (_material != null) Object.Destroy(_material);
            if (_shardTexture != null) Object.Destroy(_shardTexture);
        }
    }
}
