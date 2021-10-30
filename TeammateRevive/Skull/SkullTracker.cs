﻿using System.Collections.Generic;
using System.Linq;
using TeammateRevive.Logging;
using UnityEngine.Networking;

namespace TeammateRevive.Skull
{
    public class SkullTracker
    {
        public static SkullTracker instance;
        
        public readonly HashSet<DeadPlayerSkull> skulls = new();

        public bool HasAnySkulls => this.skulls.Count > 0;

        public SkullTracker()
        {
            instance = this;
            DeadPlayerSkull.GlobalOnDestroy += OnSkullDestroy;
            DeadPlayerSkull.GlobalOnCreated += OnSkullUpdate;
            DeadPlayerSkull.GlobalOnValuesReceived += OnSkullUpdate;
        }

        public void Clear()
        {
            this.skulls.Clear();
        }

        private void OnSkullUpdate(DeadPlayerSkull obj)
        {
            Log.Debug("Skull updated! " + string.Join(", ", obj.insidePlayerIDs.Select(i => i.ToString())));
            this.skulls.Add(obj);
        }

        private void OnSkullDestroy(DeadPlayerSkull obj)
        {
            Log.Debug("Skull destroyed! " + string.Join(", ", obj.insidePlayerIDs.Select(i => i.ToString())));
            this.skulls.Remove(obj);
        }

        public DeadPlayerSkull GetSkullInRange(NetworkInstanceId userBodyId)
        {
            var skull = this.skulls.FirstOrDefault(s => s.insidePlayerIDs.Contains(userBodyId));
            return skull;
        }
    }
}