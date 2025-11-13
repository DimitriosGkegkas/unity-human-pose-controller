"""Drawing helpers for pose visualization."""

from __future__ import annotations

import cv2
import numpy as np
import mediapipe as mp
from mediapipe.framework.formats import landmark_pb2


class PoseVisualizer:
    """Draws body and hand landmarks on frames."""

    def __init__(
        self,
        window_name: str = "Pose Estimation",
        depth_min_percentile: float = 5.0,
        depth_max_percentile: float = 95.0,
        depth_max_meters: float = 6.5,
    ) -> None:
        self._window_name = window_name
        self._depth_window_name = f"{window_name} - Depth"
        self._depth_min_percentile = depth_min_percentile
        self._depth_max_percentile = depth_max_percentile
        self._depth_max_meters = depth_max_meters
        self._drawing_utils = mp.solutions.drawing_utils
        self._drawing_styles = mp.solutions.drawing_styles

    def draw(self, frame, body_result, hand_result) -> None:
        if frame is None:
            return

        landmarks_px = getattr(body_result, "landmarks_px", None)
        if landmarks_px is not None and landmarks_px.size:
            visibility = getattr(body_result, "visibility", None)
            landmarks_world = getattr(body_result, "landmarks_world", None)

            height, width = frame.shape[:2]
            normalized_landmarks = []

            for idx, point in enumerate(landmarks_px):
                x_px, y_px = point

                z_value = 0.0
                if landmarks_world is not None and idx < len(landmarks_world):
                    z_coord = landmarks_world[idx][2]
                    if np.isfinite(z_coord):
                        z_value = float(z_coord)

                visibility_value = 0.0
                if visibility is not None and idx < len(visibility):
                    vis = visibility[idx]
                    if np.isfinite(vis):
                        visibility_value = float(vis)

                if np.isfinite(x_px) and np.isfinite(y_px):
                    x_norm = float(np.clip(x_px / width, 0.0, 1.0))
                    y_norm = float(np.clip(y_px / height, 0.0, 1.0))
                    normalized_landmarks.append(
                        landmark_pb2.NormalizedLandmark(
                            x=x_norm,
                            y=y_norm,
                            z=z_value,
                            visibility=visibility_value,
                        )
                    )

                    cv2.putText(
                        frame,
                        str(idx),
                        (int(round(x_px)), int(round(y_px)) - 10),
                        cv2.FONT_HERSHEY_SIMPLEX,
                        0.4,
                        (0, 255, 0),
                        1,
                    )
                else:
                    normalized_landmarks.append(
                        landmark_pb2.NormalizedLandmark(
                            x=0.0,
                            y=0.0,
                            z=z_value,
                            visibility=0.0,
                        )
                    )

            if normalized_landmarks:
                self._drawing_utils.draw_landmarks(
                    frame,
                    landmark_pb2.NormalizedLandmarkList(landmark=normalized_landmarks),
                    mp.solutions.pose.POSE_CONNECTIONS,
                    landmark_drawing_spec=self._drawing_styles.get_default_pose_landmarks_style(),
                )

        if hand_result:
            height, width = frame.shape[:2]
            for hand in hand_result:
                landmarks_px = hand.get("landmarks_px")
                if landmarks_px is None:
                    continue

                landmarks_px = np.asarray(landmarks_px, dtype=float)
                if landmarks_px.size == 0:
                    continue

                normalized_landmarks = []
                for point in landmarks_px:
                    x_px, y_px = point
                    if np.isfinite(x_px) and np.isfinite(y_px):
                        normalized_landmarks.append(
                            landmark_pb2.NormalizedLandmark(
                                x=float(np.clip(x_px / width, 0.0, 1.0)),
                                y=float(np.clip(y_px / height, 0.0, 1.0)),
                                z=0.0,
                                visibility=1.0,
                            )
                        )
                    else:
                        normalized_landmarks.append(
                            landmark_pb2.NormalizedLandmark(
                                x=0.0,
                                y=0.0,
                                z=0.0,
                                visibility=0.0,
                            )
                        )

                if not normalized_landmarks:
                    continue

                self._drawing_utils.draw_landmarks(
                    frame,
                    landmark_pb2.NormalizedLandmarkList(landmark=normalized_landmarks),
                    mp.solutions.hands.HAND_CONNECTIONS,
                    landmark_drawing_spec=self._drawing_styles.get_default_hand_landmarks_style(),
                    connection_drawing_spec=self._drawing_styles.get_default_hand_connections_style(),
                )

                first_landmark = landmarks_px[0]
                handedness = hand.get("handedness")
                gesture = hand.get("gesture")
                if np.isfinite(first_landmark[0]) and np.isfinite(first_landmark[1]):
                    label = handedness or "hand"
                    if gesture:
                        label = f"{label}: {gesture}"
                    cv2.putText(
                        frame,
                        label,
                        (int(round(first_landmark[0])), int(round(first_landmark[1])) - 20),
                        cv2.FONT_HERSHEY_SIMPLEX,
                        0.5,
                        (255, 255, 0),
                        1,
                    )

    def show(self, frame, depth=None) -> bool:
        """Display frame (and depth visualization if provided)."""
        if frame is None and depth is None:
            return True

        if frame is not None:
            cv2.imshow(self._window_name, frame)

        if depth is not None:
            depth_visual = self._prepare_depth_display(depth)
            if depth_visual is not None:
                cv2.imshow(self._depth_window_name, depth_visual)

        return cv2.waitKey(1) & 0xFF != ord("q")

    def close(self) -> None:
        cv2.destroyAllWindows()

    def _prepare_depth_display(self, depth):
        if depth is None:
            return None

        depth_array = np.asarray(depth)

        if depth_array.ndim > 2:
            depth_array = depth_array[..., 0]

        if depth_array.size == 0:
            return np.zeros((1, 3), dtype=np.uint8)

        depth_array = depth_array.astype(np.float32)
        valid_mask = np.isfinite(depth_array) & (depth_array > 0)

        if not np.any(valid_mask):
            return np.zeros((*depth_array.shape, 3), dtype=np.uint8)

        valid_values = depth_array[valid_mask]
        min_val = float(np.percentile(valid_values, self._depth_min_percentile))
        max_val = float(np.percentile(valid_values, self._depth_max_percentile))

        if self._depth_max_meters is not None:
            max_val = min(max_val, self._depth_max_meters)

        if max_val - min_val < 1e-6:
            max_val = min_val + 1e-6

        normalized = np.zeros_like(depth_array, dtype=np.float32)
        clipped = np.clip(depth_array, min_val, max_val)
        normalized[valid_mask] = (clipped[valid_mask] - min_val) / (max_val - min_val)
        normalized = np.clip(normalized, 0.0, 1.0)

        depth_uint8 = (normalized * 255).astype(np.uint8)
        depth_color = cv2.applyColorMap(depth_uint8, cv2.COLORMAP_TURBO)
        depth_color[~valid_mask] = (0, 0, 0)

        return depth_color

