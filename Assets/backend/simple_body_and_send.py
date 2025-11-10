from frame_provider import FrameProvider
from body_pose_estimator import BodyPoseEstimator
from hand_pose_estimator import HandPoseEstimator
from gesture_calculator import BodyGestureRecognizer, PoseCalculator
from pose_formatter import PoseFormatter
from pose_sender import PoseSender
from pose_visualizer import PoseVisualizer


def main() -> None:
    frame_provider = FrameProvider()
    body_pose = BodyPoseEstimator()
    hand_pose = HandPoseEstimator()
    calculator = PoseCalculator()
    gesture_recognizer = BodyGestureRecognizer()
    formatter = PoseFormatter()
    sender = PoseSender()
    visualizer = PoseVisualizer()

    try:
        while True:
            frame = frame_provider.get_frame()
            if frame is None:
                continue

            body_result = body_pose.get_body_pose(frame)
            hand_result = hand_pose.get_hand_pose(frame)
            metrics = calculator.compute(body_result, hand_result)
            body_gesture = gesture_recognizer.get_body_gesture(body_result)
            payload = formatter.format(frame.shape, body_result, hand_result, metrics, body_gesture)

            sender.send(payload)

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


