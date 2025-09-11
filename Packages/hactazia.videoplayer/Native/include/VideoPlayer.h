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

// Forward declaration for VideoPlayerImpl class
class VideoPlayerImpl;
