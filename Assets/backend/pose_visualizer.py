"""Drawing helpers for pose visualization."""

from __future__ import annotations

import cv2
import mediapipe as mp


class PoseVisualizer:
    """Draws body and hand landmarks on frames."""

    def __init__(self, window_name: str = "Pose Estimation") -> None:
        self._window_name = window_name
        self._drawing_utils = mp.solutions.drawing_utils
        self._drawing_styles = mp.solutions.drawing_styles

    def draw(self, frame, body_result, hand_result) -> None:
        if frame is None:
            return

        if body_result and body_result.landmarks:
            self._drawing_utils.draw_landmarks(
                frame,
                body_result.landmarks,
                mp.solutions.pose.POSE_CONNECTIONS,
                landmark_drawing_spec=self._drawing_styles.get_default_pose_landmarks_style(),
            )

            height, width = frame.shape[:2]
            for idx, landmark in enumerate(body_result.landmarks.landmark):
                x = int(landmark.x * width)
                y = int(landmark.y * height)
                cv2.putText(
                    frame,
                    str(idx),
                    (x, y - 10),
                    cv2.FONT_HERSHEY_SIMPLEX,
                    0.4,
                    (0, 255, 0),
                    1,
                )

    def show(self, frame) -> bool:
        """Display frame and return whether the loop should continue."""
        if frame is None:
            return True

        cv2.imshow(self._window_name, frame)
        return cv2.waitKey(1) & 0xFF != ord("q")

    def close(self) -> None:
        cv2.destroyAllWindows()

