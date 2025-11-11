from __future__ import annotations
from typing import Optional, Tuple
import numpy as np
import pyrealsense2 as rs
import cv2


class RealSenseFrameProvider:
    """
    Provides:
      - color_image (BGR, HxWx3)
      - depth_image_m (float32, HxW, meters), aligned to color
    """

    def __init__(
        self,
        width: int = 640,
        height: int = 480,
        fps: int = 30,
        enable_filters: bool = True,
    ) -> None:
        self._width = width
        self._height = height
        self._fps = fps
        self._enable_filters = enable_filters

        # Pipeline setup
        self._pipeline = rs.pipeline()
        self._pipeline_profile: Optional[rs.pipeline_profile] = None
        self._pipeline_started = False
        config = rs.config()
        config.enable_stream(rs.stream.depth, width, height, rs.format.z16, fps)
        config.enable_stream(rs.stream.color, width, height, rs.format.bgr8, fps)
        try:
            self._pipeline_profile = self._pipeline.start(config)
        except Exception:
            self._pipeline = None
            self._pipeline_profile = None
            raise
        else:
            self._pipeline_started = True

        # Align depth to color stream
        self._align = rs.align(rs.stream.color)

        # Depth filters
        if enable_filters:
            self._dec_filter = rs.decimation_filter()     # reduces depth data
            self._spat_filter = rs.spatial_filter()       # edge-preserving smoothing
            self._temp_filter = rs.temporal_filter()      # temporal smoothing
            self._hf_filter = rs.hole_filling_filter()    # fills missing depth
        else:
            self._dec_filter = self._spat_filter = self._temp_filter = self._hf_filter = None

        print("RealSenseFrameProvider initialized.")

    def get_frame(self) -> Optional[Tuple[np.ndarray, np.ndarray]]:
        """
        Returns:
            color_image (BGR, uint8, HxWx3)
            depth_image_m (float32, HxW)  -- depth in meters
        """

        if self._pipeline is None or not self._pipeline_started:
            print("Frame read skipped: RealSense pipeline not started.")
            return None

        try:
            frames = self._pipeline.wait_for_frames()
        except Exception as e:
            print("Frame read error:", e)
            return None

        # Align depth to color
        frames = self._align.process(frames)
        depth_frame = frames.get_depth_frame()
        color_frame = frames.get_color_frame()

        if not depth_frame or not color_frame:
            return None

        # Apply filters (in correct order)
        if self._enable_filters:
            depth_frame = self._dec_filter.process(depth_frame)
            depth_frame = self._spat_filter.process(depth_frame)
            depth_frame = self._temp_filter.process(depth_frame)
            depth_frame = self._hf_filter.process(depth_frame)

        # Convert depth to numpy (meters)
        depth_image = np.asanyarray(depth_frame.get_data()).astype(np.float32)
        depth_scale = self._pipeline_profile.get_device().first_depth_sensor().get_depth_scale()
        depth_image_m = depth_image * depth_scale  # Convert to meters

        # Convert color to numpy (already BGR)
        color_image = np.asanyarray(color_frame.get_data())

        if depth_image_m.shape[:2] != color_image.shape[:2]:
            depth_image_m = cv2.resize(
                depth_image_m,
                (color_image.shape[1], color_image.shape[0]),
                interpolation=cv2.INTER_NEAREST,
            )

        return color_image, depth_image_m

    def get_intrinsics(self):
        """Returns RealSense intrinsics for use in later back-projection."""
        stream = self._pipeline_profile.get_stream(rs.stream.color).as_video_stream_profile()
        return stream.get_intrinsics()

    def release(self) -> None:
        if self._pipeline is not None and self._pipeline_started:
            self._pipeline.stop()

        self._pipeline = None
        self._pipeline_profile = None
        self._pipeline_started = False

    def __del__(self) -> None:
        self.release()
