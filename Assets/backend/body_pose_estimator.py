"""Body pose estimation helpers using MediaPipe."""

from __future__ import annotations

from dataclasses import dataclass
from typing import Any, Optional

import cv2
import mediapipe as mp


@dataclass
class BodyPoseResult:
    """Container that mirrors MediaPipe pose outputs we care about."""

    landmarks: Optional[Any]
    world_landmarks: Optional[Any]


class BodyPoseEstimator:
    """Thin wrapper around MediaPipe Pose."""

    def __init__(
        self,
        min_detection_confidence: float = 0.5,
        min_tracking_confidence: float = 0.5,
        model_complexity: int = 1,
    ) -> None:
        self._pose = mp.solutions.pose.Pose(
            min_detection_confidence=min_detection_confidence,
            min_tracking_confidence=min_tracking_confidence,
            model_complexity=model_complexity,
        )

    def get_body_pose(self, frame):  # -> BodyPoseResult:
        """Estimate body pose for the provided BGR frame."""
        if frame is None:
            return BodyPoseResult(None, None)

        image_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        image_rgb.flags.writeable = False
        results = self._pose.process(image_rgb)
        return BodyPoseResult(
            landmarks=results.pose_landmarks,
            world_landmarks=results.pose_world_landmarks,
        )

    def close(self) -> None:
        if getattr(self, "_pose", None) is not None:
            self._pose.close()
            self._pose = None

    def __del__(self) -> None:
        self.close()

