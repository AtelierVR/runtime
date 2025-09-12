#ifndef VIDEOPLAYER_EXPORTS
#define VIDEOPLAYER_EXPORTS
#endif

#include "../include/hactazia_videoplayer.h"
#include "VideoPlayer.cpp"
#include <unordered_map>
#include <map>
#include <thread>
#include <mutex>
#include <condition_variable>
#include <queue>
#include <atomic>
#include <chrono>
#include <algorithm>
#include <memory>

// Implémentation de l'interface C
extern "C"
{

    VIDEOPLAYER_API VideoPlayer *CreateVideoPlayer()
    {
        return new VideoPlayer();
    }

    VIDEOPLAYER_API void DestroyVideoPlayer(VideoPlayer *player)
    {
        if (!player)
            return;
        
        try 
        {
            delete player; // appelle automatiquement ~VideoPlayer(), qui fait destroy()
        }
        catch (...)
        {
            // Prevent any exceptions from propagating to C#
            // This could happen if there are issues during destruction
        }
    }

    VIDEOPLAYER_API bool LoadVideo(VideoPlayer *player, const char *url)
    {
        if (!player)
            return false;
        return player->loadVideo(url);
    }

    VIDEOPLAYER_API void Play(VideoPlayer *player)
    {
        if (!player)
            return;
        player->play();
    }

    VIDEOPLAYER_API void Pause(VideoPlayer *player)
    {
        if (!player)
            return;
        player->pause();
    }

    VIDEOPLAYER_API void Resume(VideoPlayer *player)
    {
        if (!player)
            return;
        player->resume();
    }

    VIDEOPLAYER_API void Stop(VideoPlayer *player)
    {
        if (!player)
            return;
        player->stop();
    }

    VIDEOPLAYER_API void Seek(VideoPlayer *player, double time)
    {
        if (!player)
            return;
        player->seek(time);
    }

    VIDEOPLAYER_API VideoFrame *GetVideoFrame(VideoPlayer *player)
    {
        if (!player)
            return nullptr;
        return player->getVideoFrame();
    }

    VIDEOPLAYER_API VideoFrame *GetVideoFrameAtTime(VideoPlayer *player, double time)
    {
        if (!player)
            return nullptr;
        return player->getVideoFrameAtTime(time);
    }

    VIDEOPLAYER_API AudioFrame *GetAudioFrameAtTime(VideoPlayer *player, double time)
    {
        if (!player)
            return nullptr;
        return player->getAudioFrameAtTime(time);
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

    VIDEOPLAYER_API double GetDuration(VideoPlayer *player)
    {
        if (!player)
            return 0.0;
        return player->getDuration();
    }

    VIDEOPLAYER_API double GetCurrentTime(VideoPlayer *player)
    {
        if (!player)
            return 0.0;
        return player->getCurrentTime();
    }

    VIDEOPLAYER_API int GetVideoWidth(VideoPlayer *player)
    {
        if (!player)
            return 0;
        return player->getVideoWidth();
    }

    VIDEOPLAYER_API int GetVideoHeight(VideoPlayer *player)
    {
        if (!player)
            return 0;
        return player->getVideoHeight();
    }

    VIDEOPLAYER_API double GetFrameRate(VideoPlayer *player)
    {
        if (!player)
            return 0.0;
        return player->getFrameRate();
    }

    VIDEOPLAYER_API void SetVideoFrameCallback(VideoPlayer *player, VideoFrameCallback callback)
    {
        if (!player)
            return;
        player->setVideoFrameCallback(callback);
    }

    VIDEOPLAYER_API void SetAudioFrameCallback(VideoPlayer *player, AudioFrameCallback callback)
    {
        if (!player)
            return;
        player->setAudioFrameCallback(callback);
    }

    VIDEOPLAYER_API void UpdatePlayer(VideoPlayer *player)
    {
        if (!player)
            return;
        player->update();
    }

    VIDEOPLAYER_API int GetPlayerState(VideoPlayer *player)
    {
        if (!player)
            return static_cast<int>(PlayerState::UNINITIALIZED);
        return static_cast<int>(player->getPlayerState());
    }

    VIDEOPLAYER_API int GetPlayerError(VideoPlayer *player)
    {
        if (!player)
            return static_cast<int>(PlayerError::NONE);
        return static_cast<int>(player->getPlayerError());
    }

    VIDEOPLAYER_API const char *GetPlayerErrorMessage(VideoPlayer *player)
    {
        if (!player)
            return "Player not found";
        return player->getPlayerErrorMessage();
    }

    VIDEOPLAYER_API int GetVideoCacheSize(VideoPlayer *player)
    {
        if (!player)
            return 0;
        return static_cast<int>(player->getVideoCacheSize());
    }

    VIDEOPLAYER_API int GetAudioCacheSize(VideoPlayer *player)
    {
        if (!player)
            return 0;
        return static_cast<int>(player->getAudioCacheSize());
    }

    VIDEOPLAYER_API double GetLastVideoCacheTime(VideoPlayer *player)
    {
        if (!player)
            return -1.0;
        return player->getLastVideoCacheTime();
    }

    VIDEOPLAYER_API double GetLastAudioCacheTime(VideoPlayer *player)
    {
        if (!player)
            return -1.0;
        return player->getLastAudioCacheTime();
    }

    VIDEOPLAYER_API const char *GetFFMPEGDetails(VideoPlayer *player)
    {
        if (!player)
            return "Player not found";
        return player->getFFMPEGDetails();
    }

} // extern "C"
