using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimelessEchoes.Hero
{
    public abstract partial class HeroBase
    {
        private readonly HashSet<ConversationWait> conversationWaits = new();
        private Transform conversationPosition;
        private float conversationStoppingDistance;
        public bool IsInNpcConversation => conversationWaits.Count > 0 && conversationPosition;

        // Capture the standing spot, not the moving hero transform. Combat may
        // temporarily take the player away, but cannot assign another objective.
        public IDisposable BeginNpcConversation(Transform faceTarget = null)
        {
            if (IsEchoActor || !gameObject.activeInHierarchy) return null;
            if (!IsInNpcConversation)
            {
                conversationPosition = new GameObject("NPC conversation position").transform;
                conversationPosition.SetParent(transform.parent, false);
                conversationPosition.position = transform.position;
                conversationStoppingDistance = movementController?.AI != null ? movementController.AI.endReachedDistance : .1f;
            }
            var wait = new ConversationWait(this, faceTarget);
            conversationWaits.Add(wait);
            if (!InCombat)
            {
                if (state == State.PerformingTask)
                {
                    CurrentTask?.OnInterrupt(this);
                    // Work animations have long blends; dialogue should idle immediately.
                    if (animator) animator.Play("Idle");
                    PlaySecondaryAnimation("Idle");
                }
                state = State.Idle;
                if (movementController?.Setter != null) movementController.Setter.target = conversationPosition;
                movementController?.SetSimulateMovement(false);
            }
            return wait;
        }

        private bool WaitAtNpcConversation()
        {
            if (!IsInNpcConversation) return false;
            if (InCombat) return true;
            state = State.Idle;
            if (movementController == null) return true;
            if (movementController.AI != null) movementController.AI.endReachedDistance = Mathf.Min(.1f, conversationStoppingDistance);
            if (movementController.Setter != null && movementController.Setter.target != conversationPosition)
                movementController.SetDestination(conversationPosition);
            // A*'s reachedDestination can still describe the previous combat target
            // for a frame. Check the standing spot itself before stopping.
            const float threshold = .15f;
            movementController.SetSimulateMovement(Vector2.Distance(transform.position, conversationPosition.position) > threshold);
            return true;
        }

        private Transform ConversationFacingTarget
        {
            get
            {
                if (!IsInNpcConversation || InCombat) return null;
                foreach (var wait in conversationWaits)
                    if (wait.FaceTarget) return wait.FaceTarget;
                return null;
            }
        }

        private void RestoreConversationCombatDistance()
        {
            if (conversationPosition && movementController?.AI != null)
                movementController.AI.endReachedDistance = conversationStoppingDistance;
        }

        private void EndNpcConversation(ConversationWait wait)
        {
            if (!conversationWaits.Remove(wait) || conversationWaits.Count > 0) return;
            ClearNpcConversations();
        }

        private void ClearNpcConversations()
        {
            RestoreConversationCombatDistance();
            conversationWaits.Clear();
            if (conversationPosition)
            {
                if (movementController?.Setter != null && movementController.Setter.target == conversationPosition)
                {
                    movementController.ClearTarget();
                    movementController.SetSimulateMovement(true);
                }
                Destroy(conversationPosition.gameObject);
                conversationPosition = null;
            }
        }

        private sealed class ConversationWait : IDisposable
        {
            private HeroBase owner;
            public readonly Transform FaceTarget;
            public ConversationWait(HeroBase owner, Transform faceTarget)
            { this.owner = owner; FaceTarget = faceTarget; }
            public void Dispose()
            {
                if (owner) owner.EndNpcConversation(this);
                owner = null;
            }
        }
    }
}
