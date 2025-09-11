#pragma once

#include "VideoPlayer.h"
#include <thread>
#include <atomic>
#include <mutex>
#include <condition_variable>
#include <map>
#include <functional>

extern "C" {
#include <libavcodec/avcodec.h>
#include <libavformat/avformat.h>
#include <libavutil/avutil.h>
#include <libavutil/imgutils.h>
#include <libavutil/opt.h>
#include <libswscale/swscale.h>
#include <libswresample/swresample.h>
}

class VideoPlayerImpl {
public:
    VideoPlayerImpl();
    ~VideoPlayerImpl();

    bool loadVideo(const char* url);
    void play();
    void pause();
    void resume();
    void stop();
    void seek(double time);
    VideoFrame* getVideoFrameAtTime(double time);
    AudioFrame* getAudioFrameAtTime(double time);
    double getDuration() const;
    double getCurrentTime() const;
    int getVideoWidth() const;
    int getVideoHeight() const;
    double getFrameRate() const;
    void setVideoFrameCallback(VideoFrameCallback callback);
    void setAudioFrameCallback(AudioFrameCallback callback);
    void update();
    PlayerState getPlayerState() const;
    PlayerError getPlayerError() const;
    const char* getPlayerErrorMessage() const;

private:
    void cleanup();
    void decodingLoop();
    void decodeVideoPacket(AVPacket* packet);
    void decodeAudioPacket(AVPacket* packet);

    AVFormatContext* formatContext;
    AVCodecContext* videoCodecContext;
    AVCodecContext* audioCodecContext;
    SwsContext* swsContext;
    SwrContext* swrContext;
    AVFrame* frame;
    AVFrame* videoFrame;
    AVFrame* audioFrame;
    
    int videoStreamIndex;
    int audioStreamIndex;
    
    std::atomic<bool> isPlaying;
    std::atomic<bool> isPaused;
    std::atomic<bool> shouldStop;
    std::atomic<double> currentTime;
    std::atomic<double> duration;
    
    std::atomic<PlayerState> playerState;
    std::atomic<PlayerError> playerError;
    std::string errorMessage;
    
    std::thread decodingThread;
    std::mutex decodingMutex;
    std::condition_variable decodingCondition;
    
    VideoFrameCallback videoCallback;
    AudioFrameCallback audioCallback;
    
    std::map<double, VideoFrame> cachedVideoFrames;
    std::map<double, AudioFrame> cachedAudioFrames;
    std::mutex cacheMutex;
    
    int videoWidth;
    int videoHeight;
    double frameRate;
};
