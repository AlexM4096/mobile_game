using System.Collections;
using _Project.Gameplay.Features.Movement.Views;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace _Project.Editor.Tests
{
    public sealed class WalkingTweenViewPlayModeTests
    {
        [UnityTest]
        public IEnumerator EnabledViewAnimatesAndDisableRestoresInitialPose()
        {
            var root = new GameObject("Walking Tween Test Root");
            root.SetActive(false);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform);
            visual.transform.localScale = new Vector3(2f, 3f, 1f);
            visual.transform.localRotation = Quaternion.Euler(0f, 0f, 11f);
            var walkingConfig = ScriptableObject.CreateInstance<WalkingTweenConfig>();
            var walkingTween = visual.AddComponent<WalkingTweenView>();
            var serializedTween = new SerializedObject(walkingTween);
            serializedTween.FindProperty("config").objectReferenceValue = walkingConfig;
            serializedTween.ApplyModifiedPropertiesWithoutUndo();

            var initialScale = visual.transform.localScale;
            var initialRotation = visual.transform.localRotation;

            root.SetActive(true);
            yield return null;

            Assert.That(visual.transform.localScale, Is.Not.EqualTo(initialScale));
            Assert.That(visual.transform.localRotation, Is.Not.EqualTo(initialRotation));

            walkingTween.enabled = false;

            Assert.That(visual.transform.localScale, Is.EqualTo(initialScale));
            Assert.That(visual.transform.localRotation, Is.EqualTo(initialRotation));

            Object.Destroy(root);
            Object.Destroy(walkingConfig);
            yield return null;
        }
    }
}
