using PrimeTween;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _Project.Gameplay.Features.Health.Views
{
    [DisallowMultipleComponent]
    public sealed class EnemyDefenseView : MonoBehaviour
    {
        private const float BreakDuration = 0.45f;

        private static Sprite _squareSprite;

        private SpriteRenderer _helmetRenderer;
        private SpriteRenderer _weaponRenderer;
        private HelmetType _helmetType;
        private bool _isEnchanted;
        private int _maxHelmetDurability;
        private int _sortingLayerId;
        private int _sortingOrder;

        public HelmetVisualState CurrentHelmetState { get; private set; }
        public bool HasHelmetVisual => _helmetRenderer != null;
        public bool HasWeaponVisual => _weaponRenderer != null;

        public void Initialize(
            HelmetType helmetType,
            int helmetDurability,
            int maxHelmetDurability,
            bool hasWeapon,
            bool isEnchanted)
        {
            _helmetType = helmetType;
            _isEnchanted = isEnchanted;
            _maxHelmetDurability = maxHelmetDurability;
            ResolveSorting();

            if (helmetDurability > 0)
            {
                _helmetRenderer = CreateTile(
                    "Helmet Layer",
                    GetHelmetColor(helmetType),
                    new Vector3(0f, 0.52f, 0f),
                    new Vector3(0.72f, 0.28f, 1f),
                    _sortingOrder + 2);
                UpdateHelmetAppearance(helmetDurability);
            }

            if (hasWeapon)
            {
                _weaponRenderer = CreateTile(
                    "Weapon Layer",
                    new Color(0.3f, 0.32f, 0.36f, 1f),
                    new Vector3(0.52f, 0.02f, 0f),
                    new Vector3(0.14f, 0.72f, 1f),
                    _sortingOrder + 1);
                _weaponRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, -24f);
            }
        }

        public void ApplyState(
            int helmetDurability,
            BrokenDefenseLayers brokenLayers)
        {
            if ((brokenLayers & BrokenDefenseLayers.Helmet) != 0)
            {
                AnimateBreak(ref _helmetRenderer, new Vector3(-0.65f, 0.8f, 0f));
                CurrentHelmetState = HelmetVisualState.None;
            }
            else
            {
                UpdateHelmetAppearance(helmetDurability);
            }

            if ((brokenLayers & BrokenDefenseLayers.Weapon) != 0)
            {
                AnimateBreak(ref _weaponRenderer, new Vector3(0.8f, 0.45f, 0f));
            }

            if ((brokenLayers & BrokenDefenseLayers.Body) != 0)
            {
                var bodyPiece = CreateTile(
                    "Body Layer Broken",
                    new Color(0.78f, 0.2f, 0.18f, 1f),
                    Vector3.zero,
                    new Vector3(0.48f, 0.48f, 1f),
                    _sortingOrder + 3);
                AnimateBreak(ref bodyPiece, new Vector3(0.15f, 0.9f, 0f));
            }
        }

        private void UpdateHelmetAppearance(int durability)
        {
            CurrentHelmetState = EnemyDefenseRules.GetHelmetVisualState(
                durability,
                _maxHelmetDurability);

            if (_helmetRenderer == null || CurrentHelmetState == HelmetVisualState.None)
            {
                return;
            }

            var baseColor = GetHelmetColor(_helmetType);
            switch (CurrentHelmetState)
            {
                case HelmetVisualState.FullDurability:
                    _helmetRenderer.color = baseColor;
                    _helmetRenderer.transform.localScale = new Vector3(0.72f, 0.28f, 1f);
                    _helmetRenderer.transform.localRotation = Quaternion.identity;
                    break;

                case HelmetVisualState.Damaged:
                    _helmetRenderer.color = Color.Lerp(baseColor, Color.black, 0.35f);
                    _helmetRenderer.transform.localScale = new Vector3(0.64f, 0.24f, 1f);
                    _helmetRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, 7f);
                    break;

                case HelmetVisualState.OneHitRemaining:
                    _helmetRenderer.color = Color.Lerp(baseColor, new Color(0.75f, 0.08f, 0.06f), 0.6f);
                    _helmetRenderer.transform.localScale = new Vector3(0.54f, 0.2f, 1f);
                    _helmetRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, -11f);
                    break;
            }
        }

        private SpriteRenderer CreateTile(
            string tileName,
            Color color,
            Vector3 localPosition,
            Vector3 localScale,
            int sortingOrder)
        {
            var tile = new GameObject(tileName);
            tile.transform.SetParent(transform, false);
            tile.transform.localPosition = localPosition;
            tile.transform.localScale = localScale;

            var renderer = tile.AddComponent<SpriteRenderer>();
            renderer.sprite = GetSquareSprite();
            renderer.color = color;
            renderer.sortingLayerID = _sortingLayerId;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private void AnimateBreak(ref SpriteRenderer renderer, Vector3 flightOffset)
        {
            if (renderer == null)
            {
                return;
            }

            var brokenRenderer = renderer;
            renderer = null;

            var piece = brokenRenderer.gameObject;
            var pieceTransform = piece.transform;
            piece.name += " (Broken)";
            pieceTransform.SetParent(null, true);

            Tween.Position(
                pieceTransform,
                pieceTransform.position + flightOffset,
                BreakDuration,
                Ease.OutCubic);
            Tween.Rotation(
                pieceTransform,
                pieceTransform.rotation * Quaternion.Euler(0f, 0f, flightOffset.x >= 0f ? -170f : 170f),
                BreakDuration,
                Ease.Linear);
            Tween.Scale(
                pieceTransform,
                Vector3.zero,
                BreakDuration,
                Ease.InCubic);
            Tween.Alpha(
                    brokenRenderer,
                    0f,
                    BreakDuration,
                    Ease.InCubic)
                .OnComplete(piece, static target =>
                {
                    if (target != null)
                    {
                        Object.Destroy(target);
                    }
                });
        }

        private Color GetHelmetColor(HelmetType helmetType)
        {
            var color = helmetType switch
            {
                HelmetType.Cloth => new Color(0.82f, 0.72f, 0.56f, 1f),
                HelmetType.Wood => new Color(0.48f, 0.27f, 0.12f, 1f),
                HelmetType.Iron => new Color(0.58f, 0.63f, 0.7f, 1f),
                HelmetType.DragonScale => new Color(0.18f, 0.6f, 0.3f, 1f),
                _ => Color.white
            };

            return _isEnchanted
                ? Color.Lerp(color, new Color(0.75f, 0.25f, 1f, 1f), 0.3f)
                : color;
        }

        private void ResolveSorting()
        {
            var renderer = GetComponentInChildren<SpriteRenderer>();
            _sortingLayerId = renderer != null ? renderer.sortingLayerID : 0;
            _sortingOrder = renderer != null ? renderer.sortingOrder : 0;
        }

        private static Sprite GetSquareSprite()
        {
            if (_squareSprite != null)
            {
                return _squareSprite;
            }

            var texture = Texture2D.whiteTexture;
            _squareSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                texture.width);
            _squareSprite.name = "Runtime Defense Square";
            return _squareSprite;
        }
    }
}