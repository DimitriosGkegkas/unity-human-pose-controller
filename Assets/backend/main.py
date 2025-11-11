from frame_provider import FrameProvider
from realsense_frame_provider import RealSenseFrameProvider

from body_pose_estimator import BodyPoseEstimator
from hand_pose_estimator import HandPoseEstimator
from gesture_calculator import BodyGestureRecognizer
from hand_motion_analyzer import HandMotionAnalyzer
from pose_formatter import PoseFormatter
from pose_sender import PoseSender
from pose_visualizer import PoseVisualizer
from arm_rotation_calculator import ArmRotationCalculator


def main() -> None:
    frame_provider = RealSenseFrameProvider()

    # Body pose estimation
    body_pose = BodyPoseEstimator(
        intrinsics=frame_provider.get_intrinsics()
    )
    body_gesture_recognizer = BodyGestureRecognizer()
    arm_rotation_calculator = ArmRotationCalculator()

    # Hand pose estimation
    hand_pose = HandPoseEstimator()
    hand_motion_analyzer = HandMotionAnalyzer(
        intrinsics=frame_provider.get_intrinsics()
        )

    # Formatting and sending
    formatter = PoseFormatter()
    sender = PoseSender()
    visualizer = PoseVisualizer()

    try:
        while True:
            frame_rgb, frame_depth = frame_provider.get_frame()
            if frame_rgb is None:
                continue

            body_result = body_pose.get_body_pose(frame_rgb, frame_depth)
            arm_segments = arm_rotation_calculator.compute(body_result)
            body_gesture = body_gesture_recognizer.get_body_gesture(body_result)

            hand_result = hand_pose.get_hand_pose(frame_rgb)
            hand_states = hand_motion_analyzer.analyze(hand_result, frame_depth)
            
            print(arm_segments[0])

            payload = formatter.format(
                frame_rgb.shape,
                body_result,
                hand_result,
                body_gesture,
                arm_segments,
                hand_states=hand_states,
            )
            sender.send(payload)

            visualizer.draw(frame_rgb, body_result, hand_result)
            if not visualizer.show(frame_rgb, frame_depth):
                break
    except KeyboardInterrupt:
        pass
    finally:
        visualizer.close()
        frame_provider.release()
        body_pose.close()
        hand_pose.close()
        sender.close()


if __name__ == "__main__":
    main()


