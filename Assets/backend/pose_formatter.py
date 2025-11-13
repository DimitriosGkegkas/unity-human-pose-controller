"""Utilities to convert pose outputs to serialized formats."""

from __future__ import annotations

from typing import List, Optional

from arm_rotation_calculator import ArmSegmentRotation
from hand_motion_analyzer import HandState


class PoseFormatter:
    """Formats pose data into a string payload."""

    def format(
        self,
        frame_shape,
        body_result,
        hand_result,
        body_gesture: str,
        arm_segments: Optional[List[ArmSegmentRotation]] = None,
        hand_states: Optional[List[HandState]] = None,
    ) -> str:
        body_section = self._format_body(frame_shape, body_result)
        hand_state_section = self._format_hand_states(hand_states)
        gesture_section = f"gesture:{body_gesture}"
        arm_section = self._format_arm_segments(arm_segments)

        sections = [
            part
            for part in [
                body_section,
                hand_state_section,
                gesture_section,
                arm_section,
            ]
            if part
        ]
        return "|".join(sections)

    def _format_body(self, frame_shape, body_result) -> str:
        if not body_result:
            return ""

        height, width = frame_shape[:2]

        landmarks_world = getattr(body_result, "landmarks_world", None)
        if landmarks_world is not None and landmarks_world.size:
            serialized = ";".join(
                f"{idx}:{landmarks_world[idx,0]:.5f},{landmarks_world[idx,1]:.5f},{landmarks_world[idx,2]:.5f}"
                for idx, landmark in enumerate(body_result.landmarks_world)
            )
            return f"body_world:{serialized}"

        return ""

    def _format_hand_states(self, hand_states: Optional[List[HandState]]) -> str:
        if not hand_states:
            return ""

        payload: List[str] = []
        for state in hand_states:
            x, y, z = state.position
            gesture = state.gesture or "none"
            parts = [
                f"x={x:.1f}",
                f"y={y:.1f}",
                f"dir={state.direction}",
                f"gesture={gesture}",
            ]
            if state.palm_normal is not None:
                nx, ny, nz = state.palm_normal
                parts.extend(
                    [
                        f"nx={nx:.3f}",
                        f"ny={ny:.3f}",
                        f"nz={nz:.3f}",
                    ]
                )

            payload.append(f"{state.handedness}:" + ",".join(parts))

        return "hand_states:" + "|".join(payload)

    def _format_arm_segments(
        self, arm_segments: Optional[List[ArmSegmentRotation]]
    ) -> str:
        if not arm_segments:
            return ""

        segments_payload: List[str] = []
        for segment in arm_segments:
            direction = segment.direction
            segment_payload = (
                f"{segment.config.name}:"
                f"dir={direction[0]:.5f},{direction[1]:.5f},{direction[2]:.5f}"
            )
            segments_payload.append(segment_payload)

        return "arm_segments:" + "|".join(segments_payload)

