"""Video frame acquisition utilities."""

from __future__ import annotations

from typing import Any, Optional

import cv2


class FrameProvider:
    """Wrapper around OpenCV video capture with a simple API."""

    def __init__(self, camera_index: int = 0) -> None:
        self._camera_index = camera_index
        self._capture = cv2.VideoCapture(camera_index)

    def get_frame(self) -> Optional[Any]:
        """Return the next frame or None if capture fails."""
        if not self._capture or not self._capture.isOpened():
            return None
        success, frame = self._capture.read()
        if not success:
            return None
        return frame

    def release(self) -> None:
        if self._capture is not None:
            self._capture.release()
            self._capture = None

    def __del__(self) -> None:
        self.release()

