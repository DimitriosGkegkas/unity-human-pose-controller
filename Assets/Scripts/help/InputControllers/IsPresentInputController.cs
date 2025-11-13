using System;

namespace InputControllers
{
    public static class IsPresentInputController
    {
        private static readonly object stateSync = new object();
        private static bool isSubscribed;
        private static bool hasHandData;
        private static bool hasArmData;

        public static bool IsPresent()
        {
            EnsureSubscribed();

            lock (stateSync)
            {
                return hasHandData && hasArmData;
            }
        }

        private static void EnsureSubscribed()
        {
            if (isSubscribed)
            {
                return;
            }

            MyListener.OnHandStatesUpdated += HandleHandStates;
            MyListener.OnArmSegmentsUpdated += HandleArmSegments;
            isSubscribed = true;
        }

        private static void HandleHandStates(System.Collections.Generic.Dictionary<string, MyListener.HandStateData> handStates)
        {
            lock (stateSync)
            {
                hasHandData = handStates != null && handStates.Count > 0;
            }
        }

        private static void HandleArmSegments(System.Collections.Generic.Dictionary<string, MyListener.ArmSegmentData> armSegments)
        {
            lock (stateSync)
            {
                hasArmData = armSegments != null && armSegments.Count > 0;
            }
        }
    }
}


