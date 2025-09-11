#define VIDEOPLAYER_EXPORTS
#include "../include/hactazia_videoplayer.h"
#include <unordered_map>
#include <mutex>
#include <memory>

// Forward declaration
class VideoPlayerImpl;

// Gestionnaire global des instances
static std::unordered_map<int, std::unique_ptr<VideoPlayerImpl>> g_players;
static int g_nextPlayerId = 1;
static std::mutex g_playersMutex;

// Déclaration des fonctions pour accéder à VideoPlayerImpl
extern "C" {
    VideoPlayerImpl* CreateVideoPlayerImpl();
    void DestroyVideoPlayerImpl(VideoPlayerImpl* impl);
    bool LoadVideoImpl(VideoPlayerImpl* impl, const char* url);
    void PlayImpl(VideoPlayerImpl* impl);
    void PauseImpl(VideoPlayerImpl* impl);
    void ResumeImpl(VideoPlayerImpl* impl);
    void StopImpl(VideoPlayerImpl* impl);
    void SeekImpl(VideoPlayerImpl* impl, double time);
    VideoFrame* GetVideoFrameAtTimeImpl(VideoPlayerImpl* impl, double time);
    AudioFrame* GetAudioFrameAtTimeImpl(VideoPlayerImpl* impl, double time);
    double GetDurationImpl(VideoPlayerImpl* impl);
    double GetCurrentTimeImpl(VideoPlayerImpl* impl);
    int GetVideoWidthImpl(VideoPlayerImpl* impl);
    int GetVideoHeightImpl(VideoPlayerImpl* impl);
    double GetFrameRateImpl(VideoPlayerImpl* impl);
    void SetVideoFrameCallbackImpl(VideoPlayerImpl* impl, VideoFrameCallback callback);
    void SetAudioFrameCallbackImpl(VideoPlayerImpl* impl, AudioFrameCallback callback);
    void UpdatePlayerImpl(VideoPlayerImpl* impl);
    int GetPlayerStateImpl(VideoPlayerImpl* impl);
    int GetPlayerErrorImpl(VideoPlayerImpl* impl);
    const char* GetPlayerErrorMessageImpl(VideoPlayerImpl* impl);
}

// Implémentation de l'interface C
extern "C" {

VIDEOPLAYER_API int CreateVideoPlayer() {
    std::lock_guard<std::mutex> lock(g_playersMutex);
    int playerId = g_nextPlayerId++;
    g_players[playerId] = std::unique_ptr<VideoPlayerImpl>(CreateVideoPlayerImpl());
    return playerId;
}

VIDEOPLAYER_API void DestroyVideoPlayer(int playerId) {
    std::lock_guard<std::mutex> lock(g_playersMutex);
    auto it = g_players.find(playerId);
    if (it != g_players.end()) {
        DestroyVideoPlayerImpl(it->second.get());
        g_players.erase(it);
    }
}

VIDEOPLAYER_API bool LoadVideo(int playerId, const char* url) {
    std::lock_guard<std::mutex> lock(g_playersMutex);
    auto it = g_players.find(playerId);
    if (it != g_players.end()) {
        return LoadVideoImpl(it->second.get(), url);
    }
    return false;
}

VIDEOPLAYER_API void Play(int playerId) {
    std::lock_guard<std::mutex> lock(g_playersMutex);
    auto it = g_players.find(playerId);
    if (it != g_players.end()) {
        PlayImpl(it->second.get());
    }
}

VIDEOPLAYER_API void Pause(int playerId) {
    std::lock_guard<std::mutex> lock(g_playersMutex);
    auto it = g_players.find(playerId);
    if (it != g_players.end()) {
        PauseImpl(it->second.get());
    }
}

VIDEOPLAYER_API void Resume(int playerId) {
    std::lock_guard<std::mutex> lock(g_playersMutex);
    auto it = g_players.find(playerId);
    if (it != g_players.end()) {
        ResumeImpl(it->second.get());
    }
}

VIDEOPLAYER_API void Stop(int playerId) {
    std::lock_guard<std::mutex> lock(g_playersMutex);
    auto it = g_players.find(playerId);
    if (it != g_players.end()) {
        StopImpl(it->second.get());
    }
}

VIDEOPLAYER_API void Seek(int playerId, double time) {
    std::lock_guard<std::mutex> lock(g_playersMutex);
    auto it = g_players.find(playerId);
    if (it != g_players.end()) {
        SeekImpl(it->second.get(), time);
    }
}

VIDEOPLAYER_API VideoFrame* GetVideoFrameAtTime(int playerId, double time) {
    std::lock_guard<std::mutex> lock(g_playersMutex);
    auto it = g_players.find(playerId);
    if (it != g_players.end()) {
        return GetVideoFrameAtTimeImpl(it->second.get(), time);
    }
    return nullptr;
}

VIDEOPLAYER_API AudioFrame* GetAudioFrameAtTime(int playerId, double time) {
    std::lock_guard<std::mutex> lock(g_playersMutex);
    auto it = g_players.find(playerId);
    if (it != g_players.end()) {
        return GetAudioFrameAtTimeImpl(it->second.get(), time);
    }
    return nullptr;
}

VIDEOPLAYER_API void FreeVideoFrame(VideoFrame* frame) {
    if (frame) {
        if (frame->data) {
            delete[] frame->data;
        }
        delete frame;
    }
}

VIDEOPLAYER_API void FreeAudioFrame(AudioFrame* frame) {
    if (frame) {
        if (frame->data) {
            delete[] frame->data;
        }
        delete frame;
    }
}

VIDEOPLAYER_API double GetDuration(int playerId) {
    std::lock_guard<std::mutex> lock(g_playersMutex);
    auto it = g_players.find(playerId);
    if (it != g_players.end()) {
        return GetDurationImpl(it->second.get());
    }
    return 0.0;
}

VIDEOPLAYER_API double GetCurrentTime(int playerId) {
    std::lock_guard<std::mutex> lock(g_playersMutex);
    auto it = g_players.find(playerId);
    if (it != g_players.end()) {
        return GetCurrentTimeImpl(it->second.get());
    }
    return 0.0;
}

VIDEOPLAYER_API int GetVideoWidth(int playerId) {
    std::lock_guard<std::mutex> lock(g_playersMutex);
    auto it = g_players.find(playerId);
    if (it != g_players.end()) {
        return GetVideoWidthImpl(it->second.get());
    }
    return 0;
}

VIDEOPLAYER_API int GetVideoHeight(int playerId) {
    std::lock_guard<std::mutex> lock(g_playersMutex);
    auto it = g_players.find(playerId);
    if (it != g_players.end()) {
        return GetVideoHeightImpl(it->second.get());
    }
    return 0;
}

VIDEOPLAYER_API double GetFrameRate(int playerId) {
    std::lock_guard<std::mutex> lock(g_playersMutex);
    auto it = g_players.find(playerId);
    if (it != g_players.end()) {
        return GetFrameRateImpl(it->second.get());
    }
    return 0.0;
}

VIDEOPLAYER_API void SetVideoFrameCallback(int playerId, VideoFrameCallback callback) {
    std::lock_guard<std::mutex> lock(g_playersMutex);
    auto it = g_players.find(playerId);
    if (it != g_players.end()) {
        SetVideoFrameCallbackImpl(it->second.get(), callback);
    }
}

VIDEOPLAYER_API void SetAudioFrameCallback(int playerId, AudioFrameCallback callback) {
    std::lock_guard<std::mutex> lock(g_playersMutex);
    auto it = g_players.find(playerId);
    if (it != g_players.end()) {
        SetAudioFrameCallbackImpl(it->second.get(), callback);
    }
}

VIDEOPLAYER_API void UpdatePlayer(int playerId) {
    std::lock_guard<std::mutex> lock(g_playersMutex);
    auto it = g_players.find(playerId);
    if (it != g_players.end()) {
        UpdatePlayerImpl(it->second.get());
    }
}

VIDEOPLAYER_API int GetPlayerState(int playerId) {
    std::lock_guard<std::mutex> lock(g_playersMutex);
    auto it = g_players.find(playerId);
    if (it != g_players.end()) {
        return GetPlayerStateImpl(it->second.get());
    }
    return static_cast<int>(PlayerState::UNINITIALIZED);
}

VIDEOPLAYER_API int GetPlayerError(int playerId) {
    std::lock_guard<std::mutex> lock(g_playersMutex);
    auto it = g_players.find(playerId);
    if (it != g_players.end()) {
        return GetPlayerErrorImpl(it->second.get());
    }
    return static_cast<int>(PlayerError::NONE);
}

VIDEOPLAYER_API const char* GetPlayerErrorMessage(int playerId) {
    std::lock_guard<std::mutex> lock(g_playersMutex);
    auto it = g_players.find(playerId);
    if (it != g_players.end()) {
        return GetPlayerErrorMessageImpl(it->second.get());
    }
    return "Player not found";
}

} // extern "C"
