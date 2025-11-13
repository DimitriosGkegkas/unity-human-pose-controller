"""Pose-related calculations and gesture recognition."""

from __future__ import annotations

from dataclasses import dataclass
from typing import Iterable, Optional, Sequence, Tuple, Union

import numpy as np

LandmarkLike = Union[Sequence[float], "LandmarkProtocol"]


class LandmarkProtocol:
    x: float
    y: float
    z: float


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
        if not body_result:
            return "no_body_detected"

        landmarks = getattr(body_result, "landmarks_world", None)
        if landmarks is None:
            return "no_body_detected"

        landmarks = self._ensure_sequence(landmarks)

        try:
            left_wrist = landmarks[self.LEFT_WRIST]
            right_wrist = landmarks[self.RIGHT_WRIST]
            left_shoulder = landmarks[self.LEFT_SHOULDER]
            right_shoulder = landmarks[self.RIGHT_SHOULDER]
        except IndexError:
            return "insufficient_landmarks"

        left_wrist_coords = self._coords(left_wrist)
        right_wrist_coords = self._coords(right_wrist)
        left_shoulder_coords = self._coords(left_shoulder)
        right_shoulder_coords = self._coords(right_shoulder)

        if (
            left_wrist_coords is None
            or right_wrist_coords is None
            or left_shoulder_coords is None
            or right_shoulder_coords is None
        ):
            return "insufficient_landmarks"

        left_hand_up = left_wrist_coords[1] > left_shoulder_coords[1]
        right_hand_up = right_wrist_coords[1] > right_shoulder_coords[1]

        if left_hand_up and right_hand_up:
            return "both_hands_up"
        if left_hand_up:
            return "left_hand_up"
        if right_hand_up:
            return "right_hand_up"

        return "neutral"

    @staticmethod
    def _ensure_sequence(landmarks: Union[np.ndarray, Iterable[LandmarkLike]]) -> Sequence[LandmarkLike]:
        if isinstance(landmarks, np.ndarray):
            return landmarks

        if hasattr(landmarks, "landmark"):
            return landmarks.landmark

        if isinstance(landmarks, Sequence):
            return landmarks

        return tuple(landmarks)

    @staticmethod
    def _coords(landmark: LandmarkLike) -> Optional[Tuple[float, float, float]]:
        if landmark is None:
            return None

        if hasattr(landmark, "x"):
            return (float(landmark.x), float(landmark.y), float(landmark.z))

        try:
            return (float(landmark[0]), float(landmark[1]), float(landmark[2]))
        except (TypeError, IndexError):
            return None

