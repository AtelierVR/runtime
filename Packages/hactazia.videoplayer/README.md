# Hactazia FFmpeg Video Player

A Unity runtime video player built on top of `FFmpeg.AutoGen`. The player decodes video, audio, and (optionally) subtitle streams at runtime and drives Unity components for rendering.

## Features

- Background demuxing/decoding using FFmpeg.
- Video frames uploaded to a `Texture2D`/`RenderTexture` for use on meshes or UI materials.
- Audio resampling into Unity's output configuration with gap-resistant buffering.
- Subtitle event stream (ready for future subtitle decoding).
- `Player` MonoBehaviour orchestrating lifecycle, playback state, looping, and Unity events.

## Package structure

| Path | Description |
| ---- | ----------- |
| `Scripts/Core` | Core FFmpeg wrappers (`FFmpegCtx`, `VideoStreamDecoder`, `FFTimings`, conversion helpers). |
| `Scripts/Components/VideoOutput.cs` | Applies decoded frames to a texture and notifies listeners when a new frame is ready. |
| `Scripts/Components/VideoRenderTextureOutput.cs` | Mirrors `VideoOutput` frames into a `RenderTexture` and optional `RawImage` for UI/render target workflows. |
| `Scripts/Components/AudioOutput.cs` | Converts decoded audio into PCM buffers on a background thread and dispatches them on the main thread for Unity playback. |
| `Scripts/Components/AudioSourceOutput.cs` | Streams queued PCM into a Unity `AudioSource` with configurable buffering for gapless playback. |
| `Scripts/Player.cs` | High-level orchestrator that mirrors FFPlay-style behaviour (play/pause/seek/loop). |
| `Plugins` | FFmpeg native binaries needed at runtime. |

## Getting started

1. Add the `Player` component to a GameObject.
2. Add child GameObjects containing `VideoOutput`, `AudioOutput`, and (optionally) `AudioSourceOutput` components. Reference them in the inspector or let `Player` auto-discover them.
	- Attach `VideoRenderTextureOutput` alongside `VideoOutput` when you need the decoded frames available as a `RenderTexture` or on a UI `RawImage`.
3. Assign a video URL (local path or network stream) to `Player.url`, or call `Player.Play(videoUrl, audioUrl)` to use different sources for audio/video.
4. Register to `OnMediaReady`, `OnEndReached`, `OnVideoEndReached`, `OnAudioEndReached`, or `OnError` events as needed.
5. Use `Player.Play()`, `Pause()`, `Resume()`, `Stop()`, or `Seek(seconds)` from scripts or UnityEvents.

### Looping and events

- Toggle `Player.loop` or call `Player.SetLoop(true)` for continuous playback.
- Hook into the exposed events (`OnMediaReady`, `OnPlayStatusChanged`, `OnEndReached`, etc.) for gameplay logic.

### Audio configuration

The audio pipeline resamples into Unity's output sample rate and channel layout. Adjust `AudioOutput`'s settings or the buffering sliders on `AudioSourceOutput` to balance latency versus resilience on slow devices. All audio buffers are marshalled back to the main thread before `AudioSourceOutput` streams them via Unity's audio thread, mirroring the texture hand-off used by `VideoOutput`.

## Dependencies

- Unity (tested with 2022 LTS tooling).
- `FFmpeg.AutoGen` (shipped with the package) plus FFmpeg shared libraries in `Plugins/`.

## Testing

Automated tests were not executed because the runtime player depends on Unity-specific APIs and FFmpeg libraries that require the Unity Editor/Player for validation. Manual verification can be performed by entering Play Mode and loading sample media through the `Player` component.
