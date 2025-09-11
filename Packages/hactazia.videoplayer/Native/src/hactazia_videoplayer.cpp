#ifndef VIDEOPLAYER_EXPORTS
#define VIDEOPLAYER_EXPORTS
#endif

#include "../include/hactazia_videoplayer.h"
#include <unordered_map>
#include <mutex>
#include <memory>

// Forward declaration
class VideoPlayerImpl;

// Déclaration des fonctions internes pour accéder à VideoPlayerImpl
VideoPlayerImpl *CreateVideoPlayerImpl();
void DestroyVideoPlayerImpl(VideoPlayerImpl *impl);
bool LoadVideoImpl(VideoPlayerImpl *impl, const char *url);
void PlayImpl(VideoPlayerImpl *impl);
void PauseImpl(VideoPlayerImpl *impl);
void ResumeImpl(VideoPlayerImpl *impl);
void StopImpl(VideoPlayerImpl *impl);
void SeekImpl(VideoPlayerImpl *impl, double time);
VideoFrame *GetVideoFrameAtTimeImpl(VideoPlayerImpl *impl, double time);
AudioFrame *GetAudioFrameAtTimeImpl(VideoPlayerImpl *impl, double time);
double GetDurationImpl(VideoPlayerImpl *impl);
double GetCurrentTimeImpl(VideoPlayerImpl *impl);
int GetVideoWidthImpl(VideoPlayerImpl *impl);
int GetVideoHeightImpl(VideoPlayerImpl *impl);
double GetFrameRateImpl(VideoPlayerImpl *impl);
void SetVideoFrameCallbackImpl(VideoPlayerImpl *impl, VideoFrameCallback callback);
void SetAudioFrameCallbackImpl(VideoPlayerImpl *impl, AudioFrameCallback callback);
void UpdatePlayerImpl(VideoPlayerImpl *impl);
int GetPlayerStateImpl(VideoPlayerImpl *impl);
int GetPlayerErrorImpl(VideoPlayerImpl *impl);
const char *GetPlayerErrorMessageImpl(VideoPlayerImpl *impl);

// Implémentation de l'interface C
extern "C"
{

    VIDEOPLAYER_API VideoPlayerImpl *CreateVideoPlayer()
    {
        return CreateVideoPlayerImpl();
    }

    VIDEOPLAYER_API void DestroyVideoPlayer(VideoPlayerImpl *player)
    {
        if (!player)
            return;
        DestroyVideoPlayerImpl(player);
    }

    VIDEOPLAYER_API bool LoadVideo(VideoPlayerImpl *player, const char *url)
    {
        if (!player)
            return false;
        return LoadVideoImpl(player, url);
    }

    VIDEOPLAYER_API void Play(VideoPlayerImpl *player)
    {
        if (!player)
            return;
        PlayImpl(player);
    }

    VIDEOPLAYER_API void Pause(VideoPlayerImpl *player)
    {
        if (!player)
            return;
        PauseImpl(player);
    }

    VIDEOPLAYER_API void Resume(VideoPlayerImpl *player)
    {
        if (!player)
            return;
        ResumeImpl(player);
    }

    VIDEOPLAYER_API void Stop(VideoPlayerImpl *player)
    {
        if (!player)
            return;
        StopImpl(player);
    }

    VIDEOPLAYER_API void Seek(VideoPlayerImpl *player, double time)
    {
        if (!player)
            return;
        SeekImpl(player, time);
    }

    VIDEOPLAYER_API VideoFrame *GetVideoFrameAtTime(VideoPlayerImpl *player, double time)
    {
        if (!player)
            return nullptr;
        return GetVideoFrameAtTimeImpl(player, time);
    }

    VIDEOPLAYER_API AudioFrame *GetAudioFrameAtTime(VideoPlayerImpl *player, double time)
    {
        if (!player)
            return nullptr;
        return GetAudioFrameAtTimeImpl(player, time);
    }

    VIDEOPLAYER_API void FreeVideoFrame(VideoFrame *frame)
    {
        if (!frame)
            return;

        if (frame->data)
            delete[] frame->data;
        delete frame;
    }

    VIDEOPLAYER_API void FreeAudioFrame(AudioFrame *frame)
    {
        if (!frame)
            return;

        if (frame->data)
            delete[] frame->data;
        delete frame;
    }

    VIDEOPLAYER_API double GetDuration(VideoPlayerImpl *player)
    {
        if (!player)
            return 0.0;
        return GetDurationImpl(player);
    }

    VIDEOPLAYER_API double GetCurrentTime(VideoPlayerImpl *player)
    {
        if (!player)
            return 0.0;
        return GetCurrentTimeImpl(player);
    }

    VIDEOPLAYER_API int GetVideoWidth(VideoPlayerImpl *player)
    {
        if (player)
            return GetVideoWidthImpl(player);
        return 0;
    }

    VIDEOPLAYER_API int GetVideoHeight(VideoPlayerImpl *player)
    {
        if (!player)
            return 0;
        return GetVideoHeightImpl(player);
    }

    VIDEOPLAYER_API double GetFrameRate(VideoPlayerImpl *player)
    {
        if (!player)
            return 0.0;
        return GetFrameRateImpl(player);
    }

    VIDEOPLAYER_API void SetVideoFrameCallback(VideoPlayerImpl *player, VideoFrameCallback callback)
    {
        if (!player)
            return;
        SetVideoFrameCallbackImpl(player, callback);
    }

    VIDEOPLAYER_API void SetAudioFrameCallback(VideoPlayerImpl *player, AudioFrameCallback callback)
    {
        if (!player)
            return;
        SetAudioFrameCallbackImpl(player, callback);
    }

    VIDEOPLAYER_API void UpdatePlayer(VideoPlayerImpl *player)
    {
        if (!player)
            return;
        UpdatePlayerImpl(player);
    }

    VIDEOPLAYER_API int GetPlayerState(VideoPlayerImpl *player)
    {
        if (!player)
            return static_cast<int>(PlayerState::UNINITIALIZED);
        return GetPlayerStateImpl(player);
    }

    VIDEOPLAYER_API int GetPlayerError(VideoPlayerImpl *player)
    {
        if (!player)
            return static_cast<int>(PlayerError::NONE);
        return GetPlayerErrorImpl(player);
    }

    VIDEOPLAYER_API const char *GetPlayerErrorMessage(VideoPlayerImpl *player)
    {
        if (!player)
            return "Player not found";
        return GetPlayerErrorMessageImpl(player);
    }

} // extern "C"
