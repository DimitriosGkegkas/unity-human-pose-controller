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
        metrics,
        body_gesture: str,
        arm_segments: Optional[List[ArmSegmentRotation]] = None,
        hand_states: Optional[List[HandState]] = None,
    ) -> str:
        body_section = self._format_body(frame_shape, body_result)
        hand_section = self._format_hands(frame_shape, hand_result)
        hand_state_section = self._format_hand_states(hand_states)
        metrics_section = f"metrics:body={metrics.body_landmark_count},hands={metrics.hand_landmark_count}"
        gesture_section = f"gesture:{body_gesture}"
        arm_section = self._format_arm_segments(arm_segments)

        sections = [
            part
            for part in [
                body_section,
                hand_section,
                hand_state_section,
                metrics_section,
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

        if body_result.world_landmarks:
            serialized = ";".join(
                f"{idx}:{landmark.x:.5f},{landmark.y:.5f},{landmark.z:.5f}"
                for idx, landmark in enumerate(body_result.world_landmarks.landmark)
            )
            return f"body_world:{serialized}"

        if body_result.landmarks:
            serialized = ";".join(
                f"{idx}:{landmark.x * width:.1f},{landmark.y * height:.1f},{0.0:.1f}"
                for idx, landmark in enumerate(body_result.landmarks.landmark)
            )
            return f"body_image:{serialized}"

        return ""

    def _format_hands(self, frame_shape, hand_result) -> str:
        if not hand_result or not hand_result.normalized:
            return ""

        height, width = frame_shape[:2]
        hands_payload: List[str] = []
        for hand_idx, hand_landmarks in enumerate(hand_result.normalized):
            serialized = ";".join(
                f"{idx}:{landmark.x * width:.1f},{landmark.y * height:.1f},{landmark.z:.5f}"
                for idx, landmark in enumerate(hand_landmarks.landmark)
            )
            hands_payload.append(f"hand{hand_idx}:{serialized}")

        if not hands_payload:
            return ""

        return "hands:" + "|".join(hands_payload)

    def _format_hand_states(self, hand_states: Optional[List[HandState]]) -> str:
        if not hand_states:
            return ""

        payload: List[str] = []
        for state in hand_states:
            x, y = state.position
            pointing = 1 if state.is_pointing else 0
            gesture = state.gesture or "none"
            payload.append(
                f"{state.handedness}:x={x:.1f},y={y:.1f},dir={state.direction},pointing={pointing},gesture={gesture}"
            )

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

