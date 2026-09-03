using NUnit.Framework;
using _Project.Gameplay.Features.Collision.Systems;
using UnityEngine;

namespace _Project.Editor.Tests
{
    public sealed class CollisionMathTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void CircleCircle_ReturnsMidpointNormalAndPenetration()
        {
            var first = CollisionShape.Circle(Vector2.zero, 1f);
            var second = CollisionShape.Circle(new Vector2(1.5f, 0f), 1f);

            Assert.That(CollisionMath.TryGetContact(first, second, out var contact), Is.True);
            AssertVector(contact.Point, new Vector2(0.75f, 0f));
            AssertVector(contact.Normal, Vector2.right);
            Assert.That(contact.Penetration, Is.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void CircleCircle_TouchingCountsAsContact()
        {
            var first = CollisionShape.Circle(Vector2.zero, 1f);
            var second = CollisionShape.Circle(new Vector2(2f, 0f), 1f);

            Assert.That(CollisionMath.TryGetContact(first, second, out var contact), Is.True);
            AssertVector(contact.Point, Vector2.right);
            AssertVector(contact.Normal, Vector2.right);
            Assert.That(contact.Penetration, Is.Zero.Within(Tolerance));
        }

        [Test]
        public void CircleCircle_CoincidentCentersUseDeterministicNormal()
        {
            var first = CollisionShape.Circle(Vector2.zero, 1f);
            var second = CollisionShape.Circle(Vector2.zero, 2f);

            Assert.That(CollisionMath.TryGetContact(first, second, out var contact), Is.True);
            AssertVector(contact.Normal, Vector2.right);
            Assert.That(contact.Penetration, Is.EqualTo(3f).Within(Tolerance));
        }

        [Test]
        public void BoxBox_UsesMinimumPenetrationAxis()
        {
            var first = CollisionShape.Box(Vector2.zero, Vector2.one);
            var second = CollisionShape.Box(new Vector2(1.5f, 0.25f), Vector2.one);

            Assert.That(CollisionMath.TryGetContact(first, second, out var contact), Is.True);
            AssertVector(contact.Point, new Vector2(0.75f, 0.125f));
            AssertVector(contact.Normal, Vector2.right);
            Assert.That(contact.Penetration, Is.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void CircleBox_OutsideContactUsesOpposingSurfaceMidpoint()
        {
            var circle = CollisionShape.Circle(new Vector2(-1.5f, 0f), 1f);
            var box = CollisionShape.Box(Vector2.zero, Vector2.one);

            Assert.That(CollisionMath.TryGetContact(circle, box, out var contact), Is.True);
            AssertVector(contact.Point, new Vector2(-0.75f, 0f));
            AssertVector(contact.Normal, Vector2.right);
            Assert.That(contact.Penetration, Is.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void CircleBox_ContainedCircleUsesNearestFace()
        {
            var circle = CollisionShape.Circle(Vector2.zero, 0.5f);
            var box = CollisionShape.Box(Vector2.zero, new Vector2(2f, 1f));

            Assert.That(CollisionMath.TryGetContact(circle, box, out var contact), Is.True);
            AssertVector(contact.Point, new Vector2(0f, -0.75f));
            AssertVector(contact.Normal, Vector2.up);
            Assert.That(contact.Penetration, Is.EqualTo(1.5f).Within(Tolerance));
        }

        [Test]
        public void BoxCircle_ReversesCircleBoxNormal()
        {
            var box = CollisionShape.Box(Vector2.zero, Vector2.one);
            var circle = CollisionShape.Circle(new Vector2(-1.5f, 0f), 1f);

            Assert.That(CollisionMath.TryGetContact(box, circle, out var contact), Is.True);
            AssertVector(contact.Point, new Vector2(-0.75f, 0f));
            AssertVector(contact.Normal, Vector2.left);
            Assert.That(contact.Penetration, Is.EqualTo(0.5f).Within(Tolerance));
        }

        private static void AssertVector(Vector2 actual, Vector2 expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(Tolerance));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(Tolerance));
        }
    }
}
