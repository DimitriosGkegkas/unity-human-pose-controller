## MediaPipe Unity Pose Streamer

This project combines a Unity scene with Python-based MediaPipe pose estimation. The Python side captures webcam input, extracts body landmarks, and streams them to Unity over TCP. Use it as a starting point for body-tracking interactions, experiments, or demos.

### Prerequisites
- Unity 6000.0.58f2 or newer (other versions may work, but were not tested)
- Python 3.8 – 3.10 (MediaPipe currently provides pre-built wheels up to 3.10; on Apple Silicon prefer the arm64 build)
- Git

### Project Structure
- `Assets/` – Unity assets, scripts, and the Python backend folder
- `Assets/backend/` – Python scripts for pose estimation and networking
- `Packages/` & `ProjectSettings/` – Unity configuration files

### Getting Started
1. **Clone the repository**
   ```bash
   git clone https://github.com/DimitriosGkegkas/unity-human-pose-controller.git
   cd unity-human-pose-controller
   ```
2. **Open the Unity project**
   - Launch Unity Hub and add the cloned folder.
   - Open the project; Unity will import assets (first launch can take a few minutes).


### Python Backend Setup
The Python scripts live in `Assets/backend/`. They require `mediapipe` and `opencv-python`.

Install dependencies:
```bash
pip install --upgrade pip
pip install mediapipe opencv-python numpy
```

> **macOS (Apple Silicon) note:** If the default wheels fail, install the universal2 builds via `pip install mediapipe-silicon` or use Rosetta with an x86_64 Python 3.9 environment.

### Running the Pose Streamer
1. Ensure Unity is running the scene that expects pose data (see `Assets/Scripts/PythonRunner.cs`).
2. From a terminal, execute:
   ```bash
   python Assets/backend/simple_body_and_send.py
   ```
   - Grants camera permissions when prompted (macOS will ask the first time).
   - The script opens a webcam preview window. Press `q` to stop.
   - Pose landmarks are sent over TCP to `127.0.0.1:25001`. Adjust host/port inside the script if needed.
3. Back in Unity, hit Play. The custom scripts should consume the incoming landmark data.

### Troubleshooting
- **Camera not detected**: On Windows try `cv2.VideoCapture(0, cv2.CAP_DSHOW)`; on macOS confirm camera access in System Settings → Privacy & Security → Camera.
- **Python connection refused**: Make sure Unity (or another listener) is running on port `25001`. Update both sides if you change the port.
- **Missing dependencies**: Re-run `pip install ...` inside the active virtual environment.

### Contributing
Feel free to open issues or pull requests for bug fixes, improvements, or extended tracking features.



