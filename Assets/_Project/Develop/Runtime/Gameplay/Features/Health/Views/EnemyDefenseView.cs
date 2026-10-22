using System;
using PrimeTween;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _Project.Gameplay.Features.Health.Views
{
    [DisallowMultipleComponent]
    public sealed class EnemyDefenseView : MonoBehaviour
    {
        private const float BreakDuration = 0.45f;

        private static Sprite _squareSprite;

        [SerializeField, Required] private SpriteRenderer bodyRenderer;
        [SerializeField, Required] private Transform helmetSocket;
        [SerializeField, Required] private Transform weaponSocket;

        private SpriteRenderer _helmetRenderer;
        private SpriteRenderer _weaponRenderer;
        private HelmetType _helmetType;
        private bool _isEnchanted;
        private int _maxHelmetDurability;

        public HelmetVisualState CurrentHelmetState { get; private set; }
        public bool HasHelmetVisual => _helmetRenderer != null;
        public bool HasWeaponVisual => _weaponRenderer != null;
        public SpriteRenderer BodyRenderer => bodyRenderer;
        public Transform HelmetSocket => helmetSocket;
        public Transform WeaponSocket => weaponSocket;

        public void Initialize(
            WeaponType weaponType,
            Sprite weaponSprite,
            HelmetType helmetType,
            Sprite helmetSprite,
            int helmetDurability,
            int maxHelmetDurability,
            bool isEnchanted)
        {
            if (!TryValidate(out var error))
            {
                throw new InvalidOperationException(error);
            }

            _helmetType = helmetType;
            _isEnchanted = isEnchanted;
            _maxHelmetDurability = maxHelmetDurability;

            if (helmetDurability > 0)
            {
                _helmetRenderer = CreatePiece(
                    "Helmet Layer",
                    helmetSocket,
                    helmetSprite,
                    GetHelmetColor(helmetType),
                    bodyRenderer.sortingOrder + 2);
                UpdateHelmetAppearance(helmetDurability);
            }

            if (weaponType != WeaponType.None)
            {
                _weaponRenderer = CreatePiece(
                    "Weapon Layer",
                    weaponSocket,
                    weaponSprite,
                    Color.white,
                    bodyRenderer.sortingOrder + 1);
                _weaponRenderer.flipX = false;
            }
        }

        public bool TryValidate(out string error)
        {
            if (bodyRenderer == null)
            {
                error = $"{nameof(EnemyDefenseView)} on '{name}' requires a body renderer.";
                return false;
            }

            if (helmetSocket == null)
            {
                error = $"{nameof(EnemyDefenseView)} on '{name}' requires a helmet socket.";
                return false;
            }

            if (weaponSocket == null)
            {
                error = $"{nameof(EnemyDefenseView)} on '{name}' requires a weapon socket.";
                return false;
            }

            if (!helmetSocket.IsChildOf(bodyRenderer.transform) ||
                !weaponSocket.IsChildOf(bodyRenderer.transform))
            {
                error = $"{nameof(EnemyDefenseView)} sockets on '{name}' must be children of the body visual.";
                return false;
            }

            error = null;
            return true;
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
                var bodyPiece = CreatePiece(
                    "Body Layer Broken",
                    transform,
                    GetSquareSprite(),
                    new Color(0.78f, 0.2f, 0.18f, 1f),
                    bodyRenderer.sortingOrder + 3);
                bodyPiece.transform.localScale = new Vector3(0.48f, 0.48f, 1f);
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
                    _helmetRenderer.transform.localScale = Vector3.one;
                    _helmetRenderer.transform.localRotation = Quaternion.identity;
                    break;

                case HelmetVisualState.Damaged:
                    _helmetRenderer.color = Color.Lerp(baseColor, Color.black, 0.35f);
                    _helmetRenderer.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
                    _helmetRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, 7f);
                    break;

                case HelmetVisualState.OneHitRemaining:
                    _helmetRenderer.color = Color.Lerp(baseColor, new Color(0.75f, 0.08f, 0.06f), 0.6f);
                    _helmetRenderer.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
                    _helmetRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, -11f);
                    break;
            }
        }

        private SpriteRenderer CreatePiece(
            string pieceName,
            Transform socket,
            Sprite sprite,
            Color color,
            int sortingOrder)
        {
            var piece = new GameObject(pieceName);
            piece.transform.SetParent(socket, false);
            piece.transform.localPosition = Vector3.zero;
            piece.transform.localRotation = Quaternion.identity;
            piece.transform.localScale = Vector3.one;

            var renderer = piece.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.flipX = bodyRenderer.flipX;
            renderer.flipY = bodyRenderer.flipY;
            renderer.sortingLayerID = bodyRenderer.sortingLayerID;
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
                HelmetType.Cloth => Color.white,
                HelmetType.Wood => Color.white,
                HelmetType.Iron => Color.white,
                HelmetType.DragonScale => new Color(0.35f, 0.82f, 0.42f, 1f),
                _ => Color.white
            };

            return _isEnchanted
                ? Color.Lerp(color, new Color(0.75f, 0.25f, 1f, 1f), 0.3f)
                : color;
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
