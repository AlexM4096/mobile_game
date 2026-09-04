using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace _Project.Editor.Tests
{
    public sealed class MovementSwarmPlayModeSmokeTests
    {
        [UnityTest]
        public IEnumerator MovementSwarmSceneEntersAndExitsPlayMode()
        {
            EditorSceneManager.OpenScene(
                "Assets/_Project/Scenes/MovementSwarmTestScene.unity",
                OpenSceneMode.Single);

            yield return new EnterPlayMode();
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MovementSwarmTestScene"));

            yield return null;
            yield return null;

            var playerView = GameObject.Find("Player View");
            var swordView = GameObject.Find("Sword View 1");
            Assert.That(playerView, Is.Not.Null);
            Assert.That(swordView, Is.Not.Null);

            var firstOffset = swordView.transform.position - playerView.transform.position;
            Assert.That(firstOffset.magnitude, Is.EqualTo(2f).Within(0.05f));

            yield return null;
            yield return null;

            var secondOffset = swordView.transform.position - playerView.transform.position;
            Assert.That(secondOffset.magnitude, Is.EqualTo(2f).Within(0.05f));
            Assert.That(Vector3.Distance(firstOffset, secondOffset), Is.GreaterThan(0.001f));

            yield return new ExitPlayMode();
        }
    }
}
