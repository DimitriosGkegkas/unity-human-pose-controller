from realsense_frame_provider import RealSenseFrameProvider
from hand_pose_estimator import HandPoseEstimator
from hand_motion_analyzer import HandMotionAnalyzer
import cv2

provider = RealSenseFrameProvider()
intr = provider.get_intrinsics()

hand_pose = HandPoseEstimator()
motion = HandMotionAnalyzer(intrinsics=intr)

while True:
    color, depth_m = provider.get_frame()

    hand_result = hand_pose.get_hand_pose(color)
    states = motion.analyze(hand_result, depth_m)

    cv2.imshow("color", color)
    cv2.imshow("depth (m)", depth_m)  # visualize scaled

    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

    for s in states:
        print(s.handedness, s.direction, s.position, s.gesture)
