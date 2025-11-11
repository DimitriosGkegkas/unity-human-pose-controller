from __future__ import annotations
from collections import deque
from dataclasses import dataclass
from typing import Dict, Deque, List, Optional, Tuple
from statistics import mean
import numpy as np


HandPosition3D = Tuple[float, float, float]


@dataclass
class HandState:
    handedness: str
    position: HandPosition3D   # (X,Y,Z) in meters
    direction: str             # up/down/left/right/forward/back/none
    gesture: Optional[str] = None


class HandMotionAnalyzer:
    """
    Tracks stable 3D palm motion using:
      - MediaPipe 2D pixel landmarks
      - Depth map (meters, aligned to color)
      - RealSense intrinsics (passed once at init)

    Output is stable enough for directional gesture detection.
    """

    def __init__(
        self,
        intrinsics,
        history_size: int = 12,
        smoothing_factor: float = 0.35,
        direction_threshold_m: float = 0.01,  # 1 cm
    ):
        self._intr = intrinsics  # stored permanently
        self._history_size = history_size
        self._alpha = smoothing_factor
        self._threshold = direction_threshold_m

        self._history: Dict[str, Deque[HandPosition3D]] = {}
        self._smoothed: Dict[str, HandPosition3D] = {}

    def analyze(self, hand_result, depth_m) -> List[HandState]:
        if not hand_result:
            return []

        states = []

        for hand in hand_result:
            landmarks_px = hand["landmarks_px"]
            handedness = hand["handedness"]
            gesture = hand["gesture"]

            # 1) Palm center in pixel coordinates
            cx, cy = self._compute_palm_center(landmarks_px)

            # 2) Depth sampling (with fallback)
            z = self._sample_depth(depth_m, cx, cy)
            if not np.isfinite(z) or z <= 0:
                continue

            # 3) 3D backprojection
            X, Y, Z = self._backproject(cx, cy, z)

            pos = (X, Y, Z)

            # 4) EMA smoothing
            prev = self._smoothed.get(handedness, pos)
            sx = prev[0] + self._alpha * (X - prev[0])
            sy = prev[1] + self._alpha * (Y - prev[1])
            sz = prev[2] + self._alpha * (Z - prev[2])
            smoothed = (sx, sy, sz)
            self._smoothed[handedness] = smoothed

            # 5) History tracking
            hist = self._history.setdefault(handedness, deque(maxlen=self._history_size))
            hist.append(smoothed)

            direction = self._compute_direction(hist)

            states.append(HandState(handedness, smoothed, direction, gesture))

        return states

    def _compute_palm_center(self, landmarks_px):
        idxs = [0, 1, 5, 9, 13, 17]
        pts = landmarks_px[idxs]
        return tuple(np.mean(pts, axis=0))

    def _sample_depth(self, depth_m, u, v):
        h, w = depth_m.shape
        u = int(round(u))
        v = int(round(v))
        if u < 0 or v < 0 or u >= w or v >= h:
            return np.nan

        z = depth_m[v, u]
        if np.isfinite(z) and z > 0:
            return float(z)

        # fallback 3×3 median filter
        patch = depth_m[max(v-1,0):min(v+2,h), max(u-1,0):min(u+2,w)]
        good = patch[(patch > 0) & np.isfinite(patch)]
        return float(np.median(good)) if len(good) > 0 else np.nan

    def _backproject(self, u, v, z):
        intr = self._intr
        X = (u - intr.ppx) * z / intr.fx
        Y = (v - intr.ppy) * z / intr.fy
        Y = -Y  # convert to human coordinate (+Y up)
        return (X, Y, z)

    def _compute_direction(self, history: Deque[HandPosition3D]) -> str:
        if len(history) < 2:
            return "none"

        current = history[-1]
        prev_avg = np.mean(history, axis=0)

        dx = current[0] - prev_avg[0]
        dy = current[1] - prev_avg[1]
        dz = current[2] - prev_avg[2]

        if abs(dx) < self._threshold and abs(dy) < self._threshold and abs(dz) < self._threshold:
            return "none"

        direction = ""

        if abs(dx) > self._threshold :
            direction += ("left" if dx > 0 else "right")
        if abs(dy) > self._threshold:
            direction += ("up" if dy > 0 else "down")
        if abs(dz) > self._threshold:
            direction += ("forward" if dz < 0 else "back")

        return direction
