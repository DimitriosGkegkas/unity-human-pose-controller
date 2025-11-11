using System;

namespace InputControllers
{
    public static class RaiseHandInputController
    {
        private const string RaisedHandsGesture = "both_hands_up";

        public static bool HasRaisedHand()
        {
            if (MyListener.Instance == null)
            {
                return false;
            }

            if (!MyListener.Instance.TryGetLatestPayload(out var payload) || payload == null)
            {
                return false;
            }

            return string.Equals(payload.Gesture, RaisedHandsGesture, StringComparison.OrdinalIgnoreCase);
        }
    }
}


