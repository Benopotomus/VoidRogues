Input Smoothing example shows how exactly the KCC input smoothing algorithm behaves under various conditions.

Controls
========================================
Mouse        - Camera Rotation
WSADQE       - Camera Movement
Shift + WSAD - Camera Movement + Rotation
F4           - Toggle input smoothing preset used for runtime Camera transform manipulation
F5           - Toggle target frame rate
F7           - Toggle V-Sync
F9           - Toggle recording
Enter        - Toggle cursor

Steps to analyze input
========================================
0) Configure Reponsivities on InputSmoothingTest game object. (Optional)
1) Play in editor or run a build.
2) Configure target frame rate, v-sync.
3) Start recording.
4) Stop recording / quit.
5) Two file types are generated in the sample folder (Editor) or in the build folder.
    A) Frame       - X axis in graph is a linear representation of Time.frameCount - file ends with (Frame).
    B) Engine Time - X axis in graph is a linear representation of Time.unscaledTime - file ends with (EngineTime).
6) Run CreateHTMLGraphs.py from Explorer - this script generates browsable graphs (html + Plotly) from *.csv and *.log files.
   Python packages required: pandas, plotly, chart_studio. This script processes only files in the same folder.
7) Open generated HTML files and check mouse delta / accumulated mouse delta.
