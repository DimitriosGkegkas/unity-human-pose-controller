"""Hand pose estimation helpers using MediaPipe."""

from __future__ import annotations

from dataclasses import dataclass
from typing import Any, List, Optional

import cv2
import mediapipe as mp


@dataclass
class HandPoseResult:
    """Subset of MediaPipe Hands output."""

    normalized: Optional[List[Any]]
    world: Optional[List[Any]]
    handedness: Optional[List[str]]


class HandPoseEstimator:
    """Thin wrapper around MediaPipe Hands."""

    def __init__(
        self,
        max_num_hands: int = 2,
        min_detection_confidence: float = 0.5,
        min_tracking_confidence: float = 0.5,
    ) -> None:
        self._hands = mp.solutions.hands.Hands(
            max_num_hands=max_num_hands,
            min_detection_confidence=min_detection_confidence,
            min_tracking_confidence=min_tracking_confidence,
        )

    def get_hand_pose(self, frame):  # -> HandPoseResult:
        if frame is None:
            return HandPoseResult(None, None, None)

        image_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        image_rgb.flags.writeable = False
        results = self._hands.process(image_rgb)
        handedness = None
        if results and results.multi_handedness:
            handedness = [
                classification.classification[0].label.lower()
                for classification in results.multi_handedness
                if classification.classification
            ]

        return HandPoseResult(
            normalized=results.multi_hand_landmarks,
            world=results.multi_hand_world_landmarks,
            handedness=handedness,
        )

    def close(self) -> None:
        if getattr(self, "_hands", None) is not None:
            self._hands.close()
            self._hands = None

    def __del__(self) -> None:
        self.close()

