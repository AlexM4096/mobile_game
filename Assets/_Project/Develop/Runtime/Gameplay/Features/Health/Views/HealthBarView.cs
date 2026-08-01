using UnityEngine;
using UnityEngine.UIElements;

namespace _Project.Gameplay.Features.Health.Views
{
    public sealed class HealthBarView : VisualElement
    {
        private readonly VisualElement fill;

        public HealthBarView(float width, float height, Color backgroundColor, Color fillColor)
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.width = width;
            style.height = height;
            style.backgroundColor = backgroundColor;
            style.overflow = Overflow.Hidden;
            style.borderTopLeftRadius = 1f;
            style.borderTopRightRadius = 1f;
            style.borderBottomLeftRadius = 1f;
            style.borderBottomRightRadius = 1f;

            fill = new VisualElement
            {
                pickingMode = PickingMode.Ignore
            };
            fill.style.height = Length.Percent(100f);
            fill.style.backgroundColor = fillColor;
            Add(fill);
        }

        public void SetFill(float normalized)
        {
            fill.style.width = Length.Percent(Mathf.Clamp01(normalized) * 100f);
        }
    }
}
