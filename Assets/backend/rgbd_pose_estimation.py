import cv2
import numpy as np
import mediapipe as mp
import pyrealsense2 as rs


# --- Depth segmentation range (in meters) ---
NEAR = 1.
FAR = 2


# --- Mediapipe models ---
mp_pose = mp.solutions.pose
mp_hands = mp.solutions.hands
mp_drawing = mp.solutions.drawing_utils

pose = mp_pose.Pose(min_detection_confidence=0.5, min_tracking_confidence=0.5)
hands = mp_hands.Hands(max_num_hands=2, min_detection_confidence=0.5, min_tracking_confidence=0.5)


# --- RealSense setup ---
pipeline = rs.pipeline()
config = rs.config()
config.enable_stream(rs.stream.depth, 640, 480, rs.format.z16, 30)
config.enable_stream(rs.stream.color, 640, 480, rs.format.bgr8, 30)
pipeline.start(config)

align = rs.align(rs.stream.color)
colorizer = rs.colorizer()  # for depth visualization


while True:
    frames = pipeline.wait_for_frames()
    aligned = align.process(frames)

    depth_frame = aligned.get_depth_frame()
    color_frame = aligned.get_color_frame()

    if not depth_frame or not color_frame:
        continue

    # Convert to numpy
    color_image = np.asanyarray(color_frame.get_data())
    depth_image = np.asanyarray(depth_frame.get_data())

    # --- Depth segmentation mask ---
    depth_meters = depth_image * 0.001  # convert mm → meters
    mask = (depth_meters >= NEAR) & (depth_meters <= FAR)

    # Expand mask to match color channels
    mask_3 = np.repeat(mask[:, :, None], 3, axis=2)

    # Apply segmentation: keep RGB only where depth is in range
    segmented_rgb = np.where(mask_3, color_image, 0)

    # Convert to RGB for mediapipe
    rgb = cv2.cvtColor(segmented_rgb, cv2.COLOR_BGR2RGB)

    # --- Pose tracking ---
    pose_results = pose.process(rgb)
    if pose_results.pose_landmarks:
        mp_drawing.draw_landmarks(
            segmented_rgb,
            pose_results.pose_landmarks,
            mp_pose.POSE_CONNECTIONS
        )

    # --- Hand tracking ---
    hand_results = hands.process(rgb)
    if hand_results.multi_hand_landmarks:
        for hand_landmarks in hand_results.multi_hand_landmarks:
            mp_drawing.draw_landmarks(
                segmented_rgb,
                hand_landmarks,
                mp_hands.HAND_CONNECTIONS
            )

    # Depth visualization
    depth_colormap = np.asanyarray(colorizer.colorize(depth_frame).get_data())

    # Rotate both images 90 degrees clockwise
    segmented_rgb_rotated = cv2.rotate(segmented_rgb, cv2.ROTATE_90_CLOCKWISE)
    depth_colormap_rotated = cv2.rotate(depth_colormap, cv2.ROTATE_90_CLOCKWISE)

    # Combine images side by side (left: segmented RGB, right: depth)
    combined = np.hstack((segmented_rgb_rotated, depth_colormap_rotated))
    
    # Display combined window
    cv2.imshow("Segmented RGB + Pose/Hands | Depth", combined)

    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

pipeline.stop()
cv2.destroyAllWindows()
