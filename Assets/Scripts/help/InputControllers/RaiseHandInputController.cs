using System;

namespace InputControllers
{
    public static class RaiseHandInputController
    {
        private const string RaisedHandsGesture = "both_hands_up";
        private static readonly object payloadSync = new object();
        private static bool isSubscribed;
        private static string latestGesture;

        public static bool HasRaisedHand()
        {
            EnsureSubscribed();

            string gesture = GetLatestGesture();
            return !string.IsNullOrEmpty(gesture) &&
                   string.Equals(gesture, RaisedHandsGesture, StringComparison.OrdinalIgnoreCase);
        }

        private static void EnsureSubscribed()
        {
            if (isSubscribed)
            {
                return;
            }

            MyListener.OnGestureUpdated += HandleGesture;
            isSubscribed = true;
        }

        private static void HandleGesture(string gesture)
        {
            lock (payloadSync)
            {
                latestGesture = gesture;
            }
        }

        private static string GetLatestGesture()
        {
            lock (payloadSync)
            {
                return latestGesture;
            }
        }
    }
}


