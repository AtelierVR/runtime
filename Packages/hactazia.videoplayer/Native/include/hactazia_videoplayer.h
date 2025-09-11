#pragma once

#include "VideoPlayer.h"

// Forward declaration
class VideoPlayerImpl;

#ifdef __cplusplus
extern "C" {
#endif

// API Functions for the Video Player
VIDEOPLAYER_API VideoPlayerImpl* CreateVideoPlayer();
VIDEOPLAYER_API void DestroyVideoPlayer(VideoPlayerImpl* player);
VIDEOPLAYER_API bool LoadVideo(VideoPlayerImpl* player, const char* url);
VIDEOPLAYER_API void Play(VideoPlayerImpl* player);
VIDEOPLAYER_API void Pause(VideoPlayerImpl* player);
VIDEOPLAYER_API void Resume(VideoPlayerImpl* player);
VIDEOPLAYER_API void Stop(VideoPlayerImpl* player);
VIDEOPLAYER_API void Seek(VideoPlayerImpl* player, double time);
VIDEOPLAYER_API VideoFrame* GetVideoFrameAtTime(VideoPlayerImpl* player, double time);
VIDEOPLAYER_API AudioFrame* GetAudioFrameAtTime(VideoPlayerImpl* player, double time);
VIDEOPLAYER_API void FreeVideoFrame(VideoFrame* frame);
VIDEOPLAYER_API void FreeAudioFrame(AudioFrame* frame);
VIDEOPLAYER_API double GetDuration(VideoPlayerImpl* player);
VIDEOPLAYER_API double GetCurrentTime(VideoPlayerImpl* player);
VIDEOPLAYER_API int GetVideoWidth(VideoPlayerImpl* player);
VIDEOPLAYER_API int GetVideoHeight(VideoPlayerImpl* player);
VIDEOPLAYER_API double GetFrameRate(VideoPlayerImpl* player);
VIDEOPLAYER_API void SetVideoFrameCallback(VideoPlayerImpl* player, VideoFrameCallback callback);
VIDEOPLAYER_API void SetAudioFrameCallback(VideoPlayerImpl* player, AudioFrameCallback callback);
VIDEOPLAYER_API void UpdatePlayer(VideoPlayerImpl* player);
VIDEOPLAYER_API int GetPlayerState(VideoPlayerImpl* player);
VIDEOPLAYER_API int GetPlayerError(VideoPlayerImpl* player);
VIDEOPLAYER_API const char* GetPlayerErrorMessage(VideoPlayerImpl* player);

#ifdef __cplusplus
}
#endif
