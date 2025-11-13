using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class PosePayloadParser
{
    public static bool TryParse(string dataString, out MyListener.PosePayload payload)
    {
        payload = null;

        if (string.IsNullOrWhiteSpace(dataString))
        {
            return false;
        }

        var result = new MyListener.PosePayload();
        bool hasData = false;

        string[] tokens = dataString.Split('|');
        int index = 0;

        while (index < tokens.Length)
        {
            string token = tokens[index].Trim();
            if (string.IsNullOrEmpty(token))
            {
                index++;
                continue;
            }

            if (token.StartsWith("body_world:", StringComparison.OrdinalIgnoreCase))
            {
                result.BodyWorld = ParseIndexedVector3List(token.Substring("body_world:".Length));
                hasData = true;
                index++;
                continue;
            }

            if (token.StartsWith("body_image:", StringComparison.OrdinalIgnoreCase))
            {
                result.BodyImage = ParseIndexedVector3List(token.Substring("body_image:".Length));
                hasData = true;
                index++;
                continue;
            }

            if (token.StartsWith("metrics:", StringComparison.OrdinalIgnoreCase))
            {
                result.Metrics = ParseMetrics(token.Substring("metrics:".Length));
                hasData = true;
                index++;
                continue;
            }

            if (token.StartsWith("gesture:", StringComparison.OrdinalIgnoreCase))
            {
                result.Gesture = token.Substring("gesture:".Length);
                hasData = true;
                index++;
                continue;
            }

            if (token.StartsWith("hands:", StringComparison.OrdinalIgnoreCase))
            {
                var handParts = CollectSection(tokens, ref index, token, "hands:");
                ParseHands(handParts, result.Hands);
                hasData = result.Hands.Count > 0 || hasData;
                continue;
            }

            if (token.StartsWith("hand_states:", StringComparison.OrdinalIgnoreCase))
            {
                var stateParts = CollectSection(tokens, ref index, token, "hand_states:");
                ParseHandStates(stateParts, result.HandStates);
                hasData = result.HandStates.Count > 0 || hasData;
                continue;
            }

            if (token.StartsWith("arm_segments:", StringComparison.OrdinalIgnoreCase))
            {
                var segmentParts = CollectSection(tokens, ref index, token, "arm_segments:");
                ParseArmSegments(segmentParts, result.ArmSegments);
                hasData = result.ArmSegments.Count > 0 || hasData;
                continue;
            }

            index++;
        }

        if (!hasData)
        {
            return false;
        }

        payload = result;
        return true;
    }

    private static List<string> CollectSection(string[] tokens, ref int index, string currentToken, string header)
    {
        var parts = new List<string>();
        string first = currentToken.Substring(header.Length);
        if (!string.IsNullOrEmpty(first))
        {
            parts.Add(first);
        }

        index++;
        while (index < tokens.Length)
        {
            string peek = tokens[index].Trim();
            if (IsSectionHeader(peek))
            {
                break;
            }

            if (!string.IsNullOrEmpty(peek))
            {
                parts.Add(peek);
            }

            index++;
        }

        return parts;
    }

    private static bool IsSectionHeader(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        return token.StartsWith("body_world:", StringComparison.OrdinalIgnoreCase) ||
               token.StartsWith("body_image:", StringComparison.OrdinalIgnoreCase) ||
               token.StartsWith("hands:", StringComparison.OrdinalIgnoreCase) ||
               token.StartsWith("hand_states:", StringComparison.OrdinalIgnoreCase) ||
               token.StartsWith("metrics:", StringComparison.OrdinalIgnoreCase) ||
               token.StartsWith("gesture:", StringComparison.OrdinalIgnoreCase) ||
               token.StartsWith("arm_segments:", StringComparison.OrdinalIgnoreCase);
    }

    private static Vector3[] ParseIndexedVector3List(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return Array.Empty<Vector3>();
        }

        string[] entries = data.Split(';');
        var parsed = new Dictionary<int, Vector3>();
        int maxIndex = -1;

        foreach (string entry in entries)
        {
            string trimmed = entry.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            string[] pair = trimmed.Split(':');
            if (pair.Length != 2)
            {
                continue;
            }

            if (!int.TryParse(pair[0], out int parsedIndex))
            {
                continue;
            }

            Vector3? vector = ParseVector3(pair[1]);
            if (vector.HasValue)
            {
                parsed[parsedIndex] = vector.Value;
                if (parsedIndex > maxIndex)
                {
                    maxIndex = parsedIndex;
                }
            }
        }

        if (maxIndex < 0)
        {
            return Array.Empty<Vector3>();
        }

        Vector3[] result = new Vector3[maxIndex + 1];
        for (int i = 0; i <= maxIndex; i++)
        {
            if (parsed.TryGetValue(i, out Vector3 value))
            {
                result[i] = value;
            }
        }

        return result;
    }

    private static void ParseHands(IEnumerable<string> parts, Dictionary<string, Vector3[]> destination)
    {
        foreach (string part in parts)
        {
            string trimmed = part.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            string[] pair = trimmed.Split(':');
            if (pair.Length < 2)
            {
                continue;
            }

            string handKey = pair[0];
            string coords = trimmed.Substring(handKey.Length + 1);

            Vector3[] positions = ParseIndexedVector3List(coords);
            if (positions.Length > 0)
            {
                destination[handKey] = positions;
            }
        }
    }

    private static void ParseHandStates(IEnumerable<string> parts, Dictionary<string, MyListener.HandStateData> destination)
    {
        foreach (string part in parts)
        {
            string trimmed = part.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            int colonIndex = trimmed.IndexOf(':');
            if (colonIndex <= 0)
            {
                continue;
            }

            string handKey = trimmed.Substring(0, colonIndex);
            string payload = trimmed.Substring(colonIndex + 1);

            string[] properties = payload.Split(',');

            float x = 0f;
            float y = 0f;
            bool hasX = false;
            bool hasY = false;
            string direction = null;
            bool pointing = false;
            string gesture = null;
            float nx = 0f;
            float ny = 0f;
            float nz = 0f;
            bool hasNx = false;
            bool hasNy = false;
            bool hasNz = false;

            foreach (string property in properties)
            {
                string[] kvp = property.Split('=');
                if (kvp.Length != 2)
                {
                    continue;
                }

                string key = kvp[0];
                string value = kvp[1];

                switch (key)
                {
                    case "x":
                        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedX))
                        {
                            x = parsedX;
                            hasX = true;
                        }
                        break;
                    case "y":
                        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedY))
                        {
                            y = parsedY;
                            hasY = true;
                        }
                        break;
                    case "dir":
                        direction = value;
                        break;
                    case "pointing":
                        pointing = value == "1" ||
                                   value.Equals("true", StringComparison.OrdinalIgnoreCase);
                        break;
                    case "gesture":
                        gesture = value;
                        break;
                    case "nx":
                        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedNx))
                        {
                            nx = parsedNx;
                            hasNx = true;
                        }
                        break;
                    case "ny":
                        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedNy))
                        {
                            ny = parsedNy;
                            hasNy = true;
                        }
                        break;
                    case "nz":
                        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedNz))
                        {
                            nz = parsedNz;
                            hasNz = true;
                        }
                        break;
                }
            }

            var state = new MyListener.HandStateData
            {
                Handedness = handKey,
                Position = new Vector2(hasX ? x : 0f, hasY ? y : 0f),
                Direction = string.IsNullOrEmpty(direction) ? "none" : direction,
                IsPointing = pointing,
                Gesture = string.IsNullOrEmpty(gesture) ? "none" : gesture,
                PalmNormal = new Vector3(nx, ny, nz),
                HasPalmNormal = hasNx && hasNy && hasNz,
            };

            destination[handKey] = state;
        }
    }

    private static void ParseArmSegments(IEnumerable<string> parts, Dictionary<string, MyListener.ArmSegmentData> destination)
    {
        foreach (string part in parts)
        {
            if (TryParseArmSegment(part, out MyListener.ArmSegmentData segment))
            {
                destination[segment.Name] = segment;
            }
        }
    }

    private static bool TryParseArmSegment(string token, out MyListener.ArmSegmentData segment)
    {
        segment = default;
        string trimmed = token?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return false;
        }

        int colonIndex = trimmed.IndexOf(':');
        if (colonIndex <= 0)
        {
            return false;
        }

        string name = trimmed.Substring(0, colonIndex);
        string payload = trimmed.Substring(colonIndex + 1);
        string[] components = payload.Split(';');

        Vector3? direction = null;

        foreach (string component in components)
        {
            string[] kvp = component.Split('=');
            if (kvp.Length != 2)
            {
                continue;
            }

            string key = kvp[0];
            string value = kvp[1];

            switch (key)
            {
                case "dir":
                    direction = ParseVector3(value);
                    break;
            }
        }

        if (direction.HasValue)
        {
            segment = new MyListener.ArmSegmentData
            {
                Name = name,
                Direction = direction.Value,
            };
            return true;
        }

        return false;
    }

    private static Vector3? ParseVector3(string value)
    {
        string[] comps = value.Split(',');
        if (comps.Length < 3)
        {
            return null;
        }

        if (float.TryParse(comps[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
            float.TryParse(comps[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) &&
            float.TryParse(comps[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
        {
            return new Vector3(x, y, z);
        }

        return null;
    }

    private static MyListener.PoseMetrics ParseMetrics(string value)
    {
        var metrics = new MyListener.PoseMetrics();
        string[] parts = value.Split(',');

        foreach (string part in parts)
        {
            string[] kvp = part.Split('=');
            if (kvp.Length != 2)
            {
                continue;
            }

            if (kvp[0] == "body" && int.TryParse(kvp[1], out int bodyCount))
            {
                metrics.BodyLandmarkCount = bodyCount;
            }
            else if (kvp[0] == "hands" && int.TryParse(kvp[1], out int handCount))
            {
                metrics.HandLandmarkCount = handCount;
            }
        }

        return metrics;
    }
}

