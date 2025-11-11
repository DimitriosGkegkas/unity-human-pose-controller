using System;
using System.Collections.Generic;

namespace InputControllers
{
    public static class ControlRhythmInputController
    {
        private const int HistoryLimit = 9;
        private const int RequiredPatternRepeats = 2;

        private static readonly List<string> DirectionHistory = new List<string>(HistoryLimit);
        private static readonly Dictionary<string, int> DirectionTotals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private static readonly (string Name, string[] Sequence)[] RhythmPatterns =
        {
            ("4/4", new[] { "down", "left", "right", "up" }),
            ("3/4", new[] { "down", "right", "up" }),
            ("2/4", new[] { "down", "up" }),
        };

        public static string CurrentRhythm { get; private set; }
        public static IReadOnlyDictionary<string, int> DirectionCounts => DirectionTotals;
        public static IReadOnlyList<string> GetDirectionHistorySnapshot()
        {
            return DirectionHistory.ToArray();
        }

        public static bool IsControllingRhythm()
        {
            if (MyListener.Instance == null)
            {
                CurrentRhythm = null;
                return false;
            }

            if (!MyListener.Instance.TryGetLatestPayload(out var payload) ||
                payload?.HandStates == null ||
                payload.HandStates.Count == 0)
            {
                CurrentRhythm = null;
                return false;
            }

            if (!TryGetRightHandDirection(payload.HandStates, out var direction))
            {
                CurrentRhythm = null;
                return false;
            }

            string normalizedDirection = NormalizeDirection(direction);
            if (!string.IsNullOrEmpty(normalizedDirection))
            {
                TrackDirection(normalizedDirection);
            }

            if (TryDetectRhythm(out string rhythm))
            {
                CurrentRhythm = rhythm;
                return true;
            }

            CurrentRhythm = null;
            return false;
        }

        private static bool TryGetRightHandDirection(Dictionary<string, MyListener.HandStateData> handStates, out string direction)
        {
            foreach (var entry in handStates)
            {
                MyListener.HandStateData state = entry.Value;
                if (IsRightHand(state.Handedness) || IsRightHand(entry.Key))
                {
                    direction = state.Direction;
                    return true;
                }
            }

            direction = null;
            return false;
        }

        private static bool IsRightHand(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                return false;
            }

            return string.Equals(label.Trim(), "right", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeDirection(string direction)
        {
            if (string.IsNullOrWhiteSpace(direction))
            {
                return null;
            }

            string token = direction.Trim().ToLowerInvariant();

            if (token == "none")
            {
                return null;
            }

            if (token == "up" ||
                token == "down" ||
                token == "left" ||
                token == "right")
            {
                return token;
            }

            return null;
        }

        private static void TrackDirection(string direction)
        {
            if (!DirectionTotals.TryGetValue(direction, out int total))
            {
                total = 0;
            }

            DirectionTotals[direction] = total + 1;

            if (DirectionHistory.Count > 0 &&
                string.Equals(DirectionHistory[DirectionHistory.Count - 1], direction, StringComparison.Ordinal))
            {
                return;
            }

            DirectionHistory.Add(direction);
            if (DirectionHistory.Count > HistoryLimit)
            {
                DirectionHistory.RemoveAt(0);
            }
        }

        private static bool TryDetectRhythm(out string rhythmName)
        {
            foreach (var pattern in RhythmPatterns)
            {
                if (ContainsPattern(DirectionHistory, pattern.Sequence, RequiredPatternRepeats))
                {
                    rhythmName = pattern.Name;
                    return true;
                }
            }

            rhythmName = null;
            return false;
        }

        private static bool ContainsPattern(List<string> history, string[] pattern, int requiredRepeats)
        {
            if (requiredRepeats <= 0)
            {
                return false;
            }

            int windowLength = pattern.Length * requiredRepeats;
            if (history.Count < windowLength)
            {
                return false;
            }

            for (int start = history.Count - windowLength; start >= 0; start--)
            {
                bool allMatch = true;
                for (int repeat = 0; repeat < requiredRepeats && allMatch; repeat++)
                {
                    for (int i = 0; i < pattern.Length; i++)
                    {
                        int index = start + (repeat * pattern.Length) + i;
                        if (!string.Equals(history[index], pattern[i], StringComparison.Ordinal))
                        {
                            allMatch = false;
                            break;
                        }
                    }
                }

                if (allMatch)
                {
                    return true;
                }
            }

            return false;
        }
    }
}


