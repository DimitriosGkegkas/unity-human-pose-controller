from __future__ import annotations

from pathlib import Path
from typing import Iterable, Tuple

import cv2
import matplotlib.pyplot as plt
from matplotlib.patches import Circle
from mpl_toolkits.mplot3d import Axes3D  # noqa: F401

from body_pose_estimator import BodyPoseEstimator
from hand_pose_estimator import HandPoseEstimator
from gesture_calculator import BodyGestureRecognizer, PoseCalculator
from arm_rotation_calculator import ArmRotationCalculator
from pose_visualizer import PoseVisualizer


def main(
    image_paths: Iterable[str],
    *,
    show_visualization: bool = False,
    output_dir: str | Path | None = None,
    show_world_plot: bool = False,
) -> None:
    body_pose = BodyPoseEstimator()
    hand_pose = HandPoseEstimator()
    calculator = PoseCalculator()
    body_gesture_recognizer = BodyGestureRecognizer()
    arm_rotation_calculator = ArmRotationCalculator()
    visualizer = PoseVisualizer()

    output_directory = (
        Path(output_dir).expanduser().resolve()
        if output_dir is not None
        else Path("Assets/backend/test/output").resolve()
    )
    output_directory.mkdir(parents=True, exist_ok=True)

    try:
        for image_path in image_paths:
            resolved_path = Path(image_path).expanduser().resolve()
            if not resolved_path.exists():
                raise FileNotFoundError(f"Image not found: {resolved_path}")

            frame = cv2.imread(str(resolved_path))
            if frame is None:
                raise ValueError(f"Failed to load image: {resolved_path}")

            body_result = body_pose.get_body_pose(frame)
            hand_result = hand_pose.get_hand_pose(frame)

            metrics = calculator.compute(body_result, hand_result)
            body_gesture = body_gesture_recognizer.get_body_gesture(body_result)
            arm_segments = arm_rotation_calculator.compute(body_result)

            print("=" * 80)
            print(f"[DEBUG] Image path: {resolved_path}")
            print(f"[DEBUG] Body result: {body_result}")
            print(f"[DEBUG] Body metrics: {metrics}")
            print(f"[DEBUG] Body gesture: {body_gesture}")
            print(f"[DEBUG] Arm segments: {arm_segments}")
            _print_arm_segment_landmarks(body_result)

            annotated_frame = frame.copy()
            visualizer.draw(annotated_frame, body_result, hand_result)

            output_path = output_directory / f"{resolved_path.stem}_annotated{resolved_path.suffix}"
            cv2.imwrite(str(output_path), annotated_frame)
            print(f"[DEBUG] Saved annotated image to: {output_path}")

            if body_result.world_landmarks:
                (
                    world_plot_2d_path,
                    world_plot_3d_path,
                ) = _plot_world_landmarks(
                    body_result.world_landmarks.landmark,
                    title=f"{resolved_path.name}",
                    output_dir=output_directory,
                    show=show_world_plot,
                )
                print(f"[DEBUG] Saved 2D world landmark plot to: {world_plot_2d_path}")
                print(f"[DEBUG] Saved 3D world landmark plot to: {world_plot_3d_path}")

            if show_visualization:
                window_name = resolved_path.name
                cv2.imshow(window_name, annotated_frame)
                print("[INFO] Press any key in the image window to continue...")
                cv2.waitKey(0)
                cv2.destroyWindow(window_name)
    finally:
        body_pose.close()
        hand_pose.close()
        visualizer.close()
        if show_visualization:
            cv2.destroyAllWindows()


def _print_arm_segment_landmarks(body_result) -> None:
    world_landmarks = (
        body_result.world_landmarks.landmark
        if body_result and body_result.world_landmarks
        else None
    )
    norm_landmarks = (
        body_result.landmarks.landmark
        if body_result and body_result.landmarks
        else None
    )

    def extract_coords(landmarks, index: int):
        if landmarks is None or index < 0 or index >= len(landmarks):
            return None
        lm = landmarks[index]
        return (lm.x, lm.y, lm.z)

    def format_coords(coords):
        if coords is None:
            return "N/A"
        return f"({coords[0]:.4f}, {coords[1]:.4f}, {coords[2]:.4f})"

    print("[DEBUG] Arm segment landmark positions:")
    for config in ArmRotationCalculator.SEGMENTS:
        start_world = extract_coords(world_landmarks, config.start_landmark)
        end_world = extract_coords(world_landmarks, config.end_landmark)

        # start_coords = start_world if start_world is not None else start_norm
        # end_coords = end_world if end_world is not None else end_norm

        print(
            f"  - {config.name} ({config.start_landmark}->{config.end_landmark}): "
            f"start={format_coords(start_world)}, end={format_coords(end_world)}"
        )


def _set_equal_3d_axes(ax, xs, ys, zs) -> None:
    xs = xs if xs else [0.0]
    ys = ys if ys else [0.0]
    zs = zs if zs else [0.0]

    max_range = max(
        max(xs) - min(xs),
        max(ys) - min(ys),
        max(zs) - min(zs),
    )
    if max_range == 0:
        max_range = 1.0

    mid_x = (max(xs) + min(xs)) * 0.5
    mid_y = (max(ys) + min(ys)) * 0.5
    mid_z = (max(zs) + min(zs)) * 0.5

    half_range = max_range * 0.5
    ax.set_xlim(mid_x - half_range, mid_x + half_range)
    ax.set_ylim(mid_y - half_range, mid_y + half_range)
    ax.set_zlim(mid_z - half_range, mid_z + half_range)


def _plot_world_landmarks(
    world_landmarks,
    *,
    title: str,
    output_dir: Path,
    show: bool,
) -> Tuple[Path, Path]:
    xs = [lm.x for lm in world_landmarks]
    ys = [lm.y for lm in world_landmarks]
    zs = [lm.z for lm in world_landmarks]

    max_range = max(
        max(xs) - min(xs) if xs else 0,
        max(ys) - min(ys) if ys else 0,
        max(zs) - min(zs) if zs else 0,
    )
    radius = max_range * 0.04 if max_range > 0 else 0.01

    # Plot side by side: 2D projection and 3D scatter
    fig = plt.figure(figsize=(12, 6))
    ax2d = fig.add_subplot(1, 2, 1)
    ax3d = fig.add_subplot(1, 2, 2, projection="3d")

    # 2D subplot
    ax2d.scatter(xs, ys, c="blue", s=15, alpha=0.7)
    for idx, (x, y) in enumerate(zip(xs, ys)):
        circle = Circle((x, y), radius=radius, fill=False, edgecolor="red", linewidth=1.2)
        ax2d.add_patch(circle)
        ax2d.text(x, y, str(idx), color="red", ha="center", va="center", fontsize=8)
    ax2d.set_title(f"{title} (2D Projection)")
    ax2d.set_xlabel("X (world)")
    ax2d.set_ylabel("Y (world)")
    ax2d.set_aspect("equal", adjustable="datalim")
    ax2d.invert_yaxis()
    ax2d.grid(True, linestyle="--", alpha=0.4)

    # 3D subplot
    ax3d.scatter(xs, ys, zs, c="blue", s=20, alpha=0.8)
    for idx, (x, y, z) in enumerate(zip(xs, ys, zs)):
        ax3d.text(x, y, z, str(idx), color="red", fontsize=8, ha="center", va="center")
    ax3d.set_title(f"{title} (3D)")
    ax3d.set_xlabel("X (world)")
    ax3d.set_ylabel("Y (world)")
    ax3d.set_zlabel("Z (world)")
    _set_equal_3d_axes(ax3d, xs, ys, zs)

    fig.tight_layout()

    output_dir.mkdir(parents=True, exist_ok=True)
    combined_path = output_dir / f"{Path(title).stem}_world_plot_combined.png"
    fig.savefig(str(combined_path), bbox_inches="tight", dpi=200)

    if show:
        plt.show()
    else:
        plt.close(fig)

    # Additionally save separate 2D and 3D figures for convenience
    fig2d, ax2d_only = plt.subplots(figsize=(6, 6))
    ax2d_only.scatter(xs, ys, c="blue", s=15, alpha=0.7)
    for idx, (x, y) in enumerate(zip(xs, ys)):
        circle = Circle((x, y), radius=radius, fill=False, edgecolor="red", linewidth=1.2)
        ax2d_only.add_patch(circle)
        ax2d_only.text(x, y, str(idx), color="red", ha="center", va="center", fontsize=8)
    ax2d_only.set_title(f"{title} (2D Projection)")
    ax2d_only.set_xlabel("X (world)")
    ax2d_only.set_ylabel("Y (world)")
    ax2d_only.set_aspect("equal", adjustable="datalim")
    ax2d_only.invert_yaxis()
    ax2d_only.grid(True, linestyle="--", alpha=0.4)
    plot_2d_path = output_dir / f"{Path(title).stem}_world_plot_2d.png"
    fig2d.savefig(str(plot_2d_path), bbox_inches="tight", dpi=200)
    if not show:
        plt.close(fig2d)

    fig3d = plt.figure(figsize=(6, 6))
    ax3d_only = fig3d.add_subplot(111, projection="3d")
    ax3d_only.scatter(xs, ys, zs, c="blue", s=20, alpha=0.8)
    for idx, (x, y, z) in enumerate(zip(xs, ys, zs)):
        ax3d_only.text(x, y, z, str(idx), color="red", fontsize=8, ha="center", va="center")
    ax3d_only.set_title(f"{title} (3D)")
    ax3d_only.set_xlabel("X (world)")
    ax3d_only.set_ylabel("Y (world)")
    ax3d_only.set_zlabel("Z (world)")
    _set_equal_3d_axes(ax3d_only, xs, ys, zs)
    plot_3d_path = output_dir / f"{Path(title).stem}_world_plot_3d.png"
    fig3d.savefig(str(plot_3d_path), bbox_inches="tight", dpi=200)
    if not show:
        plt.close(fig3d)

    return plot_2d_path, plot_3d_path


if __name__ == "__main__":
    IMAGE_PATHS = [
        "/Users/dimitrisgkegkas/Work/Enneas/MediaPipeUnityPlugin/Test1/Assets/backend/test/IMG_7075.jpeg",
        "/Users/dimitrisgkegkas/Work/Enneas/MediaPipeUnityPlugin/Test1/Assets/backend/test/IMG_7076.jpeg",
        "/Users/dimitrisgkegkas/Work/Enneas/MediaPipeUnityPlugin/Test1/Assets/backend/test/IMG_7077.jpeg",
    ]
    main(
        IMAGE_PATHS,
        show_visualization=True,
        output_dir="/Users/dimitrisgkegkas/Work/Enneas/MediaPipeUnityPlugin/Test1/Assets/backend/test/output",
        show_world_plot=True,
    )


