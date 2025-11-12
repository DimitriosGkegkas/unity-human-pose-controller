using System;

namespace InputControllers
{
    public static class RaiseHandInputController
    {
        private const string RaisedHandsGesture = "both_hands_up";
        private static readonly object payloadSync = new object();
        private static bool isSubscribed;
        private static MyListener.PosePayload latestPayload;

        public static bool HasRaisedHand()
        {
            EnsureSubscribed();

            var payload = GetLatestPayload();
            if (payload == null)
            {
                return false;
            }

            return string.Equals(payload.Gesture, RaisedHandsGesture, StringComparison.OrdinalIgnoreCase);
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


