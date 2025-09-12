#pragma once

#ifdef _WIN32
    #ifdef VIDEOPLAYER_EXPORTS
        #define VIDEOPLAYER_API __declspec(dllexport)
    #else
        #define VIDEOPLAYER_API __declspec(dllimport)
    #endif
#else
    #define VIDEOPLAYER_API __attribute__((visibility("default")))
#endif

#include <cstdint>

extern "C" {
#include <libavcodec/avcodec.h>
#include <libavformat/avformat.h>
#include <libavutil/avutil.h>
#include <libavutil/imgutils.h>
#include <libavutil/opt.h>
#include <libswscale/swscale.h>
#include <libswresample/swresample.h>
}

// Énumérations pour l'état du player
enum class PlayerState {
    UNINITIALIZED = 0,
    LOADED = 1,
    PLAYING = 2,
    PAUSED = 3,
    STOPPED = 4,
    ERROR = 5
};

enum class PlayerError {
    NONE = 0,
    FILE_NOT_FOUND = 1,
    INVALID_FORMAT = 2,
    CODEC_ERROR = 3,
    MEMORY_ERROR = 4,
    UNKNOWN_ERROR = 5
};

// Structures pour les données de frame
struct VideoFrame {
    uint8_t* data;
    int width;
    int height;
    double timestamp;
    bool valid;
};

struct AudioFrame {
    uint8_t* data;
    int size;
    int sampleRate;
    int channels;
    double timestamp;
    bool valid;
};

// Types de callback pour Unity
typedef void (*VideoFrameCallback)(const VideoFrame* frame);
typedef void (*AudioFrameCallback)(const AudioFrame* frame);

// Forward declaration
class VideoPlayer;

#ifdef __cplusplus
extern "C" {
#endif

// API Functions for the Video Player
VIDEOPLAYER_API VideoPlayer* CreateVideoPlayer();
VIDEOPLAYER_API void DestroyVideoPlayer(VideoPlayer* player);
VIDEOPLAYER_API bool LoadVideo(VideoPlayer* player, const char* url);
VIDEOPLAYER_API void Play(VideoPlayer* player);
VIDEOPLAYER_API void Pause(VideoPlayer* player);
VIDEOPLAYER_API void Resume(VideoPlayer* player);
VIDEOPLAYER_API void Stop(VideoPlayer* player);
VIDEOPLAYER_API void Seek(VideoPlayer* player, double time);
VIDEOPLAYER_API VideoFrame* GetVideoFrame(VideoPlayer* player);
VIDEOPLAYER_API VideoFrame* GetVideoFrameAtTime(VideoPlayer* player, double time);
VIDEOPLAYER_API AudioFrame* GetAudioFrameAtTime(VideoPlayer* player, double time);
VIDEOPLAYER_API void FreeVideoFrame(VideoFrame* frame);
VIDEOPLAYER_API void FreeAudioFrame(AudioFrame* frame);
VIDEOPLAYER_API double GetDuration(VideoPlayer* player);
VIDEOPLAYER_API double GetCurrentTime(VideoPlayer* player);
VIDEOPLAYER_API int GetVideoWidth(VideoPlayer* player);
VIDEOPLAYER_API int GetVideoHeight(VideoPlayer* player);
VIDEOPLAYER_API double GetFrameRate(VideoPlayer* player);
VIDEOPLAYER_API void SetVideoFrameCallback(VideoPlayer* player, VideoFrameCallback callback);
VIDEOPLAYER_API void SetAudioFrameCallback(VideoPlayer* player, AudioFrameCallback callback);
VIDEOPLAYER_API void UpdatePlayer(VideoPlayer* player);
VIDEOPLAYER_API int GetPlayerState(VideoPlayer* player);
VIDEOPLAYER_API int GetPlayerError(VideoPlayer* player);
VIDEOPLAYER_API const char* GetPlayerErrorMessage(VideoPlayer* player);

// Cache information functions
VIDEOPLAYER_API int GetVideoCacheSize(VideoPlayer* player);
VIDEOPLAYER_API int GetAudioCacheSize(VideoPlayer* player);
VIDEOPLAYER_API double GetLastVideoCacheTime(VideoPlayer* player);
VIDEOPLAYER_API double GetLastAudioCacheTime(VideoPlayer* player);

// FFmpeg debug information
VIDEOPLAYER_API const char* GetFFMPEGDetails(VideoPlayer* player);

#ifdef __cplusplus
}
#endif
