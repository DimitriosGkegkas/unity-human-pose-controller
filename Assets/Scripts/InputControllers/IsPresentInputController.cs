using System;

namespace InputControllers
{
    public static class IsPresentInputController
    {
        public static bool IsPresent()
        {
            if (MyListener.Instance == null)
            {
                return false;
            }

            if (!MyListener.Instance.TryGetLatestPayload(out var payload) || payload == null)
            {
                return false;
            }

            bool hasBodyData =
                (payload.BodyWorld != null && payload.BodyWorld.Length > 0) ||
                (payload.BodyImage != null && payload.BodyImage.Length > 0) ||
                payload.Metrics.BodyLandmarkCount > 0;

            bool hasHandData =
                (payload.Hands != null && payload.Hands.Count > 0) ||
                (payload.HandStates != null && payload.HandStates.Count > 0) ||
                payload.Metrics.HandLandmarkCount > 0;

            return hasBodyData && hasHandData;
        }
    }
}


