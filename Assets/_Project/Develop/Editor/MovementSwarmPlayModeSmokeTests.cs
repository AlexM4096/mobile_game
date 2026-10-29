using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Unity.Cinemachine;

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

            yield return new WaitForSecondsRealtime(1.2f);

            var playerView = GameObject.Find("Player View");
            var playerCameraTarget = GameObject.Find("Player Camera Target");
            var playerFollowCamera = GameObject.Find("Player Follow Camera");
            var swordView = GameObject.Find("Sword View 1");
            var enemyViewsRoot = GameObject.Find("Spawned Enemy Views");
            Assert.That(playerView, Is.Not.Null);
            Assert.That(playerCameraTarget, Is.Not.Null);
            Assert.That(playerFollowCamera, Is.Not.Null);
            Assert.That(swordView, Is.Not.Null);
            Assert.That(enemyViewsRoot, Is.Not.Null);
            Assert.That(enemyViewsRoot.transform.childCount, Is.GreaterThan(0));
            Assert.That(enemyViewsRoot.transform.childCount, Is.LessThan(400));

            var cinemachineCamera = playerFollowCamera.GetComponent<CinemachineCamera>();
            var positionComposer = playerFollowCamera.GetComponent<CinemachinePositionComposer>();
            Assert.That(cinemachineCamera.Follow, Is.EqualTo(playerCameraTarget.transform));
            Assert.That(positionComposer.Damping.x, Is.EqualTo(0.25f));
            Assert.That(positionComposer.Damping.y, Is.EqualTo(0.25f));

            var firstOffset = swordView.transform.position - playerView.transform.position;
            Assert.That(firstOffset.magnitude, Is.EqualTo(2f).Within(0.05f));

            yield return new WaitForSecondsRealtime(0.1f);

            var secondOffset = swordView.transform.position - playerView.transform.position;
            Assert.That(secondOffset.magnitude, Is.EqualTo(2f).Within(0.05f));

            yield return new ExitPlayMode();
        }
    }
}
