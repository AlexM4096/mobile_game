using System;
using System.Collections.Generic;
using Arch.Buffer;
using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Movement;
using UnityEngine;

namespace _Project.Gameplay.Features.Collision.Systems
{
    public sealed class DetectCircleCollisionsSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<Position, CircleCollider>();

        private readonly CollisionMatrix _collisionMatrix;
        private readonly List<CircleBody> _bodies = new();
        private readonly Dictionary<Entity, List<CollisionEvent>> _eventLists = new();
        private readonly HashSet<Entity> _pendingEventBuffers = new();
        private Dictionary<ContactPair, ContactState> _previousContacts = new();
        private Dictionary<ContactPair, ContactState> _currentContacts = new();

        public DetectCircleCollisionsSystem(World world, CollisionMatrix collisionMatrix) : base(world)
        {
            _collisionMatrix = collisionMatrix;
        }

        public override void Update(in SystemState state)
        {
            using var commandBuffer = new CommandBuffer();

            CollectBodiesAndPrepareEventBuffers();
            DetectContacts();
            EmitExits();
            AddPendingEventBuffers(commandBuffer);

            commandBuffer.Playback(World, false);
            SwapContactHistory();
        }

        private void CollectBodiesAndPrepareEventBuffers()
        {
            _bodies.Clear();
            _eventLists.Clear();
            _pendingEventBuffers.Clear();

            World.Query(in _description, (
                Entity entity,
                ref Position position,
                ref CircleCollider collider) =>
            {
                List<CollisionEvent> events;
                if (World.Has<CollisionEvents>(entity))
                {
                    ref var collisionEvents = ref World.Get<CollisionEvents>(entity);
                    collisionEvents.Items ??= new List<CollisionEvent>(2);
                    collisionEvents.Items.Clear();
                    events = collisionEvents.Items;
                }
                else
                {
                    events = new List<CollisionEvent>(2);
                    _pendingEventBuffers.Add(entity);
                }

                _eventLists.Add(entity, events);
                if (collider.Radius > 0f && collider.Layer != CollisionLayer.None)
                {
                    _bodies.Add(new CircleBody(entity, position.Value, collider));
                }
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

                    if (!_collisionMatrix.CanInteract(first.Collider.Layer, second.Collider.Layer))
                    {
                        continue;
                    }

                    var difference = second.Position - first.Position;
                    var radiusSum = first.Collider.Radius + second.Collider.Radius;
                    var distanceSquared = difference.sqrMagnitude;
                    if (distanceSquared > radiusSum * radiusSum)
                    {
                        continue;
                    }

                    var distance = Mathf.Sqrt(distanceSquared);
                    var normal = distance > Mathf.Epsilon ? difference / distance : Vector2.right;
                    var penetration = radiusSum - distance;
                    var kind = first.Collider.Kind == ColliderKind.Trigger ||
                               second.Collider.Kind == ColliderKind.Trigger
                        ? ColliderKind.Trigger
                        : ColliderKind.Solid;
                    var contact = new ContactState(first.Entity, second.Entity, kind, normal, penetration);
                    var pair = new ContactPair(first.Entity, second.Entity);
                    var phase = ContactPhase.Enter;

                    if (_previousContacts.TryGetValue(pair, out var previousContact))
                    {
                        if (previousContact.Kind == kind)
                        {
                            phase = ContactPhase.Stay;
                        }
                        else
                        {
                            EmitContact(previousContact, ContactPhase.Exit);
                        }
                    }

                    _currentContacts.Add(pair, contact);
                    EmitContact(contact, phase);

                    if (kind == ColliderKind.Solid)
                    {
                        SeparateSolidBodies(firstIndex, secondIndex, normal, penetration);
                    }
                }
            }
        }

        private void EmitExits()
        {
            foreach (var pair in _previousContacts)
            {
                if (!_currentContacts.ContainsKey(pair.Key))
                {
                    EmitContact(pair.Value, ContactPhase.Exit);
                }
            }
        }

        private void EmitContact(ContactState contact, ContactPhase phase)
        {
            var type = ToEventType(contact.Kind, phase);
            var penetration = phase == ContactPhase.Exit ? 0f : contact.Penetration;

            AddEvent(contact.First, new CollisionEvent
            {
                Other = contact.Second,
                Type = type,
                Normal = contact.NormalFromFirstToSecond,
                Penetration = penetration
            });
            AddEvent(contact.Second, new CollisionEvent
            {
                Other = contact.First,
                Type = type,
                Normal = -contact.NormalFromFirstToSecond,
                Penetration = penetration
            });
        }

        private void AddEvent(Entity entity, CollisionEvent collisionEvent)
        {
            if (!World.IsAlive(entity) || !_eventLists.TryGetValue(entity, out var events))
            {
                return;
            }

            events.Add(collisionEvent);
        }

        private void AddPendingEventBuffers(CommandBuffer commandBuffer)
        {
            foreach (var entity in _pendingEventBuffers)
            {
                if (World.IsAlive(entity))
                {
                    commandBuffer.Add(entity, new CollisionEvents { Items = _eventLists[entity] });
                }
            }
        }

        private void SwapContactHistory()
        {
            var oldContacts = _previousContacts;
            _previousContacts = _currentContacts;
            _currentContacts = oldContacts;
        }

        private static CollisionEventType ToEventType(ColliderKind kind, ContactPhase phase)
        {
            if (kind == ColliderKind.Trigger)
            {
                return phase switch
                {
                    ContactPhase.Enter => CollisionEventType.TriggerEnter,
                    ContactPhase.Stay => CollisionEventType.TriggerStay,
                    ContactPhase.Exit => CollisionEventType.TriggerExit,
                    _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, null)
                };
            }

            return phase switch
            {
                ContactPhase.Enter => CollisionEventType.CollisionEnter,
                ContactPhase.Stay => CollisionEventType.CollisionStay,
                ContactPhase.Exit => CollisionEventType.CollisionExit,
                _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, null)
            };
        }

        private void SeparateSolidBodies(int firstIndex, int secondIndex, Vector2 normal, float penetration)
        {
            if (penetration <= 0f)
            {
                return;
            }

            var first = _bodies[firstIndex];
            var second = _bodies[secondIndex];
            var firstDynamic = first.Collider.BodyType == ColliderBodyType.Dynamic;
            var secondDynamic = second.Collider.BodyType == ColliderBodyType.Dynamic;

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
            body.Position += offset;
            _bodies[bodyIndex] = body;

            ref var position = ref World.Get<Position>(body.Entity);
            position.Value = body.Position;
        }

        private enum ContactPhase
        {
            Enter,
            Stay,
            Exit
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
                ColliderKind kind,
                Vector2 normalFromFirstToSecond,
                float penetration)
            {
                First = first;
                Second = second;
                Kind = kind;
                NormalFromFirstToSecond = normalFromFirstToSecond;
                Penetration = penetration;
            }

            public Entity First { get; }
            public Entity Second { get; }
            public ColliderKind Kind { get; }
            public Vector2 NormalFromFirstToSecond { get; }
            public float Penetration { get; }
        }

        private struct CircleBody
        {
            public CircleBody(Entity entity, Vector2 position, CircleCollider collider)
            {
                Entity = entity;
                Position = position;
                Collider = collider;
            }

            public Entity Entity;
            public Vector2 Position;
            public CircleCollider Collider;
        }
    }
}
