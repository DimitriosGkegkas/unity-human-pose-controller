from realsense_frame_provider import RealSenseFrameProvider
from body_pose_estimator import BodyPoseEstimator
import cv2
from mpl_toolkits.mplot3d import Axes3D
import matplotlib.pyplot as plt

fig = plt.figure()
ax = fig.add_subplot(111, projection='3d')

def plot_3d_skeleton(pts_world):
    ax.cla()
    X, Y, Z = pts_world[:,0], pts_world[:,1], pts_world[:,2]

    ax.scatter(X, Z, Y, s=20, c='red')   # Note the axis arrangement

    ax.set_xlabel("X (m)")
    ax.set_ylabel("Z (m)")
    ax.set_ylim([-2, 2])
    ax.set_xlim([-2, 2])
    ax.set_zlim([-2, 2])
    plt.pause(0.001)


provider = RealSenseFrameProvider()
intr = provider.get_intrinsics()

body_pose = BodyPoseEstimator(intrinsics=intr)

def draw_pose_px(image, pts_px):
    for (x, y) in pts_px:
        x = int(x); y = int(y)
        cv2.circle(image, (x, y), 4, (0, 255, 0), -1)
    return image


while True:
    color, depth_m = provider.get_frame()
    result = body_pose.get_body_pose(color, depth_m)
    print(result)

    if result.landmarks_world is not None:
        print("Hip center world:", result.landmarks_world[24])

    if result.landmarks_px is not None:
        draw_pose_px(color, result.landmarks_px)

    cv2.imshow("color + landmarks", color)
    cv2.imshow("depth (m)", depth_m / 3.0)
    if result.landmarks_world is not None:
        plot_3d_skeleton(result.landmarks_world)

    if cv2.waitKey(1) & 0xFF == ord('q'):
        break


provider.release()
cv2.destroyAllWindows()