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
#include <memory>

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

// Interface C pour Unity
extern "C" {
    // Gestion du player
    VIDEOPLAYER_API int CreateVideoPlayer();
    VIDEOPLAYER_API void DestroyVideoPlayer(int playerId);
    
    // Contrôle de lecture
    VIDEOPLAYER_API bool LoadVideo(int playerId, const char* url);
    VIDEOPLAYER_API void Play(int playerId);
    VIDEOPLAYER_API void Pause(int playerId);
    VIDEOPLAYER_API void Resume(int playerId);
    VIDEOPLAYER_API void Stop(int playerId);
    VIDEOPLAYER_API void Seek(int playerId, double time);
    
    // Récupération de frames par timestamp
    VIDEOPLAYER_API VideoFrame* GetVideoFrameAtTime(int playerId, double time);
    VIDEOPLAYER_API AudioFrame* GetAudioFrameAtTime(int playerId, double time);
    
    // Libération mémoire
    VIDEOPLAYER_API void FreeVideoFrame(VideoFrame* frame);
    VIDEOPLAYER_API void FreeAudioFrame(AudioFrame* frame);
    
    // Informations vidéo
    VIDEOPLAYER_API double GetDuration(int playerId);
    VIDEOPLAYER_API double GetCurrentTime(int playerId);
    VIDEOPLAYER_API int GetVideoWidth(int playerId);
    VIDEOPLAYER_API int GetVideoHeight(int playerId);
    VIDEOPLAYER_API double GetFrameRate(int playerId);
    
    // État du player
    VIDEOPLAYER_API int GetPlayerState(int playerId);
    VIDEOPLAYER_API int GetPlayerError(int playerId);
    VIDEOPLAYER_API const char* GetPlayerErrorMessage(int playerId);
    
    // Callbacks (optionnel)
    VIDEOPLAYER_API void SetVideoFrameCallback(int playerId, VideoFrameCallback callback);
    VIDEOPLAYER_API void SetAudioFrameCallback(int playerId, AudioFrameCallback callback);
    
    // Mise à jour (à appeler depuis Unity Update)
    VIDEOPLAYER_API void UpdatePlayer(int playerId);
}
