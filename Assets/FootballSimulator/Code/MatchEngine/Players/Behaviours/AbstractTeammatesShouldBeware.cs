
using UnityEngine;

namespace FStudio.MatchEngine.Players.Behaviours {
    public abstract class AbstractTeammatesShouldBeware : BallChasingBehaviour {
        private const int MAX_MARKING = 3;

        // Reused every call instead of allocating LINQ queries.
        private readonly PlayerBase[] bewareTeammates = new PlayerBase[MAX_MARKING];
        private readonly float[] bewareTeammateProgress = new float[MAX_MARKING];
        private readonly PlayerBase[] opponentsBehind = new PlayerBase[MAX_MARKING];
        private readonly float[] opponentsBehindProgress = new float[MAX_MARKING];

        public (bool shouldI, PlayerBase target) ShouldIBeware (bool ignoreOffside = false) {
            // most advanced opponents first.
            int opponentsBehindTotal = 0;
            int opponentsBehindKept = 0;
            foreach (var opponent in opponents) {
                if (opponent.IsGK || (!ignoreOffside && opponent.IsInOffside)) {
                    continue;
                }

                opponentsBehindTotal++;
                InsertTop(opponentsBehind, opponentsBehindProgress, ref opponentsBehindKept, opponent, -opponent.PlayerFieldProgress);
            }

            int opponentsBehindCount = Mathf.Min (MAX_MARKING, opponentsBehindTotal);

            // least advanced teammates first.
            int teammatesKept = 0;
            foreach (var teammate in teammates) {
                if (teammate.IsGK) {
                    continue;
                }

                InsertTop(bewareTeammates, bewareTeammateProgress, ref teammatesKept, teammate, teammate.PlayerFieldProgress);
            }

            int teammatesShouldBeBewareCount = Mathf.Min(opponentsBehindCount, teammatesKept);

            for (int i = 0; i < teammatesShouldBeBewareCount; i++) {
                if (bewareTeammates[i] == Player) {
                    return (true, opponentsBehind[i]);
                }
            }

            return default;
        }

        /// <summary>
        /// Keeps the lowest keys in ascending order; equal keys keep insertion order (like a stable OrderBy).
        /// </summary>
        private static void InsertTop (PlayerBase[] players, float[] keys, ref int count, PlayerBase player, float key) {
            int index = count;
            while (index > 0 && keys[index - 1] > key) {
                index--;
            }

            if (index >= players.Length) {
                return;
            }

            int last = Mathf.Min(count, players.Length - 1);
            for (int i = last; i > index; i--) {
                players[i] = players[i - 1];
                keys[i] = keys[i - 1];
            }

            players[index] = player;
            keys[index] = key;
            count = Mathf.Min(count + 1, players.Length);
        }
    }
}
