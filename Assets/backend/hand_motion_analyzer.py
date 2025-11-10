"""Hand motion analysis utilities."""

from __future__ import annotations

from collections import deque
from dataclasses import dataclass
from statistics import mean
from typing import Deque, Dict, Iterable, List, Optional, Tuple

from hand_gesture_recognizer import RecognizedHandGesture
from hand_pose_estimator import HandPoseResult


HandPosition = Tuple[float, float]


@dataclass
class HandState:
    """Output of the hand motion analyzer for a single hand."""

    handedness: str
    position: HandPosition
    direction: str
    is_pointing: bool
    gesture: Optional[str] = None


class HandMotionAnalyzer:
    """Tracks hand motion across frames and detects pointing gestures."""

    def __init__(
        self,
        history_size: int = 6,
        consistency_window: int = 3,
        movement_threshold_px: float = 8.0,
        pointing_extension_threshold: float = 0.08,
        pointing_margin: float = 0.02,
    ) -> None:
        self._history_size = history_size
        self._consistency_window = consistency_window
        self._movement_threshold_px = movement_threshold_px
        self._pointing_extension_threshold = pointing_extension_threshold
        self._pointing_margin = pointing_margin

        self._position_history: Dict[str, Deque[HandPosition]] = {}
        self._direction_history: Dict[str, Deque[str]] = {}

    def analyze(
        self,
        frame_shape,
        hand_result: Optional[HandPoseResult],
        recognized_gestures: Optional[List[RecognizedHandGesture]] = None,
    ) -> List[HandState]:
        if not hand_result or not hand_result.normalized:
            return []

        height, width = frame_shape[:2]
        handedness_labels = hand_result.handedness or []
        gesture_lookup = {
            gesture.handedness: gesture for gesture in recognized_gestures or []
        }

        states: List[HandState] = []
        for idx, landmarks in enumerate(hand_result.normalized):
            label = handedness_labels[idx] if idx < len(handedness_labels) else f"hand{idx}"
            label = label.lower()

            wrist = landmarks.landmark[0]
            position = (wrist.x * width, wrist.y * height)

            direction = self._compute_direction(label, position)
            recognized_gesture = gesture_lookup.get(label)
            gesture_label: Optional[str] = None
            if recognized_gesture and recognized_gesture.gesture:
                gesture_label = recognized_gesture.gesture

            if recognized_gesture and recognized_gesture.gesture:
                is_pointing = "point" in recognized_gesture.gesture
            else:
                is_pointing = self._detect_pointing(landmarks)

            states.append(
                HandState(
                    handedness=label,
                    position=position,
                    direction=direction,
                    is_pointing=is_pointing,
                    gesture=gesture_label,
                )
            )

        return states

    def _compute_direction(self, label: str, position: HandPosition) -> str:
        history = self._position_history.setdefault(label, deque(maxlen=self._history_size))
        direction_memory = self._direction_history.setdefault(label, deque(maxlen=self._history_size))

        history.append(position)

        if len(history) < 2:
            direction_memory.append("none")
            return "none"

        delta_x, delta_y = self._movement_vector(history)

        magnitude_x = abs(delta_x)
        magnitude_y = abs(delta_y)

        if magnitude_x < self._movement_threshold_px and magnitude_y < self._movement_threshold_px:
            direction_memory.append("none")
            return "none"

        if magnitude_x > magnitude_y:
            direction = "right" if delta_x > 0 else "left"
        else:
            direction = "down" if delta_y > 0 else "up"

        direction_memory.append(direction)
        return self._consistent_direction(direction_memory)

    def _movement_vector(self, history: Deque[HandPosition]) -> HandPosition:
        current_x, current_y = history[-1]
        if len(history) == 2:
            prev_x, prev_y = history[-2]
            return current_x - prev_x, current_y - prev_y

        lookback = min(len(history) - 1, self._consistency_window + 1)
        previous_samples = list(history)[-1 - lookback : -1]
        prev_x = mean(point[0] for point in previous_samples)
        prev_y = mean(point[1] for point in previous_samples)
        return current_x - prev_x, current_y - prev_y

    def _consistent_direction(self, history: Deque[str]) -> str:
        window = list(history)[-self._consistency_window :]
        if len(window) < self._consistency_window:
            return window[-1] if window else "none"

        first = window[0]
        if first == "none":
            return "none"
        if all(entry == first for entry in window[1:]):
            return first
        return "none"

    def _detect_pointing(self, landmarks) -> bool:
        if not landmarks or len(landmarks.landmark) < 21:
            return False

        wrist = landmarks.landmark[0]
        index_mcp = landmarks.landmark[5]
        index_pip = landmarks.landmark[6]
        index_tip = landmarks.landmark[8]
        middle_tip = landmarks.landmark[12]
        ring_tip = landmarks.landmark[16]
        pinky_tip = landmarks.landmark[20]

        index_extension = self._distance_2d(index_tip, wrist)
        if index_extension < self._pointing_extension_threshold:
            return False

        other_extensions = self._max_distance_2d(
            [middle_tip, ring_tip, pinky_tip], wrist
        )
        if index_extension - other_extensions < self._pointing_margin:
            return False

        index_straightness = self._distance_2d(index_tip, index_pip)
        base_distance = self._distance_2d(index_mcp, wrist)

        return index_straightness > base_distance * 0.6

    @staticmethod
    def _distance_2d(a, b) -> float:
        dx = a.x - b.x
        dy = a.y - b.y
        return (dx * dx + dy * dy) ** 0.5

    def _max_distance_2d(self, points: Iterable, origin) -> float:
        return max((self._distance_2d(point, origin) for point in points), default=0.0)


