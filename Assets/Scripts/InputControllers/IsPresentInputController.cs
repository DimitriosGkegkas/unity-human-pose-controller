using System;

namespace InputControllers
{
    public static class IsPresentInputController
    {
        private static readonly object payloadSync = new object();
        private static bool isSubscribed;
        private static MyListener.PosePayload latestPayload;

        public static bool IsPresent()
        {
            EnsureSubscribed();

            var payload = GetLatestPayload();
            if (payload == null)
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

        private static void EnsureSubscribed()
        {
            if (isSubscribed)
            {
                return;
            }

            MyListener.OnNewPosePayload += HandlePayload;
            isSubscribed = true;
        }

        private static void HandlePayload(MyListener.PosePayload payload)
        {
            lock (payloadSync)
            {
                latestPayload = payload;
            }
        }

        private static MyListener.PosePayload GetLatestPayload()
        {
            lock (payloadSync)
            {
                return latestPayload;
            }
        }
    }
}


