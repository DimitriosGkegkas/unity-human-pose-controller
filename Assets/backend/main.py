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
    body_pose = BodyPoseEstimator()
    hand_pose = HandPoseEstimator()
    body_gesture_recognizer = BodyGestureRecognizer()
    formatter = PoseFormatter()
    sender = PoseSender()
    visualizer = PoseVisualizer()

    arm_rotation_calculator = ArmRotationCalculator()
    hand_motion_analyzer = HandMotionAnalyzer()
    frame_index = 0

    try:
        while True:
            frame, depth = frame_provider.get_frame()
            if frame is None:
                continue

            body_result = body_pose.get_body_pose(frame)
            arm_segments = arm_rotation_calculator.compute(body_result)
            body_gesture = body_gesture_recognizer.get_body_gesture(body_result)

            hand_result = hand_pose.get_hand_pose(frame)
            hand_states = hand_motion_analyzer.analyze(hand_result)
            

            print(hand_states)

            payload = formatter.format(
                frame.shape,
                body_result,
                hand_result,
                body_gesture,
                arm_segments,
                hand_states=hand_states,
            )
            sender.send(payload)
            frame_index += 1

            visualizer.draw(frame, body_result, hand_result)
            if not visualizer.show(frame):
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


