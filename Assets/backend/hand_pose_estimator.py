"""Hand pose estimation helpers using MediaPipe."""

from __future__ import annotations

import os
from dataclasses import dataclass, field
from typing import Any, List, Optional

import cv2
import numpy as np
import mediapipe as mp
from mediapipe.tasks import python as mp_python
from mediapipe.tasks.python import vision
    
DEFAULT_GESTURE_MODEL_PATH = os.path.join(
    os.path.dirname(__file__), "models", "gesture_recognizer.task"
)


@dataclass
class RecognizedHandGesture:
    handedness: str
    gesture: Optional[str]
    score: float


class HandPoseEstimator:
    """Thin wrapper around MediaPipe Tasks gesture recognizer."""

    def __init__(
        self,
        min_detection_confidence: float = 0.5,
        min_tracking_confidence: float = 0.5,
        gesture_model_path: Optional[str] = None,
    ) -> None:
        self._last_timestamp_ms = 0
        self._recognizer = self._create_gesture_recognizer(
            gesture_model_path or DEFAULT_GESTURE_MODEL_PATH,
            min_detection_confidence,
            min_tracking_confidence,
        )

    def _create_gesture_recognizer(
        self,
        model_path: str,
        min_detection_confidence: float,
        min_tracking_confidence: float,
    ) -> vision.GestureRecognizer:
        if not os.path.exists(model_path):
            raise FileNotFoundError(
                "Gesture recognizer model not found at %s. "
                "Download it from https://storage.googleapis.com/mediapipe-models/gesture_recognizer/gesture_recognizer/float16/1/gesture_recognizer.task "
                "and place it under Assets/backend/models/." % model_path
            )

        base_options = mp_python.BaseOptions(model_asset_path=model_path)
        options = vision.GestureRecognizerOptions(
            base_options=base_options,
            running_mode=vision.RunningMode.VIDEO,
            min_hand_detection_confidence=min_detection_confidence,
            min_tracking_confidence=min_tracking_confidence,
            num_hands=2,
        )
        return vision.GestureRecognizer.create_from_options(options)

    def get_hand_pose(self, frame, timestamp_ms=None):
        if frame is None:
            return []

        image_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=image_rgb)
        result = self._run_recognizer(mp_image, timestamp_ms)

        # If no hands → return empty list
        if result is None or not result.hand_landmarks:
            return []

        height, width = frame.shape[:2]
        hands_out = []

        for landmarks, handedness_list, gestures_list in zip(
            result.hand_landmarks, result.handedness, result.gestures
        ):
            handed = handedness_list[0].category_name.lower()
            gesture = gestures_list[0].category_name.lower() if gestures_list else None

            # convert normalized → pixel
            pts_px = [(lm.x * width, lm.y * height) for lm in landmarks]

            hands_out.append({
                "handedness": handed,
                "gesture": gesture,
                "landmarks_px": np.array(pts_px, dtype=float),
            })

        return hands_out

    def _run_recognizer(
        self,
        mp_image: mp.Image,
        timestamp_ms: Optional[int],
    ):
        if timestamp_ms is None:
            self._last_timestamp_ms += 33
            timestamp_ms = self._last_timestamp_ms
        else:
            self._last_timestamp_ms = timestamp_ms

        return self._recognizer.recognize_for_video(mp_image, timestamp_ms)

    def close(self) -> None:
        if getattr(self, "_recognizer", None) is not None:
            self._recognizer.close()
            self._recognizer = None

    def __del__(self) -> None:
        self.close()

