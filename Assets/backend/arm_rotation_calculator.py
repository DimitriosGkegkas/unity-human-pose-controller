"""Compute arm segment direction vectors from MediaPipe pose data."""

from __future__ import annotations

from dataclasses import dataclass
from typing import Dict, Iterable, List, Optional

import numpy as np
from numpy.linalg import norm

ROTATION_180_X = np.diag([1.0, -1.0, -1.0])
FLIP_X = np.diag([-1.0, 1.0, 1.0])


@dataclass(frozen=True)
class ArmSegmentConfig:
    name: str
    start_landmark: int
    end_landmark: int


@dataclass
class ArmSegmentRotation:
    config: ArmSegmentConfig
    direction: np.ndarray


class ArmRotationCalculator:
    """Derives normalized arm segment direction vectors."""

    SEGMENTS: Iterable[ArmSegmentConfig] = (
        ArmSegmentConfig("left_upper_arm", 11, 13),
        ArmSegmentConfig("left_lower_arm", 13, 15),
        ArmSegmentConfig("left_hand", 15, 17),

        ArmSegmentConfig("right_upper_arm", 12, 14),
        ArmSegmentConfig("right_lower_arm", 14, 16),
        ArmSegmentConfig("right_hand", 16, 18),
    )
    HEAD_CONFIG = ArmSegmentConfig("head", -1, 0)

    LEFT_SHOULDER_IDX = 11
    RIGHT_SHOULDER_IDX = 12
    NOSE_IDX = 0

    def __init__(self, smoothing_factor: float = 0.5) -> None:
        """Initialize calculator with optional exponential smoothing.

        Args:
            smoothing_factor: Value in [0, 1]. When 0, the output is fully
                smoothed (no response to new samples). When 1, no smoothing is
                applied. Defaults to 0.5 for light smoothing.
        """

        self.smoothing_factor = float(np.clip(smoothing_factor, 0.0, 1.0))
        self._previous_directions: Dict[str, np.ndarray] = {}

    def reset(self) -> None:
        """Clear the smoothing history."""

        self._previous_directions.clear()

    def compute(self, body_result) -> List[ArmSegmentRotation]:
        if not body_result or not body_result.world_landmarks:
            return []

        landmarks = body_result.world_landmarks.landmark
        segment_rotations: List[ArmSegmentRotation] = []

        for config in self.SEGMENTS:
            start = self._landmark_to_array(landmarks, config.start_landmark)
            end = self._landmark_to_array(landmarks, config.end_landmark)
            if start is None or end is None:
                continue

            direction = end - start
            direction_norm = norm(direction)
            if direction_norm < 1e-6:
                continue
            direction_unit = direction / direction_norm
            direction_unit = ROTATION_180_X @ direction_unit
            direction_unit = FLIP_X @ direction_unit

            segment_rotations.append(
                ArmSegmentRotation(
                    config=config,
                    direction=direction_unit,
                )
            )

        head_rotation = self._compute_head_rotation(landmarks)
        if head_rotation is not None:
            segment_rotations.append(head_rotation)

        return segment_rotations

    @staticmethod
    def _landmark_to_array(landmarks, index: int) -> Optional[np.ndarray]:
        if index < 0 or index >= len(landmarks):
            return None
        landmark = landmarks[index]
        return np.array([landmark.x, landmark.y, landmark.z], dtype=np.float64)

    def _apply_low_pass(
        self, key: str, direction: np.ndarray
    ) -> np.ndarray:
        if self.smoothing_factor >= 0.999:
            self._previous_directions[key] = direction
            return direction

        alpha = self.smoothing_factor

        prev_direction = self._previous_directions.get(key)
        if prev_direction is None:
            direction_smoothed = direction
        else:
            direction_smoothed = alpha * direction + (1.0 - alpha) * prev_direction
            norm = np.linalg.norm(direction_smoothed)
            if norm < 1e-6:
                direction_smoothed = prev_direction
            else:
                direction_smoothed /= norm

        self._previous_directions[key] = direction_smoothed

        return direction_smoothed

    def _compute_head_rotation(self, landmarks) -> Optional[ArmSegmentRotation]:
        left_shoulder = self._landmark_to_array(landmarks, self.LEFT_SHOULDER_IDX)
        right_shoulder = self._landmark_to_array(landmarks, self.RIGHT_SHOULDER_IDX)
        nose = self._landmark_to_array(landmarks, self.NOSE_IDX)

        if left_shoulder is None or right_shoulder is None or nose is None:
            return None

        shoulder_center = (left_shoulder + right_shoulder) * 0.5
        direction = nose - shoulder_center
        norm = np.linalg.norm(direction)
        if norm < 1e-6:
            return None

        direction_unit = direction / norm
        direction_smoothed = self._apply_low_pass(
            self.HEAD_CONFIG.name, direction_unit
        )

        return ArmSegmentRotation(
            config=self.HEAD_CONFIG,
            direction=direction_smoothed,
        )

