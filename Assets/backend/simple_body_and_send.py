import socket
import cv2
import mediapipe as mp


def landmarks_to_string(results, image_shape):
	# Prefer world landmarks (meters, camera-centered). Fallback to 2D image landmarks.
	if results.pose_world_landmarks is not None:
		landmarks = results.pose_world_landmarks.landmark
		values = []
		for i, l in enumerate(landmarks):
			values.append(f"{i}:{l.x},{l.y},{l.z}")
		return ";".join(values)
	elif results.pose_landmarks is not None:
		# Convert image-space normalized coords to pixels; z not defined -> 0.0
		h, w = image_shape[:2]
		landmarks = results.pose_landmarks.landmark
		values = []
		for i, l in enumerate(landmarks):
			x = l.x * w
			y = l.y * h
			z = 0.0
			values.append(f"{i}:{x},{y},{z}")
		return ";".join(values)
	else:
		return ""  # No detection this frame


def main():
	# Network target
	host, port = "127.0.0.1", 25001

	# Create TCP socket
	sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
	sock.settimeout(2.0)
	try:
		sock.connect((host, port))
	except Exception:
		# If not available, continue without crashing; we'll try to send anyway
		pass

	# MediaPipe Pose
	mp_pose = mp.solutions.pose
	mp_drawing = mp.solutions.drawing_utils
	mp_drawing_styles = mp.solutions.drawing_styles
	cap = cv2.VideoCapture(0)

	with mp_pose.Pose(min_detection_confidence=0.5, min_tracking_confidence=0.5) as pose:
		try:
			while True:
				ret, frame = cap.read()
				if not ret:
					continue

				frame.flags.writeable = True
				results = pose.process(frame)

				# Draw pose landmarks on the frame
				if results.pose_landmarks:
					mp_drawing.draw_landmarks(
						frame,
						results.pose_landmarks,
						mp_pose.POSE_CONNECTIONS,
						landmark_drawing_spec=mp_drawing_styles.get_default_pose_landmarks_style()
					)
					# Draw index numbers on top of landmarks
					h, w = frame.shape[:2]
					for idx, landmark in enumerate(results.pose_landmarks.landmark):
						x = int(landmark.x * w)
						y = int(landmark.y * h)
						cv2.putText(frame, str(idx), (x, y - 10), 
									cv2.FONT_HERSHEY_SIMPLEX, 0.4, (0, 255, 0), 1)

				# Display the frame
				cv2.imshow('Pose Estimation', frame)
				if cv2.waitKey(1) & 0xFF == ord('q'):
					break

				pose_str = landmarks_to_string(results, frame.shape)
				if pose_str:
					try:
						sock.sendall(pose_str.encode("utf-8"))
					except Exception:
						# Attempt reconnect once if send fails
						try:
							sock.close()
						except Exception:
							pass
						sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
						sock.settimeout(2.0)
						try:
							sock.connect((host, port))
							sock.sendall(pose_str.encode("utf-8"))
						except Exception:
							pass
		finally:
			cap.release()
			cv2.destroyAllWindows()
			try:
				sock.close()
			except Exception:
				pass


if __name__ == "__main__":
	main()


