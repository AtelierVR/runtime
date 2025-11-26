using Hactazia.FFplay;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Example video player controller with UI
/// Demonstrates usage of FFplayController
/// </summary>
public class ExampleVideoPlayer : MonoBehaviour
{
    [Header("References")]
    public FFplayController player;
    public RawImage videoDisplay;
    public Slider progressSlider;
    public Text timeText;
    public Button playPauseButton;
    public Button stopButton;

    [Header("Media")]
    public string videoPath = "path/to/video.mp4";

    private bool isDraggingSlider;

    void Start()
    {
        // Setup player
        if (player == null)
        {
            player = gameObject.AddComponent<FFplayController>();
        }

        player.videoDisplay = videoDisplay;
        player.autoPlay = false;
        player.loop = false;
        player.syncType = AVSyncType.AudioMaster;

        // Setup UI
        if (playPauseButton != null)
            playPauseButton.onClick.AddListener(OnPlayPauseClicked);

        if (stopButton != null)
            stopButton.onClick.AddListener(OnStopClicked);

        if (progressSlider != null)
        {
            progressSlider.onValueChanged.AddListener(OnProgressChanged);
        }

        // Auto-load video
        if (!string.IsNullOrEmpty(videoPath))
        {
            LoadVideo(videoPath);
        }
    }

    void Update()
    {
        UpdateUI();
        HandleKeyboardInput();
    }

    private void UpdateUI()
    {
        if (player == null)
            return;

        // Update time display
        double position = player.GetPosition();
        double duration = player.GetDuration();

        if (timeText != null)
        {
            timeText.text = $"{FormatTime(position)} / {FormatTime(duration)}";
        }

        // Update progress slider (only if not dragging)
        if (progressSlider != null && !isDraggingSlider && duration > 0)
        {
            progressSlider.value = (float)(position / duration);
        }
    }

    private void HandleKeyboardInput()
    {
        // Space: Play/Pause
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnPlayPauseClicked();
        }

        // Left/Right: Seek ±5 seconds
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Seek(-5);
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            Seek(5);
        }

        // Home/End: Jump to start/end
        if (Input.GetKeyDown(KeyCode.Home))
        {
            player.Seek(0);
        }
        if (Input.GetKeyDown(KeyCode.End))
        {
            double duration = player.GetDuration();
            if (duration > 0)
                player.Seek(duration - 1);
        }
    }

    public void LoadVideo(string path)
    {
        videoPath = path;
        player.Play(path);
    }

    public void OnPlayPauseClicked()
    {
        if (player != null)
        {
            player.TogglePause();
        }
    }

    public void OnStopClicked()
    {
        if (player != null)
        {
            player.Stop();
        }
    }

    public void Seek(double deltaSeconds)
    {
        if (player != null)
        {
            double newPos = player.GetPosition() + deltaSeconds;
            double duration = player.GetDuration();
            newPos = Mathf.Clamp((float)newPos, 0, (float)duration);
            player.Seek(newPos);
        }
    }

    private void OnProgressChanged(float value)
    {
        if (!isDraggingSlider)
            return;

        if (player != null)
        {
            double duration = player.GetDuration();
            if (duration > 0)
            {
                player.Seek(value * duration);
            }
        }
    }

    public void OnProgressSliderBeginDrag()
    {
        isDraggingSlider = true;
    }

    public void OnProgressSliderEndDrag()
    {
        isDraggingSlider = false;
    }

    private string FormatTime(double seconds)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds))
            return "00:00";

        int hours = (int)(seconds / 3600);
        int minutes = (int)((seconds % 3600) / 60);
        int secs = (int)(seconds % 60);

        if (hours > 0)
            return $"{hours:D2}:{minutes:D2}:{secs:D2}";
        else
            return $"{minutes:D2}:{secs:D2}";
    }

    private void OnDestroy()
    {
        if (playPauseButton != null)
            playPauseButton.onClick.RemoveListener(OnPlayPauseClicked);

        if (stopButton != null)
            stopButton.onClick.RemoveListener(OnStopClicked);

        if (progressSlider != null)
            progressSlider.onValueChanged.RemoveListener(OnProgressChanged);
    }
}
