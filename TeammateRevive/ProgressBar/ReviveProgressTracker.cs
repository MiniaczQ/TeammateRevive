﻿
using RoR2;
using RoR2.UI;
using TeammateRevive.Common;
using TeammateRevive.Configuration;
using TeammateRevive.Logging;
using TeammateRevive.Players;
using TeammateRevive.Revive;
using TeammateRevive.Skull;
using UnityEngine;
using UnityEngine.Networking;

namespace TeammateRevive.ProgressBar
{
    public class ReviveProgressTracker
    {
        private static readonly Color NegativeProgressColor = new(1, .35f, .35f);
        private static readonly Color PositiveProgressColor = Color.white;
        
        private readonly ProgressBarController progressBar;
        private readonly PlayersTracker players;
        private readonly RunTracker run;
        private readonly SkullTracker skullTracker;

        public DeadPlayerSkull trackingSkull;

        private float queuedToHideAt;
        private bool IsQueuedToHide => this.queuedToHideAt > 0;
        private readonly float delayBeforeHiding;

        private SpectatorLabel spectatorLabel;

        public ReviveProgressTracker(ProgressBarController progressBar, PlayersTracker players, RunTracker run, SkullTracker skullTracker, PluginConfig config)
        {
            this.progressBar = progressBar;
            this.players = players;
            this.run = run;
            this.skullTracker = skullTracker;
            this.delayBeforeHiding = config.ReviveTimeSeconds / RevivalTracker.ReduceReviveProgressFactor;
            
            DeadPlayerSkull.GlobalOnDestroy += OnSkullDestroy;
            On.RoR2.UI.SpectatorLabel.Awake += SpectatorLabelAwake;
        }

        private void SpectatorLabelAwake(On.RoR2.UI.SpectatorLabel.orig_Awake orig, RoR2.UI.SpectatorLabel self)
        {
            orig(self);
            this.spectatorLabel = self;
        }

        private void OnSkullDestroy(DeadPlayerSkull skull)
        {
            if (this.trackingSkull == skull)
            {
                Log.DebugMethod("removing tracking - skull destroyed");
                RemoveTracking();
            }
        }

        public void Update()
        {
            var skull = GetSkullInRange();
            
            // no skull, no tracking
            if (skull == this.trackingSkull && skull == null)
            {
                if (this.progressBar.IsShown)
                {
                    Log.DebugMethod("hide - no skull, no tracking");
                    this.progressBar.Hide();
                }
                return;
            }

            // new skull
            if (skull != null && this.trackingSkull != skull)
            {
                Log.DebugMethod("new skull");
                this.trackingSkull = skull;
                DequeFromHiding();
                this.progressBar.SetUser(skull.PlayerName);
            }
            
            // update progress
            if (this.trackingSkull != null)
            {
                this.progressBar.SetFraction(this.trackingSkull.progress);
                this.progressBar.SetColor(this.trackingSkull.fractionPerSecond >= 0 ? PositiveProgressColor : NegativeProgressColor);
            }

            // player is out of skull circle, queuing to hide
            if (skull == null && this.trackingSkull != null && !this.IsQueuedToHide)
            {
                Log.DebugMethod("queue to hide");
                QueueToHide();
            }

            // hiding either if progress become 0 or specified delay elapsed
            if (this.IsQueuedToHide && (this.trackingSkull == null || this.trackingSkull.progress == 0 || Time.time - this.queuedToHideAt > this.delayBeforeHiding))
            {
                Log.DebugMethod("removing tracking after delay");
                RemoveTracking();
            }
        }

        private void QueueToHide() => this.queuedToHideAt = Time.time;
        private void DequeFromHiding() => this.queuedToHideAt = 0;

        private void RemoveTracking()
        {
            DequeFromHiding();
            this.progressBar.Hide();
            this.trackingSkull = null;
        }

        public DeadPlayerSkull GetSkullInRange()
        {
            if (!this.skullTracker.HasAnySkulls)
                return null;

            var trackingBodyId = this.players.CurrentUserBodyId ?? GetSpectatingBody();
            if (trackingBodyId == null)
                return null;

            return this.skullTracker.GetSkullInRange(trackingBodyId.Value);
        }

        private NetworkInstanceId? GetSpectatingBody()
        {
            if (this.spectatorLabel != null && !this.spectatorLabel.gameObject.IsDestroyed() && !this.spectatorLabel.labelRoot.IsDestroyed() && this.spectatorLabel.labelRoot.activeSelf)
            {
                var target = this.spectatorLabel.cachedTarget;
                if (target.IsDestroyed())
                {
                    return null;
                }

                var characterBody = target.GetComponent<CharacterBody>();
                if (characterBody != null && !characterBody.isPlayerControlled)
                {
                    return null;
                }

                return characterBody.netId;
            }

            return null;
        }
    }
}