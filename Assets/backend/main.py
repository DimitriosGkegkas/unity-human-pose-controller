from frame_provider import FrameProvider
from body_pose_estimator import BodyPoseEstimator
from hand_pose_estimator import HandPoseEstimator
from gesture_calculator import BodyGestureRecognizer, PoseCalculator
from hand_gesture_recognizer import HandGestureRecognizer
from hand_motion_analyzer import HandMotionAnalyzer
from pose_formatter import PoseFormatter
from pose_sender import PoseSender
from pose_visualizer import PoseVisualizer
from arm_rotation_calculator import ArmRotationCalculator


def main() -> None:
    frame_provider = FrameProvider()
    body_pose = BodyPoseEstimator()
    hand_pose = HandPoseEstimator()
    calculator = PoseCalculator()
    body_gesture_recognizer = BodyGestureRecognizer()
    formatter = PoseFormatter()
    sender = PoseSender()
    visualizer = PoseVisualizer()

    arm_rotation_calculator = ArmRotationCalculator()
    hand_motion_analyzer = HandMotionAnalyzer()
    hand_gesture_recognizer = HandGestureRecognizer()

    frame_index = 0

    try:
        while True:
            frame = frame_provider.get_frame()
            if frame is None:
                continue

            body_result = body_pose.get_body_pose(frame)
            hand_result = hand_pose.get_hand_pose(frame)
            
            metrics = calculator.compute(body_result, hand_result)
            body_gesture = body_gesture_recognizer.get_body_gesture(body_result)

            arm_segments = arm_rotation_calculator.compute(body_result)

            print(arm_segments)

            gesture_recognitions = hand_gesture_recognizer.recognize(
                frame, timestamp_ms=frame_index * 33
            )
            hand_states = hand_motion_analyzer.analyze(
                frame.shape,
                hand_result,
                recognized_gestures=gesture_recognitions,
            )


            payload = formatter.format(
                frame.shape,
                body_result,
                hand_result,
                metrics,
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
        hand_gesture_recognizer.close()
        sender.close()


if __name__ == "__main__":
    main()


