#pragma once

#include "VideoPlayer.h"

#ifdef __cplusplus
extern "C" {
#endif

// API Functions for the Video Player
VIDEOPLAYER_API int CreateVideoPlayer();
VIDEOPLAYER_API void DestroyVideoPlayer(int playerId);
VIDEOPLAYER_API bool LoadVideo(int playerId, const char* url);
VIDEOPLAYER_API void Play(int playerId);
VIDEOPLAYER_API void Pause(int playerId);
VIDEOPLAYER_API void Resume(int playerId);
VIDEOPLAYER_API void Stop(int playerId);
VIDEOPLAYER_API void Seek(int playerId, double time);
VIDEOPLAYER_API VideoFrame* GetVideoFrameAtTime(int playerId, double time);
VIDEOPLAYER_API AudioFrame* GetAudioFrameAtTime(int playerId, double time);
VIDEOPLAYER_API void FreeVideoFrame(VideoFrame* frame);
VIDEOPLAYER_API void FreeAudioFrame(AudioFrame* frame);
VIDEOPLAYER_API double GetDuration(int playerId);
VIDEOPLAYER_API double GetCurrentTime(int playerId);
VIDEOPLAYER_API int GetVideoWidth(int playerId);
VIDEOPLAYER_API int GetVideoHeight(int playerId);
VIDEOPLAYER_API double GetFrameRate(int playerId);
VIDEOPLAYER_API void SetVideoFrameCallback(int playerId, VideoFrameCallback callback);
VIDEOPLAYER_API void SetAudioFrameCallback(int playerId, AudioFrameCallback callback);
VIDEOPLAYER_API void UpdatePlayer(int playerId);
VIDEOPLAYER_API int GetPlayerState(int playerId);
VIDEOPLAYER_API int GetPlayerError(int playerId);
VIDEOPLAYER_API const char* GetPlayerErrorMessage(int playerId);

#ifdef __cplusplus
}
#endif
