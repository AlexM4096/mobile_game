using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Common;
using Unity.Mathematics;
using UnityEngine;
using RotationComponent = _Project.Gameplay.Features.Common.Rotation;

namespace _Project.Gameplay.Features.Collision.Systems
{
    public sealed class DetectCollisionsSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<Position, CollisionBody>();

        private readonly CollisionMatrix _collisionMatrix;
        private readonly List<BodySnapshot> _bodies = new();
        private Dictionary<ContactPair, ContactState> _previousContacts = new();
        private Dictionary<ContactPair, ContactState> _currentContacts = new();

        public DetectCollisionsSystem(World world, CollisionMatrix collisionMatrix) : base(world)
        {
            _collisionMatrix = collisionMatrix;
        }

        public override void Update(in SystemState state)
        {
            CollectBodies();
            DetectContacts();
            EmitExits();
            SwapContactHistory();
        }

        private void CollectBodies()
        {
            _bodies.Clear();

            World.Query(in _description, (
                Entity entity,
                ref Position position,
                ref CollisionBody body
            ) =>
            {
                if (body.Layer == CollisionLayer.None ||
                    !CollisionShapeFactory.TryCreate(
                        World,
                        entity,
                        position.Value,
                        out var shape))
                {
                    return;
                }

                _bodies.Add(new BodySnapshot(
                    entity,
                    body,
                    shape,
                    World.Has<TriggerTag>(entity)));
            });
        }

        private void DetectContacts()
        {
            _currentContacts.Clear();

            for (var firstIndex = 0; firstIndex < _bodies.Count; firstIndex++)
            {
                for (var secondIndex = firstIndex + 1; secondIndex < _bodies.Count; secondIndex++)
                {
                    var first = _bodies[firstIndex];
                    var second = _bodies[secondIndex];

                    if (!_collisionMatrix.CanInteract(first.Body.Layer, second.Body.Layer) ||
                        !CollisionMath.TryGetContact(first.Shape, second.Shape, out var contact))
                    {
                        continue;
                    }

                    var pair = new ContactPair(first.Entity, second.Entity);
                    var state = CreateStableContactState(first.Entity, second.Entity, contact);
                    var phase = _previousContacts.ContainsKey(pair)
                        ? CollisionPhase.Stay
                        : CollisionPhase.Enter;

                    _currentContacts.Add(pair, state);
                    EmitEvent(state, phase);

                    if (!first.IsTrigger && !second.IsTrigger)
                    {
                        SeparateSolidBodies(firstIndex, secondIndex, contact.Normal, contact.Penetration);
                    }
                }
            }
        }

        private void EmitExits()
        {
            foreach (var previousContact in _previousContacts)
            {
                if (_currentContacts.ContainsKey(previousContact.Key))
                {
                    continue;
                }

                var contact = previousContact.Value;
                if (World.IsAlive(contact.First) && World.IsAlive(contact.Second))
                {
                    EmitEvent(contact, CollisionPhase.Exit);
                }
            }
        }

        private void EmitEvent(ContactState contact, CollisionPhase phase)
        {
            World.Create(new CollisionEvent
            {
                First = contact.First,
                Second = contact.Second,
                Phase = phase,
                Point = contact.Point,
                Normal = contact.NormalFromFirstToSecond,
                Penetration = phase == CollisionPhase.Exit ? 0f : contact.Penetration
            });
        }

        private void SeparateSolidBodies(
            int firstIndex,
            int secondIndex,
            Vector2 normal,
            float penetration)
        {
            if (penetration <= 0f)
            {
                return;
            }

            var first = _bodies[firstIndex];
            var second = _bodies[secondIndex];
            var firstDynamic = first.Body.BodyType == ColliderBodyType.Dynamic;
            var secondDynamic = second.Body.BodyType == ColliderBodyType.Dynamic;

            if (!firstDynamic && !secondDynamic)
            {
                return;
            }

            if (firstDynamic && secondDynamic)
            {
                MoveBody(firstIndex, -normal * (penetration * 0.5f));
                MoveBody(secondIndex, normal * (penetration * 0.5f));
                return;
            }

            if (firstDynamic)
            {
                MoveBody(firstIndex, -normal * penetration);
                return;
            }

            MoveBody(secondIndex, normal * penetration);
        }

        private void MoveBody(int bodyIndex, Vector2 offset)
        {
            var body = _bodies[bodyIndex];
            body.Shape = body.Shape.WithPosition(body.Shape.Position + offset);
            _bodies[bodyIndex] = body;

            ref var position = ref World.Get<Position>(body.Entity);
            position.Value = body.Shape.Position;
        }

        private void SwapContactHistory()
        {
            var oldContacts = _previousContacts;
            _previousContacts = _currentContacts;
            _currentContacts = oldContacts;
        }

        private static ContactState CreateStableContactState(
            Entity first,
            Entity second,
            CollisionContact contact)
        {
            return first.CompareTo(second) <= 0
                ? new ContactState(
                    first,
                    second,
                    contact.Point,
                    contact.Normal,
                    contact.Penetration)
                : new ContactState(
                    second,
                    first,
                    contact.Point,
                    -contact.Normal,
                    contact.Penetration);
        }

        private readonly struct ContactPair : IEquatable<ContactPair>
        {
            public ContactPair(Entity first, Entity second)
            {
                if (first.CompareTo(second) <= 0)
                {
                    First = first;
                    Second = second;
                }
                else
                {
                    First = second;
                    Second = first;
                }
            }

            private Entity First { get; }
            private Entity Second { get; }

            public bool Equals(ContactPair other)
            {
                return First.Equals(other.First) && Second.Equals(other.Second);
            }

            public override bool Equals(object obj)
            {
                return obj is ContactPair other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (First.GetHashCode() * 397) ^ Second.GetHashCode();
                }
            }
        }

        private readonly struct ContactState
        {
            public ContactState(
                Entity first,
                Entity second,
                Vector2 point,
                Vector2 normalFromFirstToSecond,
                float penetration)
            {
                First = first;
                Second = second;
                Point = point;
                NormalFromFirstToSecond = normalFromFirstToSecond;
                Penetration = penetration;
            }

            public Entity First { get; }
            public Entity Second { get; }
            public Vector2 Point { get; }
            public Vector2 NormalFromFirstToSecond { get; }
            public float Penetration { get; }
        }

        private struct BodySnapshot
        {
            public BodySnapshot(
                Entity entity,
                CollisionBody body,
                CollisionShape shape,
                bool isTrigger)
            {
                Entity = entity;
                Body = body;
                Shape = shape;
                IsTrigger = isTrigger;
            }

            public Entity Entity;
            public CollisionBody Body;
            public CollisionShape Shape;
            public bool IsTrigger;
        }
    }

    internal enum CollisionShapeType
    {
        Circle,
        Box
    }

    internal readonly struct CollisionShape
    {
        private CollisionShape(
            CollisionShapeType type,
            Vector2 position,
            float radius,
            Vector2 halfExtents,
            Vector2 axisX,
            Vector2 axisY,
            bool isAxisAligned)
        {
            Type = type;
            Position = position;
            Radius = radius;
            HalfExtents = halfExtents;
            AxisX = axisX;
            AxisY = axisY;
            IsAxisAligned = isAxisAligned;
        }

        public CollisionShapeType Type { get; }
        public Vector2 Position { get; }
        public float Radius { get; }
        public Vector2 HalfExtents { get; }
        public Vector2 AxisX { get; }
        public Vector2 AxisY { get; }
        public bool IsAxisAligned { get; }

        public static CollisionShape Circle(Vector2 position, float radius)
        {
            return new CollisionShape(
                CollisionShapeType.Circle,
                position,
                radius,
                default,
                Vector2.right,
                Vector2.up,
                true);
        }

        public static CollisionShape Box(Vector2 position, Vector2 halfExtents)
        {
            return new CollisionShape(
                CollisionShapeType.Box,
                position,
                default,
                halfExtents,
                Vector2.right,
                Vector2.up,
                true);
        }

        public static CollisionShape Box(
            Vector2 position,
            Vector2 halfExtents,
            float rotationRadians)
        {
            if (Mathf.Abs(Mathf.DeltaAngle(0f, rotationRadians * Mathf.Rad2Deg)) <= 0.0001f)
            {
                return Box(position, halfExtents);
            }

            var sine = Mathf.Sin(rotationRadians);
            var cosine = Mathf.Cos(rotationRadians);
            return new CollisionShape(
                CollisionShapeType.Box,
                position,
                default,
                halfExtents,
                new Vector2(cosine, sine),
                new Vector2(-sine, cosine),
                false);
        }

        public CollisionShape WithPosition(Vector2 position)
        {
            return new CollisionShape(
                Type,
                position,
                Radius,
                HalfExtents,
                AxisX,
                AxisY,
                IsAxisAligned);
        }
    }

    internal static class CollisionShapeFactory
    {
        public static bool TryCreate(
            World world,
            Entity entity,
            Vector2 position,
            out CollisionShape shape)
        {
            var hasCircle = world.Has<CircleCollider>(entity);
            var hasBox = world.Has<BoxCollider>(entity);
            if (hasCircle == hasBox)
            {
                shape = default;
                return false;
            }

            if (hasCircle)
            {
                ref var circle = ref world.Get<CircleCollider>(entity);
                if (!IsPositiveFinite(circle.Radius))
                {
                    shape = default;
                    return false;
                }

                shape = CollisionShape.Circle(position, circle.Radius);
                return true;
            }

            ref var box = ref world.Get<BoxCollider>(entity);
            if (!IsPositiveFinite(box.HalfExtents.x) ||
                !IsPositiveFinite(box.HalfExtents.y))
            {
                shape = default;
                return false;
            }

            var angle = 0f;
            if (world.Has<RotationComponent>(entity))
            {
                ref var rotation = ref world.Get<RotationComponent>(entity);
                var right = math.mul(rotation.Value, new float3(1f, 0f, 0f));
                angle = math.atan2(right.y, right.x);
            }

            shape = CollisionShape.Box(position, box.HalfExtents, angle);
            return true;
        }

        private static bool IsPositiveFinite(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    internal readonly struct CollisionContact
    {
        public CollisionContact(Vector2 point, Vector2 normal, float penetration)
        {
            Point = point;
            Normal = normal;
            Penetration = penetration;
        }

        public Vector2 Point { get; }
        public Vector2 Normal { get; }
        public float Penetration { get; }

        public CollisionContact Flipped()
        {
            return new CollisionContact(Point, -Normal, Penetration);
        }
    }

    internal static class CollisionMath
    {
        private const float DistanceEpsilonSquared = 0.000000000001f;

        public static bool TryGetContact(
            CollisionShape first,
            CollisionShape second,
            out CollisionContact contact)
        {
            if (first.Type == CollisionShapeType.Circle)
            {
                if (second.Type == CollisionShapeType.Circle)
                {
                    return TryCircleCircle(first, second, out contact);
                }

                return TryCircleBox(first, second, out contact);
            }

            if (second.Type == CollisionShapeType.Circle)
            {
                if (!TryCircleBox(second, first, out contact))
                {
                    return false;
                }

                contact = contact.Flipped();
                return true;
            }

            return TryBoxBox(first, second, out contact);
        }

        private static bool TryCircleCircle(
            CollisionShape first,
            CollisionShape second,
            out CollisionContact contact)
        {
            var difference = second.Position - first.Position;
            var radiusSum = first.Radius + second.Radius;
            var distanceSquared = difference.sqrMagnitude;
            if (distanceSquared > radiusSum * radiusSum)
            {
                contact = default;
                return false;
            }

            var distance = distanceSquared > DistanceEpsilonSquared
                ? Mathf.Sqrt(distanceSquared)
                : 0f;
            var normal = distance > 0f ? difference / distance : Vector2.right;
            var firstSurface = first.Position + normal * first.Radius;
            var secondSurface = second.Position - normal * second.Radius;

            contact = new CollisionContact(
                (firstSurface + secondSurface) * 0.5f,
                normal,
                radiusSum - distance);
            return true;
        }

        private static bool TryBoxBox(
            CollisionShape first,
            CollisionShape second,
            out CollisionContact contact)
        {
            if (first.IsAxisAligned && second.IsAxisAligned)
            {
                return TryAxisAlignedBoxBox(first, second, out contact);
            }

            var centerDifference = second.Position - first.Position;
            var minimumOverlap = float.MaxValue;
            var minimumAxis = Vector2.right;

            if (!TryUpdateMinimumOverlap(
                    first,
                    second,
                    centerDifference,
                    first.AxisX,
                    ref minimumOverlap,
                    ref minimumAxis) ||
                !TryUpdateMinimumOverlap(
                    first,
                    second,
                    centerDifference,
                    first.AxisY,
                    ref minimumOverlap,
                    ref minimumAxis) ||
                !TryUpdateMinimumOverlap(
                    first,
                    second,
                    centerDifference,
                    second.AxisX,
                    ref minimumOverlap,
                    ref minimumAxis) ||
                !TryUpdateMinimumOverlap(
                    first,
                    second,
                    centerDifference,
                    second.AxisY,
                    ref minimumOverlap,
                    ref minimumAxis))
            {
                contact = default;
                return false;
            }

            var normal = Vector2.Dot(centerDifference, minimumAxis) >= 0f
                ? minimumAxis
                : -minimumAxis;
            var firstSurface = GetSupportPoint(first, normal);
            var secondSurface = GetSupportPoint(second, -normal);
            contact = new CollisionContact(
                (firstSurface + secondSurface) * 0.5f,
                normal,
                minimumOverlap);
            return true;
        }

        private static bool TryUpdateMinimumOverlap(
            CollisionShape first,
            CollisionShape second,
            Vector2 centerDifference,
            Vector2 axis,
            ref float minimumOverlap,
            ref Vector2 minimumAxis)
        {
            var firstRadius = GetProjectionRadius(first, axis);
            var secondRadius = GetProjectionRadius(second, axis);
            var centerDistance = Mathf.Abs(Vector2.Dot(centerDifference, axis));
            var overlap = firstRadius + secondRadius - centerDistance;
            if (overlap < 0f)
            {
                return false;
            }

            if (overlap < minimumOverlap)
            {
                minimumOverlap = overlap;
                minimumAxis = axis;
            }

            return true;
        }

        private static float GetProjectionRadius(CollisionShape box, Vector2 axis)
        {
            return box.HalfExtents.x * Mathf.Abs(Vector2.Dot(axis, box.AxisX)) +
                   box.HalfExtents.y * Mathf.Abs(Vector2.Dot(axis, box.AxisY));
        }

        private static Vector2 GetSupportPoint(CollisionShape box, Vector2 direction)
        {
            var xSign = Vector2.Dot(direction, box.AxisX) >= 0f ? 1f : -1f;
            var ySign = Vector2.Dot(direction, box.AxisY) >= 0f ? 1f : -1f;
            return box.Position +
                   box.AxisX * (box.HalfExtents.x * xSign) +
                   box.AxisY * (box.HalfExtents.y * ySign);
        }

        private static bool TryAxisAlignedBoxBox(
            CollisionShape first,
            CollisionShape second,
            out CollisionContact contact)
        {
            var difference = second.Position - first.Position;
            var overlapX = first.HalfExtents.x + second.HalfExtents.x - Mathf.Abs(difference.x);
            var overlapY = first.HalfExtents.y + second.HalfExtents.y - Mathf.Abs(difference.y);
            if (overlapX < 0f || overlapY < 0f)
            {
                contact = default;
                return false;
            }

            if (overlapX <= overlapY)
            {
                var normal = difference.x >= 0f ? Vector2.right : Vector2.left;
                var firstSurface = first.Position.x + normal.x * first.HalfExtents.x;
                var secondSurface = second.Position.x - normal.x * second.HalfExtents.x;
                var overlapMinimum = Mathf.Max(
                    first.Position.y - first.HalfExtents.y,
                    second.Position.y - second.HalfExtents.y);
                var overlapMaximum = Mathf.Min(
                    first.Position.y + first.HalfExtents.y,
                    second.Position.y + second.HalfExtents.y);

                contact = new CollisionContact(
                    new Vector2(
                        (firstSurface + secondSurface) * 0.5f,
                        (overlapMinimum + overlapMaximum) * 0.5f),
                    normal,
                    overlapX);
                return true;
            }

            {
                var normal = difference.y >= 0f ? Vector2.up : Vector2.down;
                var firstSurface = first.Position.y + normal.y * first.HalfExtents.y;
                var secondSurface = second.Position.y - normal.y * second.HalfExtents.y;
                var overlapMinimum = Mathf.Max(
                    first.Position.x - first.HalfExtents.x,
                    second.Position.x - second.HalfExtents.x);
                var overlapMaximum = Mathf.Min(
                    first.Position.x + first.HalfExtents.x,
                    second.Position.x + second.HalfExtents.x);

                contact = new CollisionContact(
                    new Vector2(
                        (overlapMinimum + overlapMaximum) * 0.5f,
                        (firstSurface + secondSurface) * 0.5f),
                    normal,
                    overlapY);
                return true;
            }
        }

        private static bool TryCircleBox(
            CollisionShape circle,
            CollisionShape box,
            out CollisionContact contact)
        {
            var relativePosition = circle.Position - box.Position;
            var localCirclePosition = new Vector2(
                Vector2.Dot(relativePosition, box.AxisX),
                Vector2.Dot(relativePosition, box.AxisY));
            var minimum = -box.HalfExtents;
            var maximum = box.HalfExtents;
            var closestLocal = new Vector2(
                Mathf.Clamp(localCirclePosition.x, minimum.x, maximum.x),
                Mathf.Clamp(localCirclePosition.y, minimum.y, maximum.y));
            var difference = closestLocal - localCirclePosition;
            var distanceSquared = difference.sqrMagnitude;
            var radiusSquared = circle.Radius * circle.Radius;
            if (distanceSquared > radiusSquared)
            {
                contact = default;
                return false;
            }

            if (distanceSquared > DistanceEpsilonSquared)
            {
                var distance = Mathf.Sqrt(distanceSquared);
                var normal = ToWorldDirection(box, difference / distance);
                var closest = ToWorldPoint(box, closestLocal);
                var circleSurface = circle.Position + normal * circle.Radius;
                contact = new CollisionContact(
                    (circleSurface + closest) * 0.5f,
                    normal,
                    circle.Radius - distance);
                return true;
            }

            var distanceToFace = localCirclePosition.x - minimum.x;
            var escapeDirection = Vector2.left;
            var boxSurfaceLocal = new Vector2(minimum.x, localCirclePosition.y);

            var candidateDistance = maximum.x - localCirclePosition.x;
            if (candidateDistance < distanceToFace)
            {
                distanceToFace = candidateDistance;
                escapeDirection = Vector2.right;
                boxSurfaceLocal = new Vector2(maximum.x, localCirclePosition.y);
            }

            candidateDistance = localCirclePosition.y - minimum.y;
            if (candidateDistance < distanceToFace)
            {
                distanceToFace = candidateDistance;
                escapeDirection = Vector2.down;
                boxSurfaceLocal = new Vector2(localCirclePosition.x, minimum.y);
            }

            candidateDistance = maximum.y - localCirclePosition.y;
            if (candidateDistance < distanceToFace)
            {
                distanceToFace = candidateDistance;
                escapeDirection = Vector2.up;
                boxSurfaceLocal = new Vector2(localCirclePosition.x, maximum.y);
            }

            var worldEscapeDirection = ToWorldDirection(box, escapeDirection);
            var boxSurface = ToWorldPoint(box, boxSurfaceLocal);
            var circleSurfaceInside = circle.Position + worldEscapeDirection * circle.Radius;
            contact = new CollisionContact(
                (circleSurfaceInside + boxSurface) * 0.5f,
                -worldEscapeDirection,
                circle.Radius + distanceToFace);
            return true;
        }

        private static Vector2 ToWorldPoint(CollisionShape box, Vector2 localPoint)
        {
            return box.Position + ToWorldDirection(box, localPoint);
        }

        private static Vector2 ToWorldDirection(CollisionShape box, Vector2 localDirection)
        {
            return box.AxisX * localDirection.x + box.AxisY * localDirection.y;
        }
    }
}
