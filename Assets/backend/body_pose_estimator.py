"""Body pose estimation with RealSense world-point reconstruction."""

from __future__ import annotations
from dataclasses import dataclass
from typing import Optional
import numpy as np
import cv2
import mediapipe as mp


@dataclass
class BodyPoseResult:
    landmarks_px: Optional[np.ndarray]      # (33,2) pixel coordinates
    landmarks_world: Optional[np.ndarray]   # (33,3) world coordinates in meters
    visibility: Optional[np.ndarray]        # (33,) visibility confidence


class BodyPoseEstimator:
    """MediaPipe Pose + optional real 3D world coordinate reconstruction."""

    def __init__(
        self,
        intrinsics,                           # RealSense intrinsics object
        min_detection_confidence: float = 0.5,
        min_tracking_confidence: float = 0.5,
        model_complexity: int = 1,
    ) -> None:

        self.fx = intrinsics.fx
        self.fy = intrinsics.fy
        self.cx = intrinsics.ppx
        self.cy = intrinsics.ppy

        self._pose = mp.solutions.pose.Pose(
            min_detection_confidence=min_detection_confidence,
            min_tracking_confidence=min_tracking_confidence,
            model_complexity=model_complexity,
        )

    def get_body_pose(self, frame_bgr, depth_m=None) -> BodyPoseResult:
        """
        frame_bgr: BGR image (HxWx3)
        depth_m: aligned depth image (HxW) in meters, optional

        Returns BodyPoseResult with:
            - landmarks_px (always if detected)
            - landmarks_world (only if depth provided)
        """

        if frame_bgr is None:
            return BodyPoseResult(None, None, None)

        h, w = frame_bgr.shape[:2]

        # MediaPipe processing
        rgb = cv2.cvtColor(frame_bgr, cv2.COLOR_BGR2RGB)
        rgb.flags.writeable = False
        result = self._pose.process(rgb)

        if not result.pose_landmarks:
            return BodyPoseResult(None, None, None)

        # 2D pixel coordinates
        pts_px = np.array(
            [(lm.x * w, lm.y * h) for lm in result.pose_landmarks.landmark],
            dtype=float
        )

        vis = np.array([lm.visibility for lm in result.pose_landmarks.landmark], dtype=float)

        # If no depth → stop here
        if depth_m is None:
            return BodyPoseResult(pts_px, None, vis)

        # Compute world coordinates
        pts_world = self._reconstruct_world_points(pts_px, depth_m)

        return BodyPoseResult(pts_px, pts_world, vis)

    def _reconstruct_world_points(self, pts_px, depth_m):
        points = []
        h, w = depth_m.shape

        for (u, v) in pts_px:
            u_i = int(round(u))
            v_i = int(round(v))

            # Out of image bounds → NaN
            if u_i < 0 or v_i < 0 or u_i >= w or v_i >= h:
                points.append((np.nan, np.nan, np.nan))
                continue

            z = depth_m[v_i, u_i]

            # If invalid depth → fallback to 3x3 median
            if not np.isfinite(z) or z <= 0:
                patch = depth_m[max(v_i-1, 0):min(v_i+2, h), max(u_i-1, 0):min(u_i+2, w)]
                good = patch[(patch > 0) & np.isfinite(patch)]
                if len(good) == 0:
                    points.append((np.nan, np.nan, np.nan))
                    continue
                z = float(np.median(good))

            # Back-project pixel → camera space
            X = (u - self.cx) * z / self.fx
            Y = (v - self.cy) * z / self.fy
            Y = -Y  # convert +Y up
            points.append((X, Y, z))

        return np.array(points, dtype=float)

    def close(self):
        if getattr(self, "_pose", None):
            self._pose.close()
            self._pose = None

    def __del__(self):
        self.close()
