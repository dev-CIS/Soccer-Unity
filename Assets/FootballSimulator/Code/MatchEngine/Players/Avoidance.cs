using System.Linq;
using UnityEngine;

namespace FStudio.MatchEngine.Players {
    public partial class PlayerBase {
        private const float AVOIDANCE_MIN_ANGLE = -50;
        private const float AVOIDANCE_MAX_ANGLE = 50;
        private AnimationCurve avoidanceCurve;

        public void AvoidMarkers (
            PlayerBase[] targets, 
            ref Vector3 targetPosition, 
            float avoidanceDistance = 7) {

            if (avoidanceCurve == null) {
                avoidanceCurve = EngineSettings.Current.AvoidanceCurve;
            }

            var playerPos = Position;

            var dir = targetPosition - playerPos;

            Debug.DrawLine(playerPos + Vector3.up, playerPos + Vector3.up + dir, Color.white, 0.05f);

            var distToTargetPos = dir.magnitude;

            (float avoidancePow, Vector3 avoidDir) avoid(PlayerBase marker) {
                var markerPos = marker.Position;

                var dirToMe = playerPos - markerPos;

                var avoidancePow = Mathf.Max(0, (avoidanceDistance - dirToMe.magnitude) / avoidanceDistance);

                float angleMod = avoidanceCurve.Evaluate(avoidancePow);

                var debugColor = Color.Lerp(Color.white, Color.red, angleMod);
                debugColor.a = angleMod;

                Debug.DrawLine(playerPos + Vector3.up, markerPos + Vector3.up, debugColor, 0.05f);

                return (angleMod, dirToMe.normalized);
            }

            Vector3 finalDir = Vector3.zero;
            bool anyAvoidance = false;

            foreach (var target in targets) {
                if (target.IsGK || !target.PlayerController.IsPhysicsEnabled) {
                    continue;
                }

                var avoidTarget = avoid(target);

                if (avoidTarget.avoidancePow > 0) {
                    anyAvoidance = true;
                    finalDir += avoidTarget.avoidDir * avoidTarget.avoidancePow;
                }
            }

            if (!anyAvoidance) {
                return;
            }

            Debug.DrawLine(playerPos + Vector3.up, playerPos + Vector3.up + finalDir, Color.blue, 0.05f);

            var angle = Vector3.SignedAngle(dir, finalDir, Vector3.up);

            angle = Mathf.Clamp(angle, AVOIDANCE_MIN_ANGLE, AVOIDANCE_MAX_ANGLE);

            finalDir = Quaternion.Euler(0, angle, 0) * dir.normalized;

            targetPosition = playerPos + finalDir * 2;
        }
    }
}
