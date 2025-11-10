"""Pose-related calculations and gesture recognition."""

from __future__ import annotations

from dataclasses import dataclass


@dataclass
class PoseMetrics:
    body_landmark_count: int
    hand_landmark_count: int


class PoseCalculator:
    """Computes simple metrics extracted from body and hand landmarks."""

    def compute(self, body_result, hand_result) -> PoseMetrics:
        body_count = 0
        if body_result and body_result.landmarks:
            body_count = len(body_result.landmarks.landmark)

        hand_count = 0
        if hand_result and hand_result.normalized:
            hand_count = sum(len(hand.landmark) for hand in hand_result.normalized)

        return PoseMetrics(body_landmark_count=body_count, hand_landmark_count=hand_count)


class BodyGestureRecognizer:
    """Derives a simple body gesture label from pose landmarks."""

    # MediaPipe pose landmark indices of interest
    LEFT_WRIST = 15
    RIGHT_WRIST = 16
    LEFT_SHOULDER = 11
    RIGHT_SHOULDER = 12

    def get_body_gesture(self, body_result) -> str:
        if not body_result or not body_result.landmarks:
            return "no_body_detected"

        landmarks = body_result.landmarks.landmark
        try:
            left_wrist = landmarks[self.LEFT_WRIST]
            right_wrist = landmarks[self.RIGHT_WRIST]
            left_shoulder = landmarks[self.LEFT_SHOULDER]
            right_shoulder = landmarks[self.RIGHT_SHOULDER]
        except IndexError:
            return "insufficient_landmarks"

        left_hand_up = left_wrist.y < left_shoulder.y
        right_hand_up = right_wrist.y < right_shoulder.y

        if left_hand_up and right_hand_up:
            return "both_hands_up"
        if left_hand_up:
            return "left_hand_up"
        if right_hand_up:
            return "right_hand_up"

        return "neutral"

