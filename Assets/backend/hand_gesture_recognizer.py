"""Wrapper around MediaPipe Tasks hand gesture recognizer."""

from __future__ import annotations

import os
from dataclasses import dataclass
from typing import List, Optional

import cv2
import mediapipe as mp
from mediapipe.tasks import python as mp_python
from mediapipe.tasks.python import vision


DEFAULT_MODEL_PATH = os.path.join(
    os.path.dirname(__file__), "models", "gesture_recognizer.task"
)


@dataclass
class RecognizedHandGesture:
    handedness: str
    gesture: Optional[str]
    score: float


class HandGestureRecognizer:
    """MediaPipe gesture recognizer backed by a task model."""

    def __init__(
        self,
        model_path: Optional[str] = None,
        min_detection_confidence: float = 0.5,
        min_tracking_confidence: float = 0.5,
    ) -> None:
        self._model_path = model_path or DEFAULT_MODEL_PATH
        if not os.path.exists(self._model_path):
            raise FileNotFoundError(
                "Gesture recognizer model not found at %s. "
                "Download it from https://storage.googleapis.com/mediapipe-models/gesture_recognizer/gesture_recognizer/float16/1/gesture_recognizer.task "
                "and place it under Assets/backend/models/." % self._model_path
            )

        base_options = mp_python.BaseOptions(model_asset_path=self._model_path)
        options = vision.GestureRecognizerOptions(
            base_options=base_options,
            running_mode=vision.RunningMode.VIDEO,
            min_hand_detection_confidence=min_detection_confidence,
            min_tracking_confidence=min_tracking_confidence,
        )

        self._recognizer = vision.GestureRecognizer.create_from_options(options)
        self._last_timestamp_ms = 0

    def recognize(self, frame, timestamp_ms: Optional[int] = None) -> List[RecognizedHandGesture]:
        if frame is None:
            return []

        if timestamp_ms is None:
            self._last_timestamp_ms += 33
            timestamp_ms = self._last_timestamp_ms
        else:
            self._last_timestamp_ms = timestamp_ms

        rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb_frame)

        result = self._recognizer.recognize_for_video(mp_image, timestamp_ms)
        if result is None:
            return []

        recognitions: List[RecognizedHandGesture] = []
        handedness_sets = result.handedness or []
        gesture_sets = result.gestures or []

        for idx, handedness_set in enumerate(handedness_sets):
            handedness_label = (
                handedness_set[0].category_name.lower()
                if handedness_set
                else f"hand{idx}"
            )

            gesture_label: Optional[str] = None
            score = 0.0
            if idx < len(gesture_sets) and gesture_sets[idx]:
                top_gesture = gesture_sets[idx][0]
                gesture_label = top_gesture.category_name.lower()
                score = top_gesture.score

            recognitions.append(
                RecognizedHandGesture(
                    handedness=handedness_label,
                    gesture=gesture_label,
                    score=score,
                )
            )

        return recognitions

    def close(self) -> None:
        if getattr(self, "_recognizer", None) is not None:
            self._recognizer.close()
            self._recognizer = None

    def __del__(self) -> None:
        self.close()


